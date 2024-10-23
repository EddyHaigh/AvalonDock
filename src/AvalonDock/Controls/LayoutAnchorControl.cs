/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using AvalonDock.Layout;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Implements a control that is displayed when a <see cref="LayoutAnchorableControl"/>
    /// is in AutoHide mode (which can be applied via context menu drop entry or click on the Pin symbol.
    /// </summary>
    public class LayoutAnchorControl : Control, ILayoutControl
    {
        public static readonly DependencyProperty SideProperty;

        /// <summary>
        /// Side Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey SidePropertyKey
            = DependencyProperty.RegisterReadOnly(
                "Side",
                typeof(AnchorSide),
                typeof(LayoutAnchorControl),
                new FrameworkPropertyMetadata((AnchorSide)AnchorSide.Left));

        private LayoutAnchorable _model;
        private DispatcherTimer _openUpTimer = null;

        static LayoutAnchorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorControl), new FrameworkPropertyMetadata(typeof(LayoutAnchorControl)));
            Control.IsHitTestVisibleProperty.AddOwner(typeof(LayoutAnchorControl), new FrameworkPropertyMetadata(true));
            SideProperty = SidePropertyKey.DependencyProperty;
        }

        internal LayoutAnchorControl(LayoutAnchorable model)
        {
            _model = model;
            _model.IsActiveChanged += new EventHandler(ModelIsActiveChanged);
            _model.IsSelectedChanged += new EventHandler(ModelIsSelectedChanged);

            SetSide(_model.FindParent<LayoutAnchorSide>().Side);
        }

        public ILayoutElement Model
        {
            get
            {
                return _model;
            }
        }

        /// <summary>Gets the anchor side of the control.</summary>
        [Bindable(true)]
        [Description("Gets the anchor side of the control.")]
        [Category("Anchor")]
        public AnchorSide Side
        {
            get
            {
                return (AnchorSide)GetValue(SideProperty);
            }
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (!e.Handled)
            {
                _model.Root.Manager.ShowAutoHideWindow(this);
                _model.IsActive = true;
            }
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            // If the model wants to auto-show itself on hover then initiate the show action
            if (!e.Handled && _model.CanShowOnHover)
            {
                _openUpTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
                _openUpTimer.Interval = TimeSpan.FromMilliseconds(400);
                _openUpTimer.Tick += new EventHandler(OpenUpTimerTick);
                _openUpTimer.Start();
            }
        }

        //protected override void OnVisualParentChanged(DependencyObject oldParent)
        //{
        //    base.OnVisualParentChanged(oldParent);

        //    var contentModel = _model;

        //    if (oldParent != null && contentModel != null && contentModel.Content is UIElement)
        //    {
        //        var oldParentPaneControl = oldParent.FindVisualAncestor<LayoutAnchorablePaneControl>();
        //        if (oldParentPaneControl != null)
        //        {
        //            ((ILogicalChildrenContainer)oldParentPaneControl).InternalRemoveLogicalChild(contentModel.Content);
        //        }
        //    }

        //    if (contentModel.Content != null && contentModel.Content is UIElement)
        //    {
        //        var oldLogicalParentPaneControl = LogicalTreeHelper.GetParent(contentModel.Content as UIElement)
        //            as ILogicalChildrenContainer;
        //        if (oldLogicalParentPaneControl != null)
        //            oldLogicalParentPaneControl.InternalRemoveLogicalChild(contentModel.Content);
        //    }

        //    if (contentModel != null && contentModel.Content != null && contentModel.Root != null && contentModel.Content is UIElement)
        //    {
        //        ((ILogicalChildrenContainer)contentModel.Root.Manager).InternalAddLogicalChild(contentModel.Content);
        //    }
        //}

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            if (_openUpTimer != null)
            {
                _openUpTimer.Tick -= new EventHandler(OpenUpTimerTick);
                _openUpTimer.Stop();
                _openUpTimer = null;
            }
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Provides a secure method for setting the Side property.
        /// This dependency property indicates the anchor side of the control.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetSide(AnchorSide value)
        {
            SetValue(SidePropertyKey, value);
        }


        private void ModelIsActiveChanged(object sender, EventArgs e)
        {
            if (!_model.IsAutoHidden)
            {
                _model.IsActiveChanged -= new EventHandler(ModelIsActiveChanged);
            }
            else if (_model.IsActive)
            {
                _model.Root.Manager.ShowAutoHideWindow(this);
            }
        }

        private void ModelIsSelectedChanged(object sender, EventArgs e)
        {
            if (!_model.IsAutoHidden)
            {
                _model.IsSelectedChanged -= new EventHandler(ModelIsSelectedChanged);
            }
            else if (_model.IsSelected)
            {
                _model.Root.Manager.ShowAutoHideWindow(this);
                _model.IsSelected = false;
            }
        }
        private void OpenUpTimerTick(object sender, EventArgs e)
        {
            _openUpTimer.Tick -= new EventHandler(OpenUpTimerTick);
            _openUpTimer.Stop();
            _openUpTimer = null;
            _model.Root.Manager.ShowAutoHideWindow(this);
        }
    }
}