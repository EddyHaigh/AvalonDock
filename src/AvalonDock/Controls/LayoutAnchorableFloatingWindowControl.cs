/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using AvalonDock.Commands;
using AvalonDock.Converters;
using AvalonDock.Layout;

using Microsoft.Windows.Shell;

using Windows.Win32;

namespace AvalonDock.Controls
{
    /// <inheritdoc cref="LayoutFloatingWindowControl"/>
    /// <inheritdoc cref="IOverlayWindowHost"/>
    /// <summary>
    /// Class visualizes floating <see cref="LayoutAnchorable"/> (toolwindows) in AvalonDock.
    /// </summary>
    /// <seealso cref="LayoutFloatingWindowControl"/>
    /// <seealso cref="IOverlayWindowHost"/>
    public class LayoutAnchorableFloatingWindowControl : LayoutFloatingWindowControl, IOverlayWindowHost
    {
        /// <summary><see cref="SingleContentLayoutItem"/> dependency property.</summary>
        public static readonly DependencyProperty SingleContentLayoutItemProperty
            = DependencyProperty.Register(
                nameof(SingleContentLayoutItem),
                typeof(LayoutItem),
                typeof(LayoutAnchorableFloatingWindowControl),
                new FrameworkPropertyMetadata(null, OnSingleContentLayoutItemChanged));

        private readonly LayoutAnchorableFloatingWindow _model;
        private List<IDropArea> _dropAreas = null;
        private OverlayWindow _overlayWindow = null;
        static LayoutAnchorableFloatingWindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorableFloatingWindowControl), new FrameworkPropertyMetadata(typeof(LayoutAnchorableFloatingWindowControl)));
        }

        internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model, bool isContentImmutable)
           : base(model, isContentImmutable)
        {
            _model = model;
            HideWindowCommand = new RelayCommand<object>((p) => OnExecuteHideWindowCommand(p), (p) => CanExecuteHideWindowCommand(p));
            CloseWindowCommand = new RelayCommand<object>((p) => OnExecuteCloseWindowCommand(p), (p) => CanExecuteCloseWindowCommand(p));
            Activated += LayoutAnchorableFloatingWindowControl_Activated;
            UpdateThemeResources();
            MinWidth = _model.RootPanel.CalculatedDockMinWidth();
            MinHeight = _model.RootPanel.CalculatedDockMinHeight();
            if (_model.Root is LayoutRoot root)
            {
                root.Updated += OnRootUpdated;
            }

            _model.IsVisibleChanged += ModelIsVisibleChanged;
        }

        internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model)
            : this(model, false)
        {
        }

        public ICommand CloseWindowCommand { get; }

        public ICommand HideWindowCommand { get; }

        DockingManager IOverlayWindowHost.Manager => _model.Root.Manager;

        /// <inheritdoc />
        public override ILayoutElement Model => _model;

        /// <summary>Gets/sets the layout item of the selected content when shown in a single anchorable pane.</summary>
        [Bindable(true)]
        [Description("Gets/sets the layout item of the selected content when shown in a single anchorable pane.")]
        [Category("Anchorable")]
        public LayoutItem SingleContentLayoutItem
        {
            get => (LayoutItem)GetValue(SingleContentLayoutItemProperty);
            set => SetValue(SingleContentLayoutItemProperty, value);
        }

        /// <inheritdoc />
        public override void DisableBindings()
        {
            if (Model.Root is LayoutRoot layoutRoot)
            {
                layoutRoot.Updated -= OnRootUpdated;
            }

            BindingOperations.ClearBinding(_model, VisibilityProperty);
            _model.PropertyChanged -= ModelPropertyChanged;
            base.DisableBindings();
        }

        /// <inheritdoc />
        public override void EnableBindings()
        {
            _model.PropertyChanged += ModelPropertyChanged;
            SetVisibilityBinding();
            if (Model.Root is LayoutRoot layoutRoot)
            {
                layoutRoot.Updated += OnRootUpdated;
            }

            base.EnableBindings();
        }

        IEnumerable<IDropArea> IOverlayWindowHost.GetDropAreas(LayoutFloatingWindowControl draggingWindow)
        {
            if (_dropAreas != null)
            {
                return _dropAreas;
            }

            _dropAreas = new List<IDropArea>();
            if (draggingWindow.Model is LayoutDocumentFloatingWindow)
            {
                return _dropAreas;
            }

            var rootVisual = (Content as FloatingWindowContentHost).RootVisual;
            foreach (var areaHost in rootVisual.FindVisualChildren<LayoutAnchorablePaneControl>())
            {
                _dropAreas.Add(new DropArea<LayoutAnchorablePaneControl>(areaHost, DropAreaType.AnchorablePane));
            }

            foreach (var areaHost in rootVisual.FindVisualChildren<LayoutDocumentPaneControl>())
            {
                _dropAreas.Add(new DropArea<LayoutDocumentPaneControl>(areaHost, DropAreaType.DocumentPane));
            }

            return _dropAreas;
        }

        void IOverlayWindowHost.HideOverlayWindow()
        {
            _dropAreas = null;
            _overlayWindow.Owner = null;
            _overlayWindow.HideDropTargets();
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        bool IOverlayWindowHost.HitTestScreen(Point dragPoint)
        {
            return HitTest(this.TransformToDeviceDPI(dragPoint));
        }

        IOverlayWindow IOverlayWindowHost.ShowOverlayWindow(LayoutFloatingWindowControl draggingWindow)
        {
            CreateOverlayWindow(draggingWindow);
            _overlayWindow.EnableDropTargets();
            _overlayWindow.Show();
            return _overlayWindow;
        }

        /// <inheritdoc />
        internal override void UpdateThemeResources(Themes.Theme oldTheme = null)
        {
            base.UpdateThemeResources(oldTheme);
            _overlayWindow?.UpdateThemeResources(oldTheme);
        }

        /// <inheritdoc />
        protected override IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((uint)msg)
            {
                case PInvoke.WM_ACTIVATE:
                    var isInactive = ((int)wParam & 0xFFFF) == PInvoke.WA_INACTIVE;
                    if (_model.IsSinglePane)
                    {
                        LayoutFloatingWindowControlHelper.ActiveTheContentOfSinglePane(this, !isInactive);
                    }
                    else
                    {
                        LayoutFloatingWindowControlHelper.ActiveTheContentOfMultiPane(this, !isInactive);
                    }

                    handled = true;
                    break;

                case PInvoke.WM_NCRBUTTONUP:
                    if (wParam.ToInt32() == PInvoke.HTCAPTION)
                    {
                        var windowChrome = WindowChrome.GetWindowChrome(this);
                        if (windowChrome != null)
                        {
                            if (OpenContextMenu())
                            {
                                handled = true;
                            }

                            windowChrome.ShowSystemMenu = _model.Root.Manager.ShowSystemMenu && !handled;
                        }
                    }
                    break;
            }
            return base.FilterMessage(hwnd, msg, wParam, lParam, ref handled);
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            var root = Model.Root;
            if (root != null)
            {
                if (root is LayoutRoot layoutRoot)
                {
                    layoutRoot.Updated -= OnRootUpdated;
                }

                root.Manager.RemoveFloatingWindow(this);
                root.CollectGarbage();
            }
            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
            base.OnClosed(e);
            if (!CloseInitiatedByUser)
            {
                root?.FloatingWindows.Remove(_model);
            }

            // We have to clear binding instead of creating a new empty binding.
            BindingOperations.ClearBinding(_model, VisibilityProperty);

            _model.PropertyChanged -= ModelPropertyChanged;
            _model.IsVisibleChanged -= ModelIsVisibleChanged;
            Activated -= LayoutAnchorableFloatingWindowControl_Activated;
            IsVisibleChanged -= LayoutAnchorableFloatingWindowControl_IsVisibleChanged;
            BindingOperations.ClearBinding(this, VisibilityProperty);
            BindingOperations.ClearBinding(this, SingleContentLayoutItemProperty);
        }

        /// <inheritdoc />
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var canHide = HideWindowCommand.CanExecute(null);
            if (CloseInitiatedByUser && !KeepContentVisibleOnClose && !canHide)
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        /// <inheritdoc />
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var manager = _model.Root.Manager;
            Content = manager.CreateUIElementForModel(_model.RootPanel);
            //SetBinding(VisibilityProperty, new Binding("IsVisible") { Source = _model, Converter = new BoolToVisibilityConverter(), Mode = BindingMode.OneWay, ConverterParameter = Visibility.Hidden });

            //Issue: http://avalondock.codeplex.com/workitem/15036
            IsVisibleChanged += LayoutAnchorableFloatingWindowControl_IsVisibleChanged;
            SetBinding(SingleContentLayoutItemProperty, new Binding("Model.SinglePane.SelectedContent") { Source = this, Converter = new LayoutItemFromLayoutModelConverter() });
            _model.PropertyChanged += ModelPropertyChanged;
        }

        /// <summary>Provides derived classes an opportunity to handle changes to the <see cref="SingleContentLayoutItem"/> property.</summary>
        protected virtual void OnSingleContentLayoutItemChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>Handles changes to the <see cref="SingleContentLayoutItem"/> property.</summary>
        private static void OnSingleContentLayoutItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LayoutAnchorableFloatingWindowControl)d).OnSingleContentLayoutItemChanged(e);

        private void ModelIsVisibleChanged(object sender, EventArgs e)
        {
            if (!IsVisible && _model.IsVisible)
            {
                Show();
            }
        }

        private void ModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LayoutAnchorableFloatingWindow.RootPanel):
                    if (_model.RootPanel == null)
                    {
                        InternalClose();
                    }

                    break;

                case nameof(LayoutAnchorableFloatingWindow.IsVisible):
                    if (_model.IsVisible != IsVisible)
                    {
                        Visibility = _model.IsVisible ? Visibility.Visible : Visibility.Hidden;
                    }

                    break;
            }
        }

        private bool CanExecuteCloseWindowCommand(object parameter)
        {
            var manager = Model?.Root?.Manager;
            if (manager == null)
            {
                return false;
            }

            var canExecute = false;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                if (!anchorable.CanClose)
                {
                    canExecute = false;
                    break;
                }
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                if (anchorableLayoutItem?.CloseCommand == null || !anchorableLayoutItem.CloseCommand.CanExecute(parameter))
                {
                    canExecute = false;
                    break;
                }
                canExecute = true;
            }
            return canExecute;
        }

        private bool CanExecuteHideWindowCommand(object parameter)
        {
            var manager = Model?.Root?.Manager;
            if (manager == null)
            {
                return false;
            }

            var canExecute = false;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                if (!anchorable.CanHide)
                {
                    canExecute = false;
                    break;
                }
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                if (anchorableLayoutItem?.HideCommand == null || !anchorableLayoutItem.HideCommand.CanExecute(parameter))
                {
                    canExecute = false;
                    break;
                }
                canExecute = true;
            }
            return canExecute;
        }

        private void CreateOverlayWindow(LayoutFloatingWindowControl draggingWindow)
        {
            _overlayWindow ??= new OverlayWindow(this);

            // Usually, the overlay window is made a child of the main window. However, if the floating
            // window being dragged isn't also a child of the main window (because OwnedByDockingManagerWindow
            // is set to false to allow the parent window to be minimized independently of floating windows)
            if (draggingWindow?.OwnedByDockingManagerWindow ?? true)
            {
                _overlayWindow.Owner = Window.GetWindow(_model.Root.Manager);
            }
            else
            {
                _overlayWindow.Owner = null;
            }

            var rectWindow = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()), this.TransformActualSizeToAncestor());
            _overlayWindow.Left = rectWindow.Left;
            _overlayWindow.Top = rectWindow.Top;
            _overlayWindow.Width = rectWindow.Width;
            _overlayWindow.Height = rectWindow.Height;
        }

        bool HitTest(Point dragPoint)
        {
            if (dragPoint == default(Point))
            {
                return false;
            }

            var detectionRect = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()), this.TransformActualSizeToAncestor());
            return detectionRect.Contains(dragPoint);
        }

        private void LayoutAnchorableFloatingWindowControl_Activated(object sender, EventArgs e)
        {
            // Issue similar to: http://avalondock.codeplex.com/workitem/15036
            var visibilityBinding = GetBindingExpression(VisibilityProperty);
            if (visibilityBinding == null && Visibility == Visibility.Visible)
            {
                SetVisibilityBinding();
            }
        }

        /// <summary>IsVisibleChanged Event Handler.</summary>
        private void LayoutAnchorableFloatingWindowControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var visibilityBinding = GetBindingExpression(VisibilityProperty);
            if (IsVisible && visibilityBinding == null)
            {
                SetBinding(VisibilityProperty, new Binding(nameof(IsVisible))
                { Source = _model, Converter = new BoolToVisibilityConverter(), Mode = BindingMode.OneWay, ConverterParameter = Visibility.Hidden });
            }
        }

        private void OnExecuteCloseWindowCommand(object parameter)
        {
            var manager = Model.Root.Manager;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                anchorableLayoutItem.CloseCommand.Execute(parameter);
            }
        }

        private void OnExecuteHideWindowCommand(object parameter)
        {
            var manager = Model.Root.Manager;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                anchorableLayoutItem.HideCommand.Execute(parameter);
            }
            Hide(); // Bring toolwindows inside hidden FloatingWindow back requires restart of app
        }

        private void OnRootUpdated(object sender, EventArgs e)
        {
            if (_model?.RootPanel == null)
            {
                return;
            }

            MinWidth = _model.RootPanel.CalculatedDockMinWidth();
            MinHeight = _model.RootPanel.CalculatedDockMinHeight();
        }
        private bool OpenContextMenu()
        {
            var ctxMenu = _model.Root.Manager.AnchorableContextMenu;
            if (ctxMenu == null || SingleContentLayoutItem == null)
            {
                return false;
            }

            ctxMenu.PlacementTarget = null;
            ctxMenu.Placement = PlacementMode.MousePoint;
            ctxMenu.DataContext = SingleContentLayoutItem;
            ctxMenu.IsOpen = true;
            return true;
        }

        private void SetVisibilityBinding()
        {
            SetBinding(
              VisibilityProperty,
              new Binding(nameof(IsVisible))
              {
                  Source = _model,
                  Converter = new BoolToVisibilityConverter(),
                  Mode = BindingMode.OneWay,
                  ConverterParameter = Visibility.Hidden
              }
            );
        }
    }
}
