/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Serialization;

using AvalonDock.Controls;

namespace AvalonDock.Layout
{
    /// <summary>
    /// Provides an abstract base class for common properties and methods of
    /// the <see cref="LayoutAnchorable"/> and <see cref="LayoutDocument"/> classes.
    /// </summary>
    [ContentProperty(nameof(Content))]
    [Serializable]
    public abstract class LayoutContent : LayoutElement, IXmlSerializable, ILayoutElementForFloatingWindow, IComparable<LayoutContent>, ILayoutPreviousContainer
    {
        public static readonly DependencyProperty ContentIdProperty
                    = DependencyProperty.Register(
                        nameof(ContentId),
                        typeof(string),
                        typeof(LayoutContent),
                        new UIPropertyMetadata(null, OnContentIdPropertyChanged));

        public static readonly DependencyProperty TitleProperty
            = DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(LayoutContent),
                new UIPropertyMetadata(null, OnTitlePropertyChanged, CoerceTitleValue));

        // BD: 14.08.2020 added _canCloseDefault to properly implement inverting _canClose default value in inheritors (e.g. LayoutAnchorable)
        //     Thus CanClose property will be serialized only when not equal to its default for given class
        //     With previous code it was not possible to serialize CanClose if set to true for LayoutAnchorable instance
        private bool _canClose = true;
        internal bool CanCloseDefault = true;

        private bool _canFloat = true;

        private bool _canShowOnHover = true;

        [NonSerialized]
        private object _content = null;

        private double _floatingHeight = 0.0;

        private double _floatingLeft = 0.0;

        private double _floatingTop = 0.0;

        private double _floatingWidth = 0.0;

        private ImageSource _iconSource = null;

        [field: NonSerialized]
        private bool _isActive = false;

        private bool _isEnabled = true;

        private bool _isLastFocusedDocument = false;

        private bool _isMaximized = false;

        private bool _isSelected = false;

        private DateTime? _lastActivationTimeStamp = null;

        [field: NonSerialized]
        private ILayoutContainer _previousContainer = null;

        [field: NonSerialized]
        private int _previousContainerIndex = -1;

        private object _toolTip = null;

        /// <summary>
        /// Class constructor
        /// </summary>
        internal LayoutContent()
        {
        }

        /// <summary>Event fired when the content is closed (i.e. removed definitely from the layout).</summary>
        public event EventHandler Closed;

        /// <summary>
        /// Event fired when the content is about to be closed (i.e. removed definitely from the layout)
        /// </summary>
        /// <remarks>Please note that <see cref="LayoutAnchorable"/> also can be hidden. Usually user hide anchorables when click the 'X' button. To completely close
        /// an anchorable the user should click the 'Close' menu item from the context menu. When an <see cref="LayoutAnchorable"/> is hidden its visibility changes to false and
        /// <see cref="LayoutAnchorable.IsHidden"/> property is set to true.
        /// Handle the Hiding event for the <see cref="LayoutAnchorable"/> to cancel the hide operation.</remarks>
        public event EventHandler<CancelEventArgs> Closing;

        /// <summary>
        /// Event fired when floating properties were updated.
        /// </summary>
        public event EventHandler FloatingPropertiesUpdated;

        public event EventHandler IsActiveChanged;

        public event EventHandler IsSelectedChanged;

        public bool CanClose
        {
            get => _canClose;
            set
            {
                if (_canClose == value)
                {
                    return;
                }

                _canClose = value;
                RaisePropertyChanged(nameof(_canClose));
            }
        }

        public bool CanFloat
        {
            get => _canFloat;
            set
            {
                if (value == _canFloat)
                {
                    return;
                }

                _canFloat = value;
                RaisePropertyChanged(nameof(CanFloat));
            }
        }

        /// <summary>
        /// Set to false to disable the behavior of auto-showing
        /// a <see cref="LayoutAnchorableControl"/> on mouse over.
        /// When true, hovering the mouse over an anchorable tab 
        /// will cause the anchorable to show itself.
        /// </summary>
        /// <remarks>Defaults to true</remarks>
        public bool CanShowOnHover
        {
            get => _canShowOnHover;
            set
            {
                if (value == _canShowOnHover)
                {
                    return;
                }

                _canShowOnHover = value;
                RaisePropertyChanged(nameof(CanShowOnHover));
            }
        }

        [XmlIgnore]
        public object Content
        {
            get => _content;
            set
            {
                if (value == _content)
                {
                    return;
                }

                RaisePropertyChanging(nameof(Content));
                _content = value;
                RaisePropertyChanged(nameof(Content));
                if (ContentId == null)
                {
                    SetContentIdFromContent();
                }
            }
        }

        public string ContentId
        {
            get
            {
                var value = (string)GetValue(ContentIdProperty);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
                // #83 - if Content.Name is empty at setting content and will be set later, ContentId will stay null.
                SetContentIdFromContent();
                return (string)GetValue(ContentIdProperty);
            }
            set => SetValue(ContentIdProperty, value);
        }

        public double FloatingHeight
        {
            get => _floatingHeight;
            set
            {
                if (value == _floatingHeight)
                {
                    return;
                }

                RaisePropertyChanging(nameof(FloatingHeight));
                _floatingHeight = value;
                RaisePropertyChanged(nameof(FloatingHeight));
            }
        }

        public double FloatingLeft
        {
            get => _floatingLeft;
            set
            {
                if (value == _floatingLeft)
                {
                    return;
                }

                RaisePropertyChanging(nameof(FloatingLeft));
                _floatingLeft = value;
                RaisePropertyChanged(nameof(FloatingLeft));
            }
        }

        public double FloatingTop
        {
            get => _floatingTop;
            set
            {
                if (value == _floatingTop)
                {
                    return;
                }

                RaisePropertyChanging(nameof(FloatingTop));
                _floatingTop = value;
                RaisePropertyChanged(nameof(FloatingTop));
            }
        }

        public double FloatingWidth
        {
            get => _floatingWidth;
            set
            {
                if (value == _floatingWidth)
                {
                    return;
                }

                RaisePropertyChanging(nameof(FloatingWidth));
                _floatingWidth = value;
                RaisePropertyChanged(nameof(FloatingWidth));
            }
        }

        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                if (value == _iconSource)
                {
                    return;
                }

                _iconSource = value;
                RaisePropertyChanged(nameof(IconSource));
            }
        }

        [XmlIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive)
                {
                    return;
                }

                RaisePropertyChanging(nameof(IsActive));
                var oldValue = _isActive;
                _isActive = value;
                var root = Root;
                if (root != null)
                {
                    if (root.ActiveContent != this && value)
                    {
                        Root.ActiveContent = this;
                    }

                    if (_isActive && root.ActiveContent != this)
                    {
                        root.ActiveContent = this;
                    }
                }
                if (_isActive)
                {
                    IsSelected = true;
                }

                OnIsActiveChanged(oldValue, value);
                RaisePropertyChanged(nameof(IsActive));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled)
                {
                    return;
                }

                _isEnabled = value;
                RaisePropertyChanged(nameof(IsEnabled));
            }
        }

        /// <summary>Gets whether the content is currently floating or not.</summary>
        [Bindable(true)]
        [Description("Gets whether the content is currently floating or not.")]
        [Category("Other")]
        public bool IsFloating => this.FindParent<LayoutFloatingWindow>() != null;

        public bool IsLastFocusedDocument
        {
            get => _isLastFocusedDocument;
            internal set
            {
                if (value == _isLastFocusedDocument)
                {
                    return;
                }

                RaisePropertyChanging(nameof(IsLastFocusedDocument));
                _isLastFocusedDocument = value;
                RaisePropertyChanged(nameof(IsLastFocusedDocument));
            }
        }

        public bool IsMaximized
        {
            get => _isMaximized;
            set
            {
                if (value == _isMaximized)
                {
                    return;
                }

                RaisePropertyChanging(nameof(IsMaximized));
                _isMaximized = value;
                RaisePropertyChanged(nameof(IsMaximized));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected)
                {
                    return;
                }

                var oldValue = _isSelected;
                RaisePropertyChanging(nameof(IsSelected));
                _isSelected = value;
                if (Parent is ILayoutContentSelector parentSelector)
                {
                    parentSelector.SelectedContentIndex = _isSelected ? parentSelector.IndexOf(this) : -1;
                }

                OnIsSelectedChanged(oldValue, value);
                RaisePropertyChanged(nameof(IsSelected));
                LayoutAnchorableTabItem.CancelMouseLeave();
            }
        }

        public DateTime? LastActivationTimeStamp
        {
            get => _lastActivationTimeStamp;
            set
            {
                if (value == _lastActivationTimeStamp)
                {
                    return;
                }

                _lastActivationTimeStamp = value;
                RaisePropertyChanged(nameof(LastActivationTimeStamp));
            }
        }

        [XmlIgnore]
        ILayoutContainer ILayoutPreviousContainer.PreviousContainer
        {
            get => _previousContainer;
            set
            {
                if (value == _previousContainer)
                {
                    return;
                }

                _previousContainer = value;
                RaisePropertyChanged(nameof(PreviousContainer));
                if (_previousContainer is ILayoutPaneSerializable paneSerializable && paneSerializable.Id == null)
                {
                    paneSerializable.Id = Guid.NewGuid().ToString();
                }
            }
        }

        [XmlIgnore]
        string ILayoutPreviousContainer.PreviousContainerId { get; set; }

        [XmlIgnore]
        public int PreviousContainerIndex
        {
            get => _previousContainerIndex;
            set
            {
                if (value == _previousContainerIndex)
                {
                    return;
                }

                _previousContainerIndex = value;
                RaisePropertyChanged(nameof(PreviousContainerIndex));
            }
        }

        public LayoutDocumentTabItem TabItem { get; internal set; }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public object ToolTip
        {
            get => _toolTip;
            set
            {
                if (value == _toolTip)
                {
                    return;
                }

                _toolTip = value;
                RaisePropertyChanged(nameof(ToolTip));
            }
        }

        protected ILayoutContainer PreviousContainer
        {
            get => ((ILayoutPreviousContainer)this).PreviousContainer;
            set => ((ILayoutPreviousContainer)this).PreviousContainer = value;
        }

        protected string PreviousContainerId
        {
            get => ((ILayoutPreviousContainer)this).PreviousContainerId;
            set => ((ILayoutPreviousContainer)this).PreviousContainerId = value;
        }

        /// <summary>Close the content</summary>
        /// <remarks>Note that the anchorable is only hidden (not closed). By default when user click the X button it only hides the content.</remarks>
        public abstract void Close();

        public int CompareTo(LayoutContent other)
        {
            if (Content is IComparable contentAsComparable)
            {
                return contentAsComparable.CompareTo(other.Content);
            }

            return string.Compare(Title, other.Title);
        }

        /// <summary>Re-dock the content to its previous container</summary>
        public void Dock()
        {
            if (PreviousContainer != null)
            {
                var currentContainer = Parent;
                var currentContainerIndex = currentContainer is ILayoutGroup ? (currentContainer as ILayoutGroup).IndexOfChild(this) : -1;
                var previousContainerAsLayoutGroup = PreviousContainer as ILayoutGroup;

                if (PreviousContainerIndex < previousContainerAsLayoutGroup.ChildrenCount)
                {
                    previousContainerAsLayoutGroup.InsertChildAt(PreviousContainerIndex, this);
                }
                else
                {
                    previousContainerAsLayoutGroup.InsertChildAt(previousContainerAsLayoutGroup.ChildrenCount, this);
                }

                if (currentContainerIndex > -1)
                {
                    PreviousContainer = currentContainer;
                    PreviousContainerIndex = currentContainerIndex;
                }
                else
                {
                    PreviousContainer = null;
                    PreviousContainerIndex = 0;
                }

                IsSelected = true;
                IsActive = true;
            }
            else
            {
                InternalDock();
            }

            Root.CollectGarbage();

            // BD: 14.08.2020 raise IsFloating property changed
            RaisePropertyChanged(nameof(IsFloating));
        }

        /// <summary>Dock the content as document.</summary>
        public void DockAsDocument()
        {
            if (Root is not LayoutRoot root)
            {
                throw new InvalidOperationException();
            }

            if (PreviousContainer is LayoutDocumentPane)
            {
                Dock();
                return;
            }

            LayoutDocumentPane newParentPane;
            if (root.LastFocusedDocument != null)
            {
                newParentPane = root.LastFocusedDocument.Parent as LayoutDocumentPane;
            }
            else
            {
                newParentPane = root.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            }

            if (newParentPane != null)
            {
                newParentPane.Children.Add(this);
                root.CollectGarbage();
            }
            IsSelected = true;
            IsActive = true;

            // BD: 14.08.2020 raise IsFloating property changed
            RaisePropertyChanged(nameof(IsFloating));
        }

        /// <summary>Float the content in a popup window</summary>
        public void Float()
        {
            if (PreviousContainer != null && PreviousContainer.FindParent<LayoutFloatingWindow>() != null)
            {
                var currentContainer = Parent as ILayoutPane;
                var currentContainerIndex = (currentContainer as ILayoutGroup).IndexOfChild(this);
                var previousContainerAsLayoutGroup = PreviousContainer as ILayoutGroup;

                if (PreviousContainerIndex < previousContainerAsLayoutGroup.ChildrenCount)
                {
                    previousContainerAsLayoutGroup.InsertChildAt(PreviousContainerIndex, this);
                }
                else
                {
                    previousContainerAsLayoutGroup.InsertChildAt(previousContainerAsLayoutGroup.ChildrenCount, this);
                }

                PreviousContainer = currentContainer;
                PreviousContainerIndex = currentContainerIndex;
                IsSelected = true;
                IsActive = true;
                Root.CollectGarbage();
            }
            else
            {
                Root.Manager.StartDraggingFloatingWindowForContent(this, false);
                IsSelected = true;
                IsActive = true;
            }

            // BD: 14.08.2020 raise IsFloating property changed
            RaisePropertyChanged(nameof(IsFloating));
        }

        /// <inheritdoc />
        public System.Xml.Schema.XmlSchema GetSchema() => null;

        void ILayoutElementForFloatingWindow.RaiseFloatingPropertiesUpdated() => FloatingPropertiesUpdated?.Invoke(this, EventArgs.Empty);

        /// <inheritdoc />
        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.MoveToAttribute(nameof(Title)))
            {
                Title = reader.Value;
            }
            //if (reader.MoveToAttribute("IconSource"))
            //    IconSource = new Uri(reader.Value, UriKind.RelativeOrAbsolute);

            if (reader.MoveToAttribute(nameof(IsSelected)))
            {
                IsSelected = bool.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(ContentId)))
            {
                ContentId = reader.Value;
            }

            if (reader.MoveToAttribute(nameof(IsLastFocusedDocument)))
            {
                IsLastFocusedDocument = bool.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(PreviousContainerId)))
            {
                PreviousContainerId = reader.Value;
            }

            if (reader.MoveToAttribute(nameof(PreviousContainerIndex)))
            {
                PreviousContainerIndex = int.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(FloatingLeft)))
            {
                FloatingLeft = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.MoveToAttribute(nameof(FloatingTop)))
            {
                FloatingTop = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.MoveToAttribute(nameof(FloatingWidth)))
            {
                FloatingWidth = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.MoveToAttribute(nameof(FloatingHeight)))
            {
                FloatingHeight = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.MoveToAttribute(nameof(IsMaximized)))
            {
                IsMaximized = bool.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(CanClose)))
            {
                CanClose = bool.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(CanFloat)))
            {
                CanFloat = bool.Parse(reader.Value);
            }

            if (reader.MoveToAttribute(nameof(LastActivationTimeStamp)))
            {
                LastActivationTimeStamp = DateTime.Parse(reader.Value, CultureInfo.InvariantCulture);
            }

            if (reader.MoveToAttribute(nameof(CanShowOnHover)))
            {
                CanShowOnHover = bool.Parse(reader.Value);
            }

            reader.Read();
        }

        /// <inheritdoc />
        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(Title))
            {
                writer.WriteAttributeString(nameof(Title), Title);
            }

            //if (IconSource != null)
            //    writer.WriteAttributeString("IconSource", IconSource.ToString());

            if (IsSelected)
            {
                writer.WriteAttributeString(nameof(IsSelected), IsSelected.ToString());
            }

            if (IsLastFocusedDocument)
            {
                writer.WriteAttributeString(nameof(IsLastFocusedDocument), IsLastFocusedDocument.ToString());
            }

            if (!string.IsNullOrWhiteSpace(ContentId))
            {
                writer.WriteAttributeString(nameof(ContentId), ContentId);
            }

            if (ToolTip is string toolTip && !string.IsNullOrWhiteSpace(toolTip))
            {
                writer.WriteAttributeString(nameof(ToolTip), toolTip);
            }

            if (FloatingLeft != 0.0)
            {
                writer.WriteAttributeString(nameof(FloatingLeft), FloatingLeft.ToString(CultureInfo.InvariantCulture));
            }

            if (FloatingTop != 0.0)
            {
                writer.WriteAttributeString(nameof(FloatingTop), FloatingTop.ToString(CultureInfo.InvariantCulture));
            }

            if (FloatingWidth != 0.0)
            {
                writer.WriteAttributeString(nameof(FloatingWidth), FloatingWidth.ToString(CultureInfo.InvariantCulture));
            }

            if (FloatingHeight != 0.0)
            {
                writer.WriteAttributeString(nameof(FloatingHeight), FloatingHeight.ToString(CultureInfo.InvariantCulture));
            }

            if (IsMaximized)
            {
                writer.WriteAttributeString(nameof(IsMaximized), IsMaximized.ToString());
            }
            // BD: 14.08.2020 changed to check CanClose value against the default in _canCloseDefault
            //     thus CanClose property will be serialized only when not equal to its default for given class
            //     With previous code it was not possible to serialize CanClose if set to true for LayoutAnchorable instance
            if (CanClose != CanCloseDefault)
            {
                writer.WriteAttributeString(nameof(CanClose), CanClose.ToString());
            }

            if (!CanFloat)
            {
                writer.WriteAttributeString(nameof(CanFloat), CanFloat.ToString());
            }

            if (LastActivationTimeStamp != null)
            {
                writer.WriteAttributeString(nameof(LastActivationTimeStamp), LastActivationTimeStamp.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (!CanShowOnHover)
            {
                writer.WriteAttributeString(nameof(CanShowOnHover), CanShowOnHover.ToString());
            }

            if (_previousContainer is ILayoutPaneSerializable paneSerializable)
            {
                writer.WriteAttributeString("PreviousContainerId", paneSerializable.Id);
                writer.WriteAttributeString("PreviousContainerIndex", _previousContainerIndex.ToString());
            }
        }

        internal void CloseInternal()
        {
            var root = Root;
            var parentAsContainer = Parent;

            if (PreviousContainer == null)
            {
                var parentAsGroup = Parent as ILayoutGroup;
                PreviousContainer = parentAsContainer;
                PreviousContainerIndex = parentAsGroup.IndexOfChild(this);

                if (parentAsGroup is ILayoutPaneSerializable layoutPaneSerializable)
                {
                    PreviousContainerId = layoutPaneSerializable.Id;
                    // This parentAsGroup will be removed in the GarbageCollection below
                    if (parentAsGroup.Children.Count() == 1 && parentAsGroup.Parent != null && Root.Manager != null)
                    {
                        Parent = Root.Manager.Layout;
                        PreviousContainer = parentAsGroup.Parent;
                        PreviousContainerIndex = -1;

                        if (parentAsGroup.Parent is ILayoutPaneSerializable paneSerializable)
                        {
                            PreviousContainerId = paneSerializable.Id;
                        }
                        else
                        {
                            PreviousContainerId = null;
                        }
                    }
                }
            }

            parentAsContainer.RemoveChild(this);
            root?.CollectGarbage();
            OnClosed();
        }

        /// <summary>Test if the content can be closed. </summary>
        /// <returns></returns>
        internal bool TestCanClose()
        {
            var args = new CancelEventArgs();
            OnClosing(args);
            return !args.Cancel;
        }

        protected virtual void InternalDock()
        {
        }

        protected virtual void OnClosed() => Closed?.Invoke(this, EventArgs.Empty);

        protected virtual void OnClosing(CancelEventArgs args) => Closing?.Invoke(this, args);

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the <see cref="IsActive"/> property.
        /// </summary>
        protected virtual void OnIsActiveChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                LastActivationTimeStamp = DateTime.Now;
            }

            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the <see cref="IsSelected"/> property.
        /// </summary>
        protected virtual void OnIsSelectedChanged(bool oldValue, bool newValue) => IsSelectedChanged?.Invoke(this, EventArgs.Empty);

        /// <inheritdoc />
        protected override void OnParentChanged(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
            if (IsSelected && Parent is ILayoutContentSelector)
            {
                var parentSelector = Parent as ILayoutContentSelector;
                parentSelector.SelectedContentIndex = parentSelector.IndexOf(this);
            }

            base.OnParentChanged(oldValue, newValue);
        }

        /// <inheritdoc />
        protected override void OnParentChanging(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
            if (oldValue != null)
            {
                IsSelected = false;
            }

            base.OnParentChanging(oldValue, newValue);
        }

        private static object CoerceTitleValue(DependencyObject obj, object value)
        {
            var lc = (LayoutContent)obj;
            if ((string)value != lc.Title)
            {
                lc.RaisePropertyChanging(TitleProperty.Name);
            }

            return value;
        }

        private static void OnContentIdPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is LayoutContent layoutContent)
            {
                layoutContent.OnContentIdPropertyChanged((string)args.OldValue, (string)args.NewValue);
            }
        }

        private static void OnTitlePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) => ((LayoutContent)obj).RaisePropertyChanged(TitleProperty.Name);
        private void OnContentIdPropertyChanged(string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                RaisePropertyChanged(nameof(ContentId));
            }
        }

        private void SetContentIdFromContent()
        {
            var contentAsControl = _content as FrameworkElement;
            if (!string.IsNullOrWhiteSpace(contentAsControl?.Name))
            {
                SetCurrentValue(ContentIdProperty, contentAsControl.Name);
            }
        }
    }
}
