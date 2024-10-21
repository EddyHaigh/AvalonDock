﻿ /************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System.Linq;
using System.Windows;
using System.Windows.Media;

using AvalonDock.Layout;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Implements a <see cref="LayoutDocumentPaneGroupControl"/> drop target
    /// on which other items (<see cref="LayoutDocument"/>) can be dropped.
    /// </summary>
    internal class DocumentPaneGroupDropTarget : DropTarget<LayoutDocumentPaneGroupControl>
    {
        private LayoutDocumentPaneGroupControl _targetPane;

        /// <summary>
        /// Class contructor
        /// </summary>
        /// <param name="paneControl"></param>
        /// <param name="detectionRect"></param>
        /// <param name="type"></param>
        internal DocumentPaneGroupDropTarget(
            LayoutDocumentPaneGroupControl paneControl,
            Rect detectionRect,
            DropTargetType type)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
        }

        /// <summary>
        /// Gets a <see cref="Geometry"/> that is used to highlight/preview the docking position
        /// of this drop target for a <paramref name="floatingWindow"/> being docked inside an
        /// <paramref name="overlayWindow"/>.
        /// </summary>
        /// <param name="overlayWindow"></param>
        /// <param name="floatingWindow"></param>
        /// <returns>The geometry of the preview/highlighting WPF figure path.</returns>
        public override Geometry GetPreviewPath(
            OverlayWindow overlayWindow,
            LayoutFloatingWindow floatingWindow)
        {
            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:
                    {
                        var targetScreenRect = TargetElement.GetScreenArea();
                        targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                        return new RectangleGeometry(targetScreenRect);
                    }
            }

            return null;
        }

        /// <summary>
        /// Method is invoked to complete a drag & drop operation with a (new) docking position
        /// by docking of the LayoutDocument <paramref name="floatingWindow"/> into this drop target.
        /// </summary>
        /// <param name="floatingWindow"></param>
        protected override void Drop(LayoutDocumentFloatingWindow floatingWindow)
        {
            ILayoutPane targetModel = _targetPane.Model as ILayoutPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:
                    {
                        var paneGroupModel = targetModel as LayoutDocumentPaneGroup;
                        paneGroupModel.Children.Insert(0, floatingWindow.RootPanel);
                    }
                    break;
            }

            base.Drop(floatingWindow);
        }

        /// <summary>
        /// Method is invoked to complete a drag & drop operation with a (new) docking position
        /// by docking of the LayoutAnchorable <paramref name="floatingWindow"/> into this drop target.
        /// </summary>
        /// <param name="floatingWindow"></param>
        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            ILayoutPane targetModel = _targetPane.Model as ILayoutPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:
                    {
                        var paneGroupModel = targetModel as LayoutDocumentPaneGroup;
                        var paneModel = paneGroupModel.Children[0] as LayoutDocumentPane;

                        int i = 0;
                        foreach (var anchorableToImport in floatingWindow.RootPanel.Descendents().OfType<LayoutAnchorable>().ToArray())
                        {
                            // BD: 18.07.2020 Remove that bodge and handle CanClose=false && CanHide=true in XAML
                            //anchorableToImport.SetCanCloseInternal(true);

                            paneModel.Children.Insert(i, anchorableToImport);
                            i++;
                        }
                    }
                    break;
            }

            base.Drop(floatingWindow);
        }
    }
}
