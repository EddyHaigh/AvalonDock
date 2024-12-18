﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Markup;

namespace AvalonDock.Layout
{
    /// <summary>
    /// Implements an element in the layout model tree that can contain and arrange multiple
    /// <see cref="LayoutAnchorablePane"/> elements in x or y directions, which in turn contain
    /// <see cref="LayoutAnchorable"/> elements.
    /// </summary>
    [ContentProperty(nameof(Children))]
    [Serializable]
    public class LayoutAnchorablePaneGroup : LayoutPositionableGroup<ILayoutAnchorablePane>, ILayoutAnchorablePane, ILayoutOrientableGroup
    {
        private Orientation _orientation;

        /// <summary>Class constructor</summary>
        public LayoutAnchorablePaneGroup()
        {
        }

        /// <summary>Class constructor <paramref name="firstChild"/> to be inserted into collection of children models.</summary>
        public LayoutAnchorablePaneGroup(LayoutAnchorablePane firstChild)
        {
            Children.Add(firstChild);
        }

        /// <summary>
        /// Gets/sets the <see cref="System.Windows.Controls.Orientation"/> of this object.
        /// </summary>
        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (value == _orientation)
                {
                    return;
                }

                RaisePropertyChanging(nameof(Orientation));
                _orientation = value;
                RaisePropertyChanged(nameof(Orientation));
            }
        }

#if TRACE
        /// <inheritdoc />
        public override void ConsoleDump(int tab)
        {
            System.Diagnostics.Trace.TraceInformation("{0}AnchorablePaneGroup({1})", new string(' ', tab * 4), Orientation);

            foreach (var child in Children.Cast<LayoutElement>())
            {
                child.ConsoleDump(tab + 1);
            }
        }
#endif

        /// <inheritdoc />
        public override void ReadXml(System.Xml.XmlReader reader)
        {
            if (reader.MoveToAttribute(nameof(Orientation)))
            {
                Orientation = (Orientation)Enum.Parse(typeof(Orientation), reader.Value, true);
            }

            base.ReadXml(reader);
        }

        /// <inheritdoc />
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(Orientation), Orientation.ToString());
            base.WriteXml(writer);
        }

        /// <inheritdoc />
        protected override bool GetVisibility() => Children.Count > 0 && Children.Any(c => c.IsVisible);

        /// <inheritdoc />
        protected override void OnChildrenCollectionChanged()
        {
            if (DockWidth.IsAbsolute && ChildrenCount == 1)
            {
                ((ILayoutPositionableElement)Children[0]).DockWidth = DockWidth;
            }

            if (DockHeight.IsAbsolute && ChildrenCount == 1)
            {
                ((ILayoutPositionableElement)Children[0]).DockHeight = DockHeight;
            }

            base.OnChildrenCollectionChanged();
        }

        /// <inheritdoc />
        protected override void OnDockHeightChanged()
        {
            if (DockHeight.IsAbsolute && ChildrenCount == 1)
            {
                ((ILayoutPositionableElement)Children[0]).DockHeight = DockHeight;
            }

            base.OnDockHeightChanged();
        }

        /// <inheritdoc />
        protected override void OnDockWidthChanged()
        {
            if (DockWidth.IsAbsolute && ChildrenCount == 1)
            {
                ((ILayoutPositionableElement)Children[0]).DockWidth = DockWidth;
            }

            base.OnDockWidthChanged();
        }

        /// <inheritdoc />
        protected override void OnIsVisibleChanged()
        {
            UpdateParentVisibility();
            base.OnIsVisibleChanged();
        }

        private void UpdateParentVisibility()
        {
            if (Parent is ILayoutElementWithVisibility parentPane)
            {
                parentPane.ComputeVisibility();
            }
        }
    }
}