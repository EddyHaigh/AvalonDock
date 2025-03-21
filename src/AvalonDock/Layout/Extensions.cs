﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AvalonDock.Layout
{
    /// <summary>Provides extension methods for WPF specific (Visual Tree) capabilities.</summary>
    public static class Extensions
    {
        public static IEnumerable<ILayoutElement> Descendents(this ILayoutElement element)
        {
            if (element is not ILayoutContainer container)
            {
                yield break;
            }

            foreach (var childElement in container.Children)
            {
                yield return childElement;
                foreach (var childChildElement in childElement.Descendents())
                {
                    yield return childChildElement;
                }
            }
        }

        public static T FindParent<T>(this ILayoutElement element) //where T : ILayoutContainer
        {
            var parent = element.Parent;
            while (parent != null && parent is not T)
            {
                parent = parent.Parent;
            }

            return (T)parent;
        }

        public static ILayoutRoot GetRoot(this ILayoutElement element) //where T : ILayoutContainer
        {
            if (element is ILayoutRoot layoutRoot)
            {
                return layoutRoot;
            }

            var parent = element.Parent;
            while (parent != null && parent is not ILayoutRoot)
            {
                parent = parent.Parent;
            }

            return (ILayoutRoot)parent;
        }

        public static bool ContainsChildOfType<T>(this ILayoutContainer element)
        {
            foreach (var childElement in element.Descendents())
            {
                if (childElement is T)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsChildOfType<T, S>(this ILayoutContainer container)
        {
            foreach (var childElement in container.Descendents())
            {
                if (childElement is T || childElement is S)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsOfType<T, S>(this ILayoutContainer container) => container is T || container is S;

        public static AnchorSide GetSide(this ILayoutElement element)
        {
            if (element.Parent is ILayoutOrientableGroup parentContainer)
            {
                var layoutPanel = parentContainer as LayoutPanel ?? parentContainer.FindParent<LayoutPanel>();
                if (layoutPanel != null && layoutPanel.Children.Count > 0)
                {
                    if (layoutPanel.Orientation == System.Windows.Controls.Orientation.Horizontal)
                    {
                        return layoutPanel.Children[0].Equals(element) || layoutPanel.Children[0].Descendents().Contains(element) ? AnchorSide.Left : AnchorSide.Right;
                    }

                    return layoutPanel.Children[0].Equals(element) || layoutPanel.Children[0].Descendents().Contains(element) ? AnchorSide.Top : AnchorSide.Bottom;
                }
            }
            Debug.Fail("Unable to find the side for an element, possible layout problem!");
            return AnchorSide.Right;
        }
    }
}