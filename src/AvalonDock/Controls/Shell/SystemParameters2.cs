﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

using AvalonDock.Diagnostics;

using Standard;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Windows.Shell
{
    public class SystemParameters2 : INotifyPropertyChanged
    {
        private delegate void SystemMetricUpdate(WPARAM wParam, LPARAM lParam);

        [ThreadStatic]
        private static readonly SystemParameters2 ThreadLocalSingleton;

        private MessageWindow _messageHwnd;

        private bool _isGlassEnabled;
        private Color _glassColor;
        private SolidColorBrush _glassColorBrush;
        private Thickness _windowResizeBorderThickness;
        private Thickness _windowNonClientFrameThickness;
        private double _captionHeight;
        private Size _smallIconSize;
        private string _uxThemeName;
        private string _uxThemeColor;
        private bool _isHighContrast;
        private CornerRadius _windowCornerRadius;
        private Rect _captionButtonLocation;

        private readonly Dictionary<uint, List<SystemMetricUpdate>> _updateTable;

        // Most properties exposed here have a way of being queried directly
        // and a way of being notified of updates via a window message.
        // This region is a grouping of both, for each of the exposed properties.

        private void InitializeIsGlassEnabled()
        {
            IsGlassEnabled = NativeMethods.DwmIsCompositionEnabled();
        }

        private void UpdateIsGlassEnabled(WPARAM wParam, LPARAM lParam)
        {
            // Neither the wParam or lParam are used in this case.
            InitializeIsGlassEnabled();
        }

        private void InitializeGlassColor()
        {
            NativeMethods.DwmGetColorizationColor(out var color, out var isOpaque);
            color |= isOpaque ? 0xFF000000 : 0;
            WindowGlassColor = Utility.ColorFromArgbDword(color);
            var glassBrush = new SolidColorBrush(WindowGlassColor);
            glassBrush.Freeze();
            WindowGlassBrush = glassBrush;
        }

        private void UpdateGlassColor(WPARAM wParam, LPARAM lParam)
        {
            var isOpaque = lParam != default;
            var color = unchecked((uint)(int)wParam.Value);
            color |= isOpaque ? 0xFF000000 : 0;
            WindowGlassColor = Utility.ColorFromArgbDword(color);
            var glassBrush = new SolidColorBrush(WindowGlassColor);
            glassBrush.Freeze();
            WindowGlassBrush = glassBrush;
        }

        private void InitializeCaptionHeight()
        {
            var ptCaption = new Point(
                0,
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMCAPTION));

            WindowCaptionHeight = DpiHelper.DevicePixelsToLogical(ptCaption).Y;
        }

        private void UpdateCaptionHeight(WPARAM wParam, LPARAM lParam)
        {
            InitializeCaptionHeight();
        }

        private void InitializeWindowResizeBorderThickness()
        {
            var frameSize = new Size(
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSIZEFRAME),
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME));
            var frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize);
            WindowResizeBorderThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height, frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void UpdateWindowResizeBorderThickness(WPARAM wParam, LPARAM lParam)
        {
            InitializeWindowResizeBorderThickness();
        }

        private void InitializeWindowNonClientFrameThickness()
        {
            var frameSize = new Size(
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSIZEFRAME),
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME));

            var frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize);
            var captionHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYCAPTION);
            var captionHeightInDips = DpiHelper.DevicePixelsToLogical(new Point(0, captionHeight)).Y;
            WindowNonClientFrameThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height + captionHeightInDips, frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void UpdateWindowNonClientFrameThickness(WPARAM wParam, LPARAM lParam)
        {
            InitializeWindowNonClientFrameThickness();
        }

        private void InitializeSmallIconSize()
        {
            SmallIconSize = new Size(
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON),
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON));
        }

        private void UpdateSmallIconSize(WPARAM wParam, LPARAM lParam)
        {
            InitializeSmallIconSize();
        }

        private void LegacyInitializeCaptionButtonLocation()
        {
            // This calculation isn't quite right, but it's pretty close.
            // I expect this is good enough for the scenarios where this is expected to be used.
            var captionX = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSIZE);
            var captionY = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZE);

            var frameX =
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSIZEFRAME)
                + PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXEDGE);

            var frameY =
                PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME)
                + PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYEDGE);

            var captionRect = new Rect(0, 0, captionX * 3, captionY);
            captionRect.Offset(-frameX - captionRect.Width, frameY);

            WindowCaptionButtonsLocation = captionRect;
        }

        private void InitializeCaptionButtonLocation()
        {
            // There is a completely different way to do this on XP.
            if (!Utility.IsOSVistaOrNewer || !PInvoke.IsThemeActive())
            {
                LegacyInitializeCaptionButtonLocation();
                return;
            }

            var tbix = new TITLEBARINFOEX { cbSize = (uint)Marshal.SizeOf<TITLEBARINFOEX>() };
            var lParam = Marshal.AllocHGlobal((int)tbix.cbSize);
            try
            {
                Marshal.StructureToPtr(tbix, lParam, false);
                // This might flash a window in the taskbar while being calculated.
                // WM_GETTITLEBARINFOEX doesn't work correctly unless the window is visible while processing.
                PInvoke.ShowWindow(new HWND(_messageHwnd.Handle), SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.SendMessage(
                    new HWND(_messageHwnd.Handle),
                    PInvoke.WM_GETTITLEBARINFOEX,
                    new WPARAM(0),
                    new LPARAM(lParam));
                tbix = Marshal.PtrToStructure<TITLEBARINFOEX>(lParam);
            }
            finally
            {
                PInvoke.ShowWindow(new HWND(_messageHwnd.Handle), SHOW_WINDOW_CMD.SW_HIDE);
                Utility.SafeFreeHGlobal(ref lParam);
            }

            // TITLEBARINFOEX has information relative to the screen.  We need to convert the containing rect
            // to instead be relative to the top-right corner of the window.
            var rcAllCaptionButtons = tbix.rgrect._5.Union(tbix.rgrect._2);
            // For all known themes, the RECT for the maximize box shouldn't add anything to the union of the minimize and close boxes.
            Assert.AreEqual(rcAllCaptionButtons, rcAllCaptionButtons.Union(tbix.rgrect._3));

            PInvoke.GetWindowRect(new HWND(_messageHwnd.Handle), out RECT rcWindow);

            // Reorient the Top/Right to be relative to the top right edge of the Window.
            var deviceCaptionLocation = new Rect(
                rcAllCaptionButtons.left - rcWindow.Width - rcWindow.left,
                rcAllCaptionButtons.top - rcWindow.top,
                rcAllCaptionButtons.Width,
                rcAllCaptionButtons.Height);
            var logicalCaptionLocation = DpiHelper.DeviceRectToLogical(deviceCaptionLocation);
            WindowCaptionButtonsLocation = logicalCaptionLocation;
        }

        private void UpdateCaptionButtonLocation(WPARAM wParam, LPARAM lParam)
        {
            InitializeCaptionButtonLocation();
        }

        private void InitializeHighContrast()
        {
            var hc = NativeMethods.SystemParameterInfoGetHighContrast();
            HighContrast = (hc.dwFlags & HIGHCONTRASTW_FLAGS.HCF_HIGHCONTRASTON) != 0;
        }

        private void UpdateHighContrast(WPARAM wParam, LPARAM lParam)
        {
            InitializeHighContrast();
        }

        private void InitializeThemeInfo()
        {
            if (!PInvoke.IsThemeActive())
            {
                UxThemeName = "Classic";
                UxThemeColor = "";
                return;
            }

            NativeMethods.GetCurrentThemeName(out var name, out var color, out _);

            // Consider whether this is the most useful way to expose this...
            UxThemeName = System.IO.Path.GetFileNameWithoutExtension(name);
            UxThemeColor = color;
        }

        private void UpdateThemeInfo(WPARAM wParam, LPARAM lParam)
        {
            InitializeThemeInfo();
        }

        private void InitializeWindowCornerRadius()
        {
            // The radius of window corners isn't exposed as a true system parameter.
            // It instead is a logical size that we're approximating based on the current theme.
            // There aren't any known variations based on theme color.
            Assert.IsNeitherNullNorEmpty(UxThemeName);

            // TODO: Probably need to make this available/adaptable by themes outside the main library
            // These radii are approximate.  The way WPF does rounding is different than how
            //     rounded-rectangle HRGNs are created, which is also different than the actual
            //     round corners on themed Windows.  For now we're not exposing anything to
            //     mitigate the differences.
            var cornerRadius = UxThemeName.ToUpperInvariant() switch
            {
                "LUNA" => new CornerRadius(6, 6, 0, 0),
                "AERO" => NativeMethods.DwmIsCompositionEnabled() ?
                                        new CornerRadius(8)
                                        : new CornerRadius(6, 6, 0, 0),// Aero has two cases.  One with glass and one without...
                _ => new CornerRadius(0),
            };
            WindowCornerRadius = cornerRadius;
        }

        private void UpdateWindowCornerRadius(WPARAM wParam, LPARAM lParam)
        {
            // Neither the wParam or lParam are used in this case.
            InitializeWindowCornerRadius();
        }

        /// <summary>
        /// Private constructor.  The public way to access this class is through the static Current property.
        /// </summary>
        private SystemParameters2()
        {
            // This window gets used for calculations about standard caption button locations
            // so it has WS_OVERLAPPEDWINDOW as a style to give it normal caption buttons.
            // This window may be shown during calculations of caption bar information, so create it at a location that's likely offscreen.
            _messageHwnd = new MessageWindow(
                0,
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_DISABLED,
                0,
                new Rect(-16000, -16000, 100, 100),
                "",
                WndProc);
            _messageHwnd.Dispatcher.ShutdownStarted += (sender, e) => Utility.SafeDispose(ref _messageHwnd);

            // Fixup the default values of the DPs.
            InitializeIsGlassEnabled();
            InitializeGlassColor();
            InitializeCaptionHeight();
            InitializeWindowNonClientFrameThickness();
            InitializeWindowResizeBorderThickness();
            InitializeCaptionButtonLocation();
            InitializeSmallIconSize();
            InitializeHighContrast();
            InitializeThemeInfo();
            // WindowCornerRadius isn't exposed by true system parameters, so it requires the theme to be initialized first.
            InitializeWindowCornerRadius();

            _updateTable = new Dictionary<uint, List<SystemMetricUpdate>>
            {
                { PInvoke.WM_THEMECHANGED,
                    new List<SystemMetricUpdate>
                    {
                        UpdateThemeInfo,
                        UpdateHighContrast,
                        UpdateWindowCornerRadius,
                        UpdateCaptionButtonLocation, } },
                { PInvoke.WM_SETTINGCHANGE,
                    new List<SystemMetricUpdate>
                    {
                        UpdateCaptionHeight,
                        UpdateWindowResizeBorderThickness,
                        UpdateSmallIconSize,
                        UpdateHighContrast,
                        UpdateWindowNonClientFrameThickness,
                        UpdateCaptionButtonLocation, } },
                { PInvoke.WM_DWMNCRENDERINGCHANGED, new List<SystemMetricUpdate> { UpdateIsGlassEnabled } },
                { PInvoke.WM_DWMCOMPOSITIONCHANGED, new List<SystemMetricUpdate> { UpdateIsGlassEnabled } },
                { PInvoke.WM_DWMCOLORIZATIONCOLORCHANGED, new List<SystemMetricUpdate> { UpdateGlassColor } },
            };
        }

        public static SystemParameters2 Current => ThreadLocalSingleton ?? new SystemParameters2();

        private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            // Don't do this if called within the SystemParameters2 constructor
            if (_updateTable == null)
            {
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            if (!_updateTable.TryGetValue(msg, out var handlers))
            {
                return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            Assert.IsNotNull(handlers);
            foreach (var handler in handlers)
            {
                handler(wParam, lParam);
            }

            return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public bool IsGlassEnabled
        {
            // return _isGlassEnabled;
            // It turns out there may be some lag between someone asking this
            // and the window getting updated.  It's not too expensive, just always do the check.
            get => NativeMethods.DwmIsCompositionEnabled();
            private set
            {
                if (value == _isGlassEnabled)
                {
                    return;
                }

                _isGlassEnabled = value;
                NotifyPropertyChanged(nameof(IsGlassEnabled));
            }
        }

        public Color WindowGlassColor
        {
            get => _glassColor;
            private set
            {
                if (value == _glassColor)
                {
                    return;
                }

                _glassColor = value;
                NotifyPropertyChanged(nameof(WindowGlassColor));
            }
        }

        public SolidColorBrush WindowGlassBrush
        {
            get => _glassColorBrush;
            private set
            {
                Assert.IsNotNull(value);
                Assert.IsTrue(value.IsFrozen);
                if (_glassColorBrush != null && value.Color == _glassColorBrush.Color)
                {
                    return;
                }

                _glassColorBrush = value;
                NotifyPropertyChanged(nameof(WindowGlassBrush));
            }
        }

        public Thickness WindowResizeBorderThickness
        {
            get => _windowResizeBorderThickness;
            private set
            {
                if (value == _windowResizeBorderThickness)
                {
                    return;
                }

                _windowResizeBorderThickness = value;
                NotifyPropertyChanged(nameof(WindowResizeBorderThickness));
            }
        }

        public Thickness WindowNonClientFrameThickness
        {
            get => _windowNonClientFrameThickness;
            private set
            {
                if (value == _windowNonClientFrameThickness)
                {
                    return;
                }

                _windowNonClientFrameThickness = value;
                NotifyPropertyChanged(nameof(WindowNonClientFrameThickness));
            }
        }

        public double WindowCaptionHeight
        {
            get => _captionHeight;
            private set
            {
                if (value == _captionHeight)
                {
                    return;
                }

                _captionHeight = value;
                NotifyPropertyChanged(nameof(WindowCaptionHeight));
            }
        }

        public Size SmallIconSize
        {
            get => new(_smallIconSize.Width, _smallIconSize.Height);
            private set
            {
                if (value == _smallIconSize)
                {
                    return;
                }

                _smallIconSize = value;
                NotifyPropertyChanged(nameof(SmallIconSize));
            }
        }

        public string UxThemeName
        {
            get => _uxThemeName;
            private set
            {
                if (value == _uxThemeName)
                {
                    return;
                }

                _uxThemeName = value;
                NotifyPropertyChanged(nameof(UxThemeName));
            }
        }

        public string UxThemeColor
        {
            get => _uxThemeColor;
            private set
            {
                if (value == _uxThemeColor)
                {
                    return;
                }

                _uxThemeColor = value;
                NotifyPropertyChanged(nameof(UxThemeColor));
            }
        }

        public bool HighContrast
        {
            get => _isHighContrast;
            private set
            {
                if (value == _isHighContrast)
                {
                    return;
                }

                _isHighContrast = value;
                NotifyPropertyChanged(nameof(HighContrast));
            }
        }

        public CornerRadius WindowCornerRadius
        {
            get => _windowCornerRadius;
            private set
            {
                if (value == _windowCornerRadius)
                {
                    return;
                }

                _windowCornerRadius = value;
                NotifyPropertyChanged(nameof(WindowCornerRadius));
            }
        }

        public Rect WindowCaptionButtonsLocation
        {
            get => _captionButtonLocation;
            private set
            {
                if (value == _captionButtonLocation)
                {
                    return;
                }

                _captionButtonLocation = value;
                NotifyPropertyChanged(nameof(WindowCaptionButtonsLocation));
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}