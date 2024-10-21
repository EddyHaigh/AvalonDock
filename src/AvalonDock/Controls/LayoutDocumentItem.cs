/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System.ComponentModel;
using System.Windows;

using AvalonDock.Layout;

namespace AvalonDock.Controls
{
    /// <inheritdoc />
    /// <summary>
    /// This is a wrapper for around the custom document content view of <see cref="LayoutElement"/>.
    /// Implements the <see cref="AvalonDock.Controls.LayoutItem" />
    ///
    /// All DPs implemented here can be bound in a corresponding style to control parameters
    /// in dependency properties via binding in MVVM.
    /// </summary>
    /// <seealso cref="AvalonDock.Controls.LayoutItem" />
    public class LayoutDocumentItem : LayoutItem
    {
        /// <summary><see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty
            = DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(LayoutDocumentItem),
                new FrameworkPropertyMetadata(null, OnDescriptionChanged));

        private LayoutDocument _document;   // The content of this item

        /// <summary>Class constructor</summary>
        internal LayoutDocumentItem()
        {
        }

        /// <summary>Gets/sets the description to display (in the <see cref="NavigatorWindow"/>) for the document item.</summary>
        [Bindable(true)]
        [Description("Gets/sets the description to display (in the NavigatorWindow) for the document item.")]
        [Category("Other")]
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <inheritdoc />
        internal override void Attach(LayoutContent model)
        {
            _document = model as LayoutDocument;
            base.Attach(model);
        }

        /// <inheritdoc />
        internal override void Detach()
        {
            _document = null;
            base.Detach();
        }

        protected override bool CanExecuteDockAsDocumentCommand()
        {
            return (LayoutElement != null && LayoutElement.FindParent<LayoutDocumentPane>() != null && LayoutElement.IsFloating);
        }

        /// <inheritdoc />
        protected override void Close()
        {
            if (_document.Root?.Manager == null)
            {
                return;
            }

            var dockingManager = _document.Root.Manager;
            dockingManager.ExecuteCloseCommand(_document);
        }

        /// <summary>Provides derived classes an opportunity to handle changes to the <see cref="Description"/> property.</summary>
        protected virtual void OnDescriptionChanged(DependencyPropertyChangedEventArgs e)
            => _document.Description = (string)e.NewValue;

        /// <inheritdoc />
        protected override void OnVisibilityChanged()
        {
            if (_document?.Root != null)
            {
                _document.IsVisible = Visibility == Visibility.Visible;
                if (_document.Parent is LayoutDocumentPane layoutDocumentPane)
                {
                    layoutDocumentPane.ComputeVisibility();
                }
            }
            base.OnVisibilityChanged();
        }

        /// <summary>Handles changes to the <see cref="Description"/> property.</summary>
        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((LayoutDocumentItem)d).OnDescriptionChanged(e);
    }
}