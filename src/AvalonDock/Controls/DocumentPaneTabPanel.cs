/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using AvalonDock.Layout;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Provides a panel that contains the TabItem Headers of the <see cref="LayoutDocumentPaneControl"/>.
    /// </summary>
    public class DocumentPaneTabPanel : Panel
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        public DocumentPaneTabPanel()
        {
            this.FlowDirection = System.Windows.FlowDirection.LeftToRight;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var visibleChildren = Children.Cast<TabItem>().Where(ch => ch.Visibility != Visibility.Collapsed);
            var offset = 0.0;
            var skipAllOthers = false;
            foreach (TabItem doc in visibleChildren)
            {
                if (skipAllOthers || offset + doc.DesiredSize.Width > finalSize.Width)
                {
                    bool isLayoutContentSelected = false;
                    var layoutContent = doc.Content as LayoutContent;

                    if (layoutContent != null)
                    {
                        isLayoutContentSelected = layoutContent.IsSelected;
                    }

                    if (isLayoutContentSelected && !doc.IsVisible)
                    {
                        var parentSelector = layoutContent.Parent as ILayoutContentSelector;
                        var parentPane = layoutContent.Parent as ILayoutPane;
                        int contentIndex = parentSelector.IndexOf(layoutContent);
                        if (contentIndex > 0 &&
                            layoutContent.Parent.ChildrenCount > 1)
                        {
                            parentPane.MoveChild(contentIndex, 0);
                            parentSelector.SelectedContentIndex = 0;
                            return ArrangeOverride(finalSize);
                        }
                    }
                    doc.Visibility = System.Windows.Visibility.Hidden;
                    skipAllOthers = true;
                }
                else
                {
                    doc.Visibility = System.Windows.Visibility.Visible;
                    doc.Arrange(new Rect(offset, 0.0, doc.DesiredSize.Width, finalSize.Height));
                    offset += doc.ActualWidth + doc.Margin.Left + doc.Margin.Right;
                }
            }
            return finalSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desideredSize = new Size();
            foreach (FrameworkElement child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                desideredSize.Width += child.DesiredSize.Width;

                desideredSize.Height = Math.Max(desideredSize.Height, child.DesiredSize.Height);
            }

            return new Size(Math.Min(desideredSize.Width, availableSize.Width), desideredSize.Height);
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            //if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
            //    LayoutDocumentTabItem.IsDraggingItem())
            //{
            //    var contentModel = LayoutDocumentTabItem.GetDraggingItem().Model;
            //    var manager = contentModel.Root.Manager;
            //    LayoutDocumentTabItem.ResetDraggingItem();
            //    System.Diagnostics.Trace.TraceInformation("OnMouseLeave()");

            //    manager.StartDraggingFloatingWindowForContent(contentModel);
            //}

            base.OnMouseLeave(e);
        }
    }
}