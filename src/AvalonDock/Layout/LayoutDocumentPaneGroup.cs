﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Markup;

namespace AvalonDock.Layout
{
    /// <summary>
    /// Implements an element in the layout model that can contain and organize multiple
    /// <see cref="LayoutDocumentPane"/> elements, which in turn contain <see cref="LayoutDocument"/> elements.
    /// </summary>
    [ContentProperty(nameof(Children))]
    [Serializable]
    public class LayoutDocumentPaneGroup : LayoutPositionableGroup<ILayoutDocumentPane>, ILayoutDocumentPane, ILayoutOrientableGroup
    {
        private Orientation _orientation;

        /// <summary>Class constructor</summary>
        public LayoutDocumentPaneGroup()
        {
        }

        /// <summary>Class constructor from <paramref name="documentPane"/> that is added into the children collection of this object.</summary>
        public LayoutDocumentPaneGroup(LayoutDocumentPane documentPane)
        {
            Children.Add(documentPane);
        }

        /// <summary>Gets/sets the (Horizontal, Vertical) <see cref="System.Windows.Controls.Orientation"/> of this group.</summary>
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
            System.Diagnostics.Trace.TraceInformation("{0}DocumentPaneGroup({1})", new string(' ', tab * 4), Orientation);

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
        protected override bool GetVisibility() => true;
    }
}