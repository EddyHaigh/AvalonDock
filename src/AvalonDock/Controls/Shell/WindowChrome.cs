﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Windows.Shell
{
#pragma warning disable IDE0065 // Misplaced using directive, some MS stuff so dont want to mess with it rn
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Data;

    using AvalonDock.Diagnostics;

    using Standard;
#pragma warning restore IDE0065 // Misplaced using directive

    public class WindowChrome : Freezable
    {
        public WindowChrome()
        {
            // Effective default values for some of these properties are set to be bindings
            // that set them to system defaults.
            // A more correct way to do this would be to Coerce the value iff the source of the DP was the default value.
            // Unfortunately with the current property system we can't detect whether the value being applied at the time
            // of the coersion is the default.
            foreach (var bp in BoundProperties)
            {
                // This list must be declared after the DP's are assigned.
                Assert.IsNotNull(bp.DependencyProperty);
                BindingOperations.SetBinding(this, bp.DependencyProperty,
                    new Binding
                    {
                        Source = SystemParameters2.Current,
                        Path = new PropertyPath(path: bp.SystemParameterPropertyName),
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    });
            }
        }

        internal event EventHandler PropertyChangedThatRequiresRepaint;

        private struct SystemParameterBoundProperty
        {
            public string SystemParameterPropertyName { get; set; }
            public DependencyProperty DependencyProperty { get; set; }
        }

        // Named property available for fully extending the glass frame.
        public static Thickness GlassFrameCompleteThickness => new(-1);

        public static readonly DependencyProperty WindowChromeProperty = DependencyProperty.RegisterAttached("WindowChrome", typeof(WindowChrome), typeof(WindowChrome),
            new PropertyMetadata(null, OnChromeChanged));

        private static void OnChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The different design tools handle drawing outside their custom window objects differently.
            // Rather than try to support this concept in the design surface let the designer draw its own
            // chrome anyways.
            // There's certainly room for improvement here.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(d))
            {
                return;
            }

            var window = (Window)d;
            var newChrome = (WindowChrome)e.NewValue;
            Assert.IsNotNull(window);

            // Update the ChromeWorker with this new object.

            // If there isn't currently a worker associated with the Window then assign a new one.
            // There can be a many:1 relationship of to Window to WindowChrome objects, but a 1:1 for a Window and a WindowChromeWorker.
            var chromeWorker = WindowChromeWorker.GetWindowChromeWorker(window);
            if (chromeWorker == null)
            {
                chromeWorker = new WindowChromeWorker();
                WindowChromeWorker.SetWindowChromeWorker(window, chromeWorker);
            }
            chromeWorker.SetWindowChrome(newChrome);
        }

        public static WindowChrome GetWindowChrome(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            return (WindowChrome)window.GetValue(WindowChromeProperty);
        }

        public static void SetWindowChrome(Window window, WindowChrome chrome)
        {
            Verify.IsNotNull(window, nameof(window));
            window.SetValue(WindowChromeProperty, chrome);
        }

        public static readonly DependencyProperty IsHitTestVisibleInChromeProperty = DependencyProperty.RegisterAttached(
            "IsHitTestVisibleInChrome", typeof(bool), typeof(WindowChrome),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsHitTestVisibleInChrome(IInputElement inputElement)
        {
            Verify.IsNotNull(inputElement, nameof(inputElement));
            if (inputElement is not DependencyObject dobj)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }

            return (bool)dobj.GetValue(IsHitTestVisibleInChromeProperty);
        }

        public static void SetIsHitTestVisibleInChrome(IInputElement inputElement, bool hitTestVisible)
        {
            Verify.IsNotNull(inputElement, nameof(inputElement));
            if (inputElement is not DependencyObject dobj)
            {
                throw new ArgumentException("The element must be a DependencyObject", nameof(inputElement));
            }

            dobj.SetValue(IsHitTestVisibleInChromeProperty, hitTestVisible);
        }

        public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.Register(nameof(CaptionHeight), typeof(double), typeof(WindowChrome),
            new PropertyMetadata(0d, (d, e) => ((WindowChrome)d).OnPropertyChangedThatRequiresRepaint()), value => (double)value >= 0d);

        /// <summary>The extent of the top of the window to treat as the caption.</summary>
        public double CaptionHeight
        {
            get => (double)GetValue(CaptionHeightProperty);
            set => SetValue(CaptionHeightProperty, value);
        }

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChrome),
            new PropertyMetadata(default(Thickness)), (value) => Utility.IsThicknessNonNegative((Thickness)value));

        public Thickness ResizeBorderThickness
        {
            get => (Thickness)GetValue(ResizeBorderThicknessProperty);
            set => SetValue(ResizeBorderThicknessProperty, value);
        }

        public static readonly DependencyProperty GlassFrameThicknessProperty = DependencyProperty.Register(nameof(GlassFrameThickness), typeof(Thickness), typeof(WindowChrome),
            new PropertyMetadata(default(Thickness), (d, e) => ((WindowChrome)d).OnPropertyChangedThatRequiresRepaint(), (d, o) => CoerceGlassFrameThickness((Thickness)o)));

        private static object CoerceGlassFrameThickness(Thickness thickness)
        {
            // If it's explicitly set, but set to a thickness with at least one negative side then
            // coerce the value to the stock GlassFrameCompleteThickness.
            return !Utility.IsThicknessNonNegative(thickness) ? GlassFrameCompleteThickness : thickness;
        }

        public Thickness GlassFrameThickness
        {
            get => (Thickness)GetValue(GlassFrameThicknessProperty);
            set => SetValue(GlassFrameThicknessProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(WindowChrome),
            new PropertyMetadata(default(CornerRadius), (d, e) => ((WindowChrome)d).OnPropertyChangedThatRequiresRepaint()), (value) => Utility.IsCornerRadiusValid((CornerRadius)value));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>Gets or sets the ShowSystemMenu property.  This dependency property indicates if the system menu should be shown at right click on the caption. </summary>
        public bool ShowSystemMenu { get; set; }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore()
        {
            return new WindowChrome();
        }

        private static readonly List<SystemParameterBoundProperty> BoundProperties =
        [
            new() { DependencyProperty = CornerRadiusProperty, SystemParameterPropertyName = "WindowCornerRadius" },
            new() { DependencyProperty = CaptionHeightProperty, SystemParameterPropertyName = "WindowCaptionHeight" },
            new() { DependencyProperty = ResizeBorderThicknessProperty, SystemParameterPropertyName = "WindowResizeBorderThickness" },
            new() { DependencyProperty = GlassFrameThicknessProperty, SystemParameterPropertyName = "WindowNonClientFrameThickness" },
        ];

        private void OnPropertyChangedThatRequiresRepaint() => PropertyChangedThatRequiresRepaint?.Invoke(this, EventArgs.Empty);
    }
}