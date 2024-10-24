﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Windows;
using System.Windows.Controls;

using AvalonDock.Layout;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Implements a <see cref="Grid"/> based panel base class
    /// that hosts a <see cref="LayoutPanel"/> as its model.
    /// </summary>
    public class LayoutPanelControl : LayoutGridControl<ILayoutPanelElement>
    {
        private readonly LayoutPanel _model;

        internal LayoutPanelControl(LayoutPanel model)
            : base(model, model.Orientation)
        {
            _model = model;
        }

        protected override void OnFixChildrenDockLengths()
        {
            if (ActualWidth == 0.0 || ActualHeight == 0.0)
            {
                return;
            }

            // Setup DockWidth/Height for children

            if (_model.Orientation == Orientation.Horizontal)
            {
                if (_model.ContainsChildOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>())
                {
                    for (var i = 0; i < _model.Children.Count; i++)
                    {
                        if (_model.Children[i] is ILayoutContainer layoutContainer
                            && (layoutContainer.IsOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>()
                            || layoutContainer.ContainsChildOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>()))
                        {
                            // Keep set values (from XML for instance)
                            if (_model.Children[i] is ILayoutPositionableElement layoutPositionableElement
                                && !layoutPositionableElement.DockWidth.IsStar)
                            {
                                layoutPositionableElement.DockWidth = new GridLength(1.0, GridUnitType.Star);
                            }
                        }
                        else if (_model.Children[i] is ILayoutPositionableElement layoutPositionableElement
                            && layoutPositionableElement.DockWidth.IsStar)
                        {
                            var childPositionableModelWidthActualSize = layoutPositionableElement as ILayoutPositionableElementWithActualSize;
                            var childDockMinWidth = layoutPositionableElement.CalculatedDockMinWidth();
                            var widthToSet = Math.Max(childPositionableModelWidthActualSize.ActualWidth, childDockMinWidth);

                            widthToSet = Math.Min(widthToSet, ActualWidth / 2.0);
                            widthToSet = Math.Max(widthToSet, childDockMinWidth);
                            layoutPositionableElement.DockWidth = new GridLength(widthToSet, GridUnitType.Pixel);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < _model.Children.Count; i++)
                    {
                        var childPositionableModel = _model.Children[i] as ILayoutPositionableElement;
                        if (!childPositionableModel.DockWidth.IsStar)
                        {
                            // Keep set values (from XML for instance)
                            if (!childPositionableModel.DockWidth.IsStar)
                            {
                                childPositionableModel.DockWidth = new GridLength(1.0, GridUnitType.Star);
                            }
                        }
                    }
                }
            }
            else // Vertical
            {
                if (_model.ContainsChildOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>())
                {
                    for (var i = 0; i < _model.Children.Count; i++)
                    {
                        if (_model.Children[i] is ILayoutContainer childContainerModel &&
                            (childContainerModel.IsOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>() ||
                             childContainerModel.ContainsChildOfType<LayoutDocumentPane, LayoutDocumentPaneGroup>()))
                        {
                            // Keep set values (from XML for instance)
                            if (!(_model.Children[i] as ILayoutPositionableElement).DockHeight.IsStar)
                            {
                                (_model.Children[i] as ILayoutPositionableElement).DockHeight = new GridLength(1.0, GridUnitType.Star);
                            }
                        }
                        else if (_model.Children[i] is ILayoutPositionableElement layoutPositionableElement
                            && layoutPositionableElement.DockHeight.IsStar)
                        {
                            var childPositionableModelWidthActualSize = layoutPositionableElement as ILayoutPositionableElementWithActualSize;
                            var childDockMinHeight = layoutPositionableElement.CalculatedDockMinHeight();
                            var heightToSet = Math.Max(childPositionableModelWidthActualSize.ActualHeight, childDockMinHeight);
                            heightToSet = Math.Min(heightToSet, ActualHeight / 2.0);
                            heightToSet = Math.Max(heightToSet, childDockMinHeight);
                            layoutPositionableElement.DockHeight = new GridLength(heightToSet, GridUnitType.Pixel);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < _model.Children.Count; i++)
                    {
                        var childPositionableModel = _model.Children[i] as ILayoutPositionableElement;
                        if (!childPositionableModel.DockHeight.IsStar)
                        {
                            // Keep set values (from XML for instance)
                            if (!childPositionableModel.DockHeight.IsStar)
                            {
                                childPositionableModel.DockHeight = new GridLength(1.0, GridUnitType.Star);
                            }
                        }
                    }
                }
            }
        }
    }
}