/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using AvalonDock.Layout;
using AvalonDock.Themes;

using Standard;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AvalonDock.Controls
{
    /// <inheritdoc cref="Window"/>
    /// <inheritdoc cref="ILayoutControl"/>
    /// <summary>
    /// Implements an abstraction layer for floating windows that can host other controls
    /// (eg. documents and/or <see cref="LayoutAnchorable"/>).
    ///
    /// A floating window can be dragged around independently of the <see cref="DockingManager"/>.
    /// </summary>
    /// <seealso cref="Window"/>
    /// <seealso cref="ILayoutControl"/>
    public abstract class LayoutFloatingWindowControl : Window, ILayoutControl
    {
        /// <summary><see cref="AllowMinimize"/> dependency property.</summary>
        public static readonly DependencyProperty AllowMinimizeProperty =
            DependencyProperty.Register(
                "AllowMinimize",
                typeof(bool),
                typeof(LayoutFloatingWindowControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ContentMinHeightProperty;

        public static readonly DependencyPropertyKey ContentMinHeightPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ContentMinHeight),
                typeof(double),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty ContentMinWidthProperty;

        public static readonly DependencyPropertyKey ContentMinWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ContentMinWidth),
                typeof(double),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(0.0));

        /// <summary> <see cref="IsContentImmutable"/> dependency property.</summary>
        public static readonly DependencyProperty IsContentImmutableProperty
            = DependencyProperty.Register(
                nameof(IsContentImmutable),
                typeof(bool),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty;
        /// <summary><see cref="IsMaximized"/> dependency property.</summary>
        public static readonly DependencyProperty IsMaximizedProperty =
            DependencyProperty.Register(
                nameof(IsMaximized),
                typeof(bool),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(false));

        /// <summary><see cref="OwnedByDockingManagerWindow"/> dependency property.</summary>
        public static readonly DependencyProperty OwnedByDockingManagerWindowProperty =
            DependencyProperty.Register(
                "OwnedByDockingManagerWindow",
                typeof(bool),
                typeof(LayoutFloatingWindowControl),
                new PropertyMetadata(true, OwnedByDockingManagerWindowPropertyChanged));

        public static readonly DependencyProperty TotalMarginProperty;

        /// <summary><see cref="IsDragging"/> Read-Only dependency property.</summary>
        private static readonly DependencyPropertyKey IsDraggingPropertyKey
            = DependencyProperty.RegisterReadOnly(
                nameof(IsDragging),
                typeof(bool),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(false, OnIsDraggingChanged));

        private static readonly DependencyPropertyKey TotalMarginPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(TotalMargin),
                typeof(Thickness),
                typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(default(Thickness)));

        private readonly ILayoutElement _model;
        private bool _attachDrag = false;
        private DragService _dragService = null;
        private HwndSource _hwndSrc;
        private HwndSourceHook _hwndSrcHook;
        private bool _internalCloseFlag = false;
        private bool _isClosing = false;
        private bool _isInternalChange;

        //false
        /// <summary>
        /// Is false until the margins have been found once.
        /// </summary>
        /// <see cref="TotalMargin"/>
        private bool _isTotalMarginSet = false;

        private ResourceDictionary _currentThemeResourceDictionary; // = null

        static LayoutFloatingWindowControl()
        {
            AllowsTransparencyProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl), new FrameworkPropertyMetadata(false));
            ContentProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl), new FrameworkPropertyMetadata(null, null, CoerceContentValue));
            ShowInTaskbarProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl), new FrameworkPropertyMetadata(false));

            ContentMinHeightProperty = ContentMinHeightPropertyKey.DependencyProperty;
            ContentMinWidthProperty = ContentMinWidthPropertyKey.DependencyProperty;
            IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;
            TotalMarginProperty = TotalMarginPropertyKey.DependencyProperty;
        }

        protected LayoutFloatingWindowControl(ILayoutElement model)
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Closing += OnClosing;
            SizeChanged += OnSizeChanged;
            _model = model;
        }

        protected LayoutFloatingWindowControl(ILayoutElement model, bool isContentImmutable)
          : this(model)
        {
            IsContentImmutable = isContentImmutable;
        }

        /// <summary>
        /// Gets/sets whether the floating window supports being minimized.
        /// </summary>
        public bool AllowMinimize
        {
            get { return (bool)GetValue(AllowMinimizeProperty); }
            set { SetValue(AllowMinimizeProperty, value); }
        }

        /// <summary>
        /// The MinHeight of the content of the window, will be 0 until the window has been rendered, or if the MinHeight is unset for the content
        /// </summary>
        public double ContentMinHeight
        {
            get { return (double)GetValue(ContentMinHeightProperty); }
            set { SetValue(ContentMinHeightPropertyKey, value); }
        }

        /// <summary>
        /// The MinWidth ocf the content of the window, will be 0 until the window has been rendered, or if the MinWidth is unset for the content
        /// </summary>
        public double ContentMinWidth
        {
            get { return (double)GetValue(ContentMinWidthProperty); }
            set { SetValue(ContentMinWidthPropertyKey, value); }
        }

        /// <summary>Gets/sets wether the content can be modified.</summary>
        [Bindable(true)]
        [Description("Gets/sets wether the content can be modified.")]
        [Category("Other")]
        public bool IsContentImmutable
        {
            get => (bool)GetValue(IsContentImmutableProperty);
            private set => SetValue(IsContentImmutableProperty, value);
        }

        /// <summary>Gets wether this floating window is being dragged.</summary>
        [Bindable(true)]
        [Description("Gets wether this floating window is being dragged.")]
        [Category("FloatingWindow")]
        public bool IsDragging => (bool)GetValue(IsDraggingProperty);

        /// <summary>Gets/sets the <see cref="IsMaximized"/> property. This dependency property indicates if the window is maximized.</summary>
        /// <remarks>Provides a secure method for setting the <see cref="IsMaximized"/> property.</remarks>
        public bool IsMaximized
        {
            get => (bool)GetValue(IsMaximizedProperty);
            private set
            {
                SetValue(IsMaximizedProperty, value);
                UpdatePositionAndSizeOfPanes();
            }
        }

        public abstract ILayoutElement Model { get; }

        /// <summary>
        /// Gets or sets a value indicating whether an undocked child window should be "owned" by the window
        /// that hosts the docking manager or whether it should be an independent window.
        /// </summary>
        public bool OwnedByDockingManagerWindow
        {
            get { return (bool)GetValue(OwnedByDockingManagerWindowProperty); }
            set { SetValue(OwnedByDockingManagerWindowProperty, value); }
        }

        /// <summary>
        /// The total margin (including window chrome and title bar).
        ///
        /// The margin is queried from the visual tree the first time it is rendered, zero until the first call of FilterMessage(WM_ACTIVATE)
        /// </summary>
        public Thickness TotalMargin
        {
            get { return (Thickness)GetValue(TotalMarginProperty); }
            protected set { SetValue(TotalMarginPropertyKey, value); }
        }

        /// <summary>
        /// Gets/Sets the X,Y delta between the element being dragged and the
        /// mouse position. The value of this property is used during the drag
        /// cycle to position the dragged item under the mouse pointer.
        ///
        /// Set this property on initialization to ensure that
        /// the delta between mouse and control being dragged
        /// remains constant.
        /// </summary>
        internal Point DragDelta { get; set; }
        internal bool KeepContentVisibleOnClose { get; set; }

        protected bool CloseInitiatedByUser => !_internalCloseFlag;

        public virtual void DisableBindings()
        {
        }

        public virtual void EnableBindings()
        {
        }

        internal void AttachDrag(bool onActivated = true)
        {
            if (onActivated)
            {
                _attachDrag = true;
                Activated += OnActivated;
            }
            else
            {
                CaptureMouse();
                var windowHandle = new WindowInteropHelper(this).Handle;
                var lParam = new IntPtr(((int)Left & 0xFFFF) | ((int)Top << 16));
                PInvoke.SendMessage(new HWND(windowHandle), PInvoke.WM_NCLBUTTONDOWN, new WPARAM(PInvoke.HTCAPTION), lParam);
            }
        }

        internal void InternalClose(bool closeInitiatedByUser = false)
        {
            _internalCloseFlag = !closeInitiatedByUser;
            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            Close();
        }

        internal void UpdateOwnership()
        {
            // Determine whether the child window should be owned by the parent or act independently
            // according to OwnedByDockingManagerWindow property.
            var manager = Model?.Root?.Manager;
            if (OwnedByDockingManagerWindow && manager != null)
            {
                this.SetParentToMainWindowOf(manager);
            }
            else
            {
                this.SetParentWindowToNull();
            }
        }

        /// <summary>Is Invoked when AvalonDock's WPF Theme changes via the <see cref="DockingManager.OnThemeChanged()"/> method.</summary>
        /// <param name="oldTheme"></param>
        internal virtual void UpdateThemeResources(Theme oldTheme = null)
        {
            if (oldTheme != null) // Remove the old theme if present
            {
                if (oldTheme is DictionaryTheme)
                {
                    if (_currentThemeResourceDictionary != null)
                    {
                        Resources.MergedDictionaries.Remove(_currentThemeResourceDictionary);
                        _currentThemeResourceDictionary = null;
                    }
                }
                else
                {
                    var resourceDictionaryToRemove =
                        Resources.MergedDictionaries.FirstOrDefault(r => r.Source == oldTheme.GetResourceUri());
                    if (resourceDictionaryToRemove != null)
                    {
                        Resources.MergedDictionaries.Remove(
                            resourceDictionaryToRemove);
                    }
                }
            }

            // Implicit parameter to this method is the new theme already set here
            var manager = _model.Root?.Manager;
            if (manager?.Theme == null)
            {
                return;
            }

            if (manager.Theme is DictionaryTheme dictionaryTheme)
            {
                _currentThemeResourceDictionary = dictionaryTheme.ThemeResourceDictionary;
                Resources.MergedDictionaries.Add(_currentThemeResourceDictionary);
            }
            else
            {
                Resources.MergedDictionaries.Add(new ResourceDictionary { Source = manager.Theme.GetResourceUri() });
            }
        }

        protected virtual IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            switch ((uint)msg)
            {
                case PInvoke.WM_ACTIVATE:
                    UpdateWindowsSizeBasedOnMinSize();
                    break;

                case PInvoke.WM_EXITSIZEMOVE:
                    UpdatePositionAndSizeOfPanes();

                    if (_dragService != null)
                    {
                        var mousePosition = (NativeMethods.GetMousePosition());
                        _dragService.Drop(mousePosition, out var dropFlag);
                        _dragService = null;
                        SetIsDragging(false);
                        if (dropFlag)
                        {
                            InternalClose();
                        }
                    }
                    break;

                case PInvoke.WM_MOVING:
                    {
                        UpdateDragPosition();
                        if (IsMaximized)
                        {
                            UpdateMaximizedState(false);
                        }
                    }
                    break;

                case PInvoke.WM_LBUTTONUP: //set as handled right button click on title area (after showing context menu)
                    if (_dragService != null && Mouse.LeftButton == MouseButtonState.Released)
                    {
                        _dragService.Abort();
                        _dragService = null;
                        SetIsDragging(false);
                    }
                    break;

                case PInvoke.WM_SYSCOMMAND:
                    var command = (int)wParam & 0xFFF0;
                    if (command == PInvoke.SC_MAXIMIZE || command == PInvoke.SC_RESTORE)
                    {
                        UpdateMaximizedState(command == PInvoke.SC_MAXIMIZE);
                    }

                    break;
            }
            return IntPtr.Zero;
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            SizeChanged -= OnSizeChanged;
            if (Content != null)
            {
                (Content as FloatingWindowContentHost)?.Dispose();
                if (_hwndSrc != null)
                {
                    _hwndSrc.RemoveHook(_hwndSrcHook);
                    _hwndSrc.Dispose();
                    _hwndSrc = null;
                }
            }
            base.OnClosed(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            AssureOwnerIsNotMinimized();
        }

        /// <inheritdoc />
        protected override void OnInitialized(EventArgs e)
        {
            CommandBindings.Add(new CommandBinding(Microsoft.Windows.Shell.SystemCommands.CloseWindowCommand,
                (s, args) => Microsoft.Windows.Shell.SystemCommands.CloseWindow((Window)args.Parameter)));
            CommandBindings.Add(new CommandBinding(Microsoft.Windows.Shell.SystemCommands.MaximizeWindowCommand,
                (s, args) => Microsoft.Windows.Shell.SystemCommands.MaximizeWindow((Window)args.Parameter)));
            CommandBindings.Add(new CommandBinding(Microsoft.Windows.Shell.SystemCommands.MinimizeWindowCommand,
                (s, args) => Microsoft.Windows.Shell.SystemCommands.MinimizeWindow((Window)args.Parameter)));
            CommandBindings.Add(new CommandBinding(Microsoft.Windows.Shell.SystemCommands.RestoreWindowCommand,
                (s, args) => Microsoft.Windows.Shell.SystemCommands.RestoreWindow((Window)args.Parameter)));
            //Debug.Assert(this.Owner != null);
            base.OnInitialized(e);
        }

        /// <summary>Provides derived classes an opportunity to handle changes to the <see cref="IsDragging"/> property.</summary>
        protected virtual void OnIsDraggingChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                CaptureMouse();
            }
            else
            {
                ReleaseMouseCapture();
            }
        }

        /// <inheritdoc />
        protected override void OnStateChanged(EventArgs e)
        {
            if (!_isInternalChange)
            {
                if (WindowState == WindowState.Maximized)
                {
                    // Forward external changes to WindowState from any state to a new Maximized state
                    // to the LayoutFloatingWindowControl internal representation.
                    UpdateMaximizedState(true);
                }
                else if (IsMaximized && OwnedByDockingManagerWindow)
                {
                    // Override any external changes to WindowState when owned and in Maximized state.
                    // This override fixes the issue of an owned LayoutFloatingWindowControl loosing
                    // its Maximized state when the owner window is restored from a Minimized state.
                    WindowState = WindowState.Maximized;
                }
            }

            base.OnStateChanged(e);
        }

        /// <summary>
        /// Provides a secure method for setting the <see cref="IsDragging"/> property.
        /// This dependency property indicates that this floating window is being dragged.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsDragging(bool value) => SetValue(IsDraggingPropertyKey, value);

        private static object CoerceContentValue(DependencyObject sender, object content)
        {
            if (sender is not LayoutFloatingWindowControl lfwc)
            {
                return null;
            }

            if (lfwc.IsLoaded && lfwc.IsContentImmutable)
            {
                return lfwc.Content;
            }

            return new FloatingWindowContentHost((LayoutFloatingWindowControl)sender) { Content = content as UIElement };
        }

        /// <summary>Handles changes to the <see cref="IsDragging"/> property.</summary>
        private static void OnIsDraggingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LayoutFloatingWindowControl)d).OnIsDraggingChanged(e);

        private static void OwnedByDockingManagerWindowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LayoutFloatingWindowControl w && w.IsLoaded)
            {
                w.UpdateOwnership();
            }
        }
        /// <summary>
        /// Prevents a known bug in WPF, which wronlgy minimizes the parent window, when closing this control
        /// </summary>
        private void AssureOwnerIsNotMinimized()
        {
            try
            {
                Owner?.Activate();
            }
            catch (Exception)
            {
            }
        }

        private void OnActivated(object sender, EventArgs e)
        {
            Activated -= OnActivated;

            if (!_attachDrag || Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var windowHandle = new WindowInteropHelper(this).Handle;
            var mousePosition = this.PointToScreenDPI(Mouse.GetPosition(this));

            var area = this.GetScreenArea();

            // BugFix Issue #6
            // This code is initializes the drag when content (document or toolwindow) is dragged
            // A second chance back up plan if DragDelta is not set
            if (DragDelta == default)
            {
                DragDelta = new Point(3, 3);
            }

            Left = mousePosition.X - DragDelta.X;                 // BugFix Issue #6
            Top = mousePosition.Y - DragDelta.Y;

            if (this.GetScreenArea().Size != area.Size) // setting the top/left co-ordinates has changed the size - this means moving to a screen with a different DPI. Recalculate mouse position based on new DPI to avoid wrong drag location
            {
                mousePosition = this.PointToScreenDPI(Mouse.GetPosition(this));
                Left = mousePosition.X - DragDelta.X;
                Top = mousePosition.Y - DragDelta.Y;
            }

            _attachDrag = false;
            Show();
            var lParam = new IntPtr(((int)mousePosition.X & 0xFFFF) | ((int)mousePosition.Y << 16));
            PInvoke.SendMessage(new HWND(windowHandle), PInvoke.WM_NCLBUTTONDOWN, new WPARAM(PInvoke.HTCAPTION), lParam);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            Closing -= OnClosing;
            // If this window was Closed not from InternalClose method,
            // mark it as closing to avoid "InvalidOperationException: : Cannot set Visibility to Visible or call Show, ShowDialog,
            // Close, or WindowInteropHelper.EnsureHandle while a Window is closing".
            if (!_isClosing)
            {
                _isClosing = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            this.UpdateOwnership();

            _hwndSrc = PresentationSource.FromDependencyObject(this) as HwndSource;
            _hwndSrcHook = FilterMessage;
            _hwndSrc.AddHook(_hwndSrcHook);
            // Restore maximize state
            var maximized = Model.Descendents().OfType<ILayoutElementForFloatingWindow>().Any(l => l.IsMaximized);
            UpdateMaximizedState(maximized);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var posElement in Model.Descendents().OfType<ILayoutElementForFloatingWindow>())
            {
                posElement.FloatingWidth = ActualWidth;
                posElement.FloatingHeight = ActualHeight;
                posElement.RaiseFloatingPropertiesUpdated();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            if (_hwndSrc == null)
            {
                return;
            }

            _hwndSrc.RemoveHook(_hwndSrcHook);
            InternalClose();
        }

        private void UpdateDragPosition()
        {
            if (_dragService == null)
            {
                _dragService = new DragService(this);
                SetIsDragging(true);
            }
            var mousePosition = (NativeMethods.GetMousePosition());
            _dragService.UpdateMouseLocation(mousePosition);
        }

        /// <summary>
        /// Set the margins of the window control (including the borders of the floating window and the title bar).
        /// The result will be stored in <code>_totalMargin</code>.
        /// </summary>
        /// <remarks>If the control is not loaded <code>_totalMargin</code> will not be set.</remarks>
        private void UpdateMargins()
        {
            // The grid with window bar and content
            var grid = this.GetChildrenRecursive()
                .OfType<Grid>()
                .FirstOrDefault(g => g.RowDefinitions.Count > 0);
            ContentPresenter contentControl = this.GetChildrenRecursive()
                .OfType<ContentPresenter>()
                .FirstOrDefault(c => c.Content is LayoutContent);
            if (contentControl == null)
            {
                return;
            }
            // The content control in the grid, this has a different tree to walk up
            var layoutContent = (LayoutContent)contentControl.Content;
            if (grid != null && layoutContent.Content is FrameworkElement content)
            {
                var parents = content.GetParents().ToArray();
                var children = this.GetChildrenRecursive()
                    .TakeWhile(c => c != grid)
                    .ToArray();
                var borders = children
                    .OfType<Border>()
                    .Concat(parents
                        .OfType<Border>())
                    .ToArray();
                var controls = children
                    .OfType<Control>()
                    .Concat(parents
                        .OfType<Control>())
                    .ToArray();
                var frameworkElements = children
                    .OfType<FrameworkElement>()
                    .Concat(parents
                        .OfType<FrameworkElement>())
                    .ToArray();
                var padding = controls.Sum(b => b.Padding);
                var border = borders.Sum(b => b.BorderThickness);
                var margin = frameworkElements.Sum(f => f.Margin);
                margin = margin.Add(padding).Add(border).Add(grid.Margin);
                margin.Top = grid.RowDefinitions[0].MinHeight;
                TotalMargin = margin;
                _isTotalMarginSet = true;
            }
        }

        private void UpdateMaximizedState(bool isMaximized)
        {
            foreach (var posElement in Model.Descendents().OfType<ILayoutElementForFloatingWindow>())
            {
                posElement.IsMaximized = isMaximized;
            }

            IsMaximized = isMaximized;
            _isInternalChange = true;

            if (isMaximized)
            {
                WindowState = WindowState.Maximized;
            }
            else if (!this.AllowMinimize || this.WindowState != WindowState.Minimized)
            {
                // If minimize is not supported, this prevents the window from being minimized.
                // by resetting it to the normal state.
                WindowState = WindowState.Normal;
            }

            _isInternalChange = false;
        }

        private void UpdatePositionAndSizeOfPanes()
        {
            foreach (var posElement in Model.Descendents().OfType<ILayoutElementForFloatingWindow>())
            {
                posElement.FloatingLeft = Left;
                posElement.FloatingTop = Top;
                posElement.FloatingWidth = Width;
                posElement.FloatingHeight = Height;
                posElement.RaiseFloatingPropertiesUpdated();
            }
        }

        /// <summary>
        /// Update the floating window size based on the <code>MinHeight</code> and <code>MinWidth</code> of the content of the control.
        /// </summary>
        /// <remarks>This will only be run once, when the window is rendered the first time and <code>_totalMargin</code> is identified.</remarks>
        private void UpdateWindowsSizeBasedOnMinSize()
        {
            if (!_isTotalMarginSet)
            {
                UpdateMargins();
                if (_isTotalMarginSet)
                {
                    // The LayoutAnchorableControl is bound via the ContentPresenter, hence it is best to do below in code and not in a style
                    // See https://github.com/Dirkster99/AvalonDock/pull/146#issuecomment-609974424
                    var layoutContents = this.GetChildrenRecursive()
                        .OfType<ContentPresenter>()
                        .Select(c => c.Content)
                        .OfType<LayoutContent>()
                        .Select(lc => lc.Content);
                    var contents = layoutContents.OfType<FrameworkElement>();
                    foreach (var content in contents)
                    {
                        ContentMinHeight = Math.Max(content.MinHeight, ContentMinHeight);
                        ContentMinWidth = Math.Max(content.MinWidth, ContentMinWidth);
                        if ((this.Model?.Root?.Manager?.AutoWindowSizeWhenOpened).GetValueOrDefault())
                        {
                            var parent = content.GetParents()
                                .OfType<FrameworkElement>()
                                .FirstOrDefault();
                            // StackPanels among others have an ActualHeight larger than visible, hence we check the parent control as well
                            if (content.ActualHeight < content.MinHeight ||
                                parent != null && parent.ActualHeight < content.MinHeight)
                            {
                                Height = content.MinHeight + TotalMargin.Top + TotalMargin.Bottom;
                            }

                            if (content.ActualWidth < content.MinWidth ||
                                parent != null && parent.ActualWidth < content.MinWidth)
                            {
                                Width = content.MinWidth + TotalMargin.Left + TotalMargin.Right;
                            }
                        }
                    }
                }
            }
        }
        protected internal class FloatingWindowContentHost : HwndHost
        {
            /// <summary><see cref="Content"/> dependency property. </summary>
            public static readonly DependencyProperty ContentProperty
                = DependencyProperty.Register(
                    nameof(Content),
                    typeof(UIElement),
                    typeof(FloatingWindowContentHost),
                    new FrameworkPropertyMetadata(null, OnContentChanged));

            /// <summary><see cref="SizeToContent"/> dependency property.</summary>
            public static readonly DependencyProperty SizeToContentProperty
                = DependencyProperty.Register(
                    nameof(SizeToContent),
                    typeof(SizeToContent),
                    typeof(FloatingWindowContentHost),
                    new FrameworkPropertyMetadata(SizeToContent.Manual, OnSizeToContentChanged));

            private readonly LayoutFloatingWindowControl _owner;
            private DockingManager _manager = null;
            private Border _rootPresenter = null;
            private HwndSource _wpfContentHost = null;

            public FloatingWindowContentHost(LayoutFloatingWindowControl owner)
            {
                _owner = owner;
                var binding = new Binding(nameof(SizeToContent)) { Source = _owner };
                BindingOperations.SetBinding(this, SizeToContentProperty, binding);
            }

            /// <summary>
            /// Gets or sets the <see cref="Content"/> property.  This dependency property
            /// indicates ....
            /// </summary>
            public UIElement Content
            {
                get => (UIElement)GetValue(ContentProperty);
                set => SetValue(ContentProperty, value);
            }

            public Visual RootVisual => _rootPresenter;

            /// <summary>Gets or sets the <see cref="SizeToContent"/> property.</summary>
            public SizeToContent SizeToContent
            {
                get => (SizeToContent)GetValue(SizeToContentProperty);
                set => SetValue(SizeToContentProperty, value);
            }

            /// <inheritdoc />
            protected override HandleRef BuildWindowCore(HandleRef hwndParent)
            {
                _wpfContentHost = new HwndSource(new HwndSourceParameters
                {
                    ParentWindow = hwndParent.Handle,
                    WindowStyle = (int)(WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPSIBLINGS | WINDOW_STYLE.WS_CLIPCHILDREN),
                    Width = 1,
                    Height = 1,
                    UsesPerPixelOpacity = true,
                });

                _rootPresenter = new Border { Child = new AdornerDecorator { Child = Content }, Focusable = true };
                AutomationProperties.SetName(_rootPresenter, "FloatingWindowHost");
                _rootPresenter.SetBinding(Border.BackgroundProperty, new Binding(nameof(Background)) { Source = _owner });
                _wpfContentHost.RootVisual = _rootPresenter;
                _manager = _owner.Model.Root.Manager;
                _manager.InternalAddLogicalChild(_rootPresenter);
                return new HandleRef(this, _wpfContentHost.Handle);
            }

            /// <inheritdoc />
            protected override void DestroyWindowCore(HandleRef hwnd)
            {
                _manager.InternalRemoveLogicalChild(_rootPresenter);
                if (_wpfContentHost == null)
                {
                    return;
                }

                _wpfContentHost.Dispose();
                _wpfContentHost = null;
            }

            /// <inheritdoc />
            protected override Size MeasureOverride(Size constraint)
            {
                if (Content == null)
                {
                    return base.MeasureOverride(constraint);
                }

                Content.Measure(constraint);
                return Content.DesiredSize;
            }

            /// <summary>Provides derived classes an opportunity to handle changes to the <see cref="Content"/> property.</summary>
            protected virtual void OnContentChanged(UIElement oldValue, UIElement newValue)
            {
                if (_rootPresenter != null)
                {
                    _rootPresenter.Child = Content;
                }

                if (oldValue is FrameworkElement oldContent)
                {
                    oldContent.SizeChanged -= Content_SizeChanged;
                }

                if (newValue is FrameworkElement newContent)
                {
                    newContent.SizeChanged += Content_SizeChanged;
                }
            }

            /// <summary>Provides derived classes an opportunity to handle changes to the <see cref="SizeToContent"/> property.</summary>
            protected virtual void OnSizeToContentChanged(SizeToContent oldValue, SizeToContent newValue)
            {
                if (_wpfContentHost != null)
                {
                    _wpfContentHost.SizeToContent = newValue;
                }
            }

            /// <summary>Handles changes to the <see cref="Content"/> property.</summary>
            private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FloatingWindowContentHost)d).OnContentChanged((UIElement)e.OldValue, (UIElement)e.NewValue);

            /// <summary>Handles changes to the <see cref="SizeToContent"/> property.</summary>
            private static void OnSizeToContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FloatingWindowContentHost)d).OnSizeToContentChanged((SizeToContent)e.OldValue, (SizeToContent)e.NewValue);

            /// <summary>
            /// Content_SizeChanged event handler.
            /// </summary>
            private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
            {
                InvalidateMeasure();
                InvalidateArrange();
            }
        }
    }
}
