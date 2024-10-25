/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/
/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

using AvalonDock.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Standard
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Retrieves the current color used for DWM colorization and whether the color is opaque.
        /// </summary>
        /// <param name="pcrColorization">The current color used for DWM colorization.</param>
        /// <param name="pfOpaqueBlend">Indicates whether the color is opaque.</param>
        /// <returns>True if the colorization color was successfully retrieved; otherwise, false.</returns>
        public static bool DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend)
        {
            // Make this call safe to make on downlevel OSes...
            if (Utility.IsOSVistaOrNewer && PInvoke.IsThemeActive())
            {
                var hr = PInvoke.DwmGetColorizationColor(out pcrColorization, out BOOL pfOpaqueBlendNative);
                pfOpaqueBlend = pfOpaqueBlendNative;
                if (hr.Succeeded)
                {
                    return true;
                }
            }

            // Default values.  If for some reason the native DWM API fails it's never enough of a reason
            // to bring down the app.  Empirically it still sometimes returns errors even when the theme service is on.
            // We'll still use the boolean return value to allow the caller to respond if they care.
            pcrColorization = 0xFF000000;
            pfOpaqueBlend = true;

            return false;
        }

        /// <summary>
        /// Retrieves the composition timing information for the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window.</param>
        /// <returns>The composition timing information, or null if the system is not ready to respond.</returns>
        public static DWM_TIMING_INFO? DwmGetCompositionTimingInfo(IntPtr hwnd)
        {
            if (!Utility.IsOSVistaOrNewer)
            {
                return null; // API was new to Vista.
            }

            var hr = PInvoke.DwmGetCompositionTimingInfo(
                new HWND(Utility.IsOSWindows8OrNewer ? IntPtr.Zero : hwnd),
                out DWM_TIMING_INFO pTimingInfo);

            if (hr.Value == HResultCodes.E_PENDING)
            {
                return null; // The system isn't yet ready to respond.  Return null rather than throw.
            }

            hr.ThrowOnFailure();
            return pTimingInfo;
        }

        /// <summary>
        /// Determines whether DWM composition is enabled.
        /// </summary>
        /// <returns>True if DWM composition is enabled; otherwise, false.</returns>
        public static bool DwmIsCompositionEnabled()
        {
            // Make this call safe to make on downlevel OSes...
            return Utility.IsOSVistaOrNewer && PInvoke.DwmIsCompositionEnabled(out BOOL pfEnabled).Succeeded && pfEnabled;
        }

        /// <summary>
        /// Enables, disables, or grays out the specified menu item.
        /// </summary>
        /// <param name="hMenu">A handle to the menu.</param>
        /// <param name="uIDEnableItem">The menu item to be enabled, disabled, or grayed.</param>
        /// <param name="uEnable">Flags specifying the action to be taken.</param>
        /// <returns>The previous state of the menu item, or -1 if the menu item does not exist.</returns>
        public static MENU_ITEM_FLAGS EnableMenuItem(SafeHandle hMenu, uint uIDEnableItem, MENU_ITEM_FLAGS uEnable)
        {
            var result = (MENU_ITEM_FLAGS)(int)global::Windows.Win32.PInvoke.EnableMenuItem(hMenu, uIDEnableItem, uEnable);
            // Returns the previous state of the menu item, or -1 if the menu item does not exist.
            return result;
        }

        /// <summary>
        /// Retrieves the current theme name, color, and size.
        /// </summary>
        /// <param name="themeFileName">The current theme file name.</param>
        /// <param name="color">The current color scheme name.</param>
        /// <param name="size">The current size name.</param>
        public unsafe static void GetCurrentThemeName(out string themeFileName, out string color, out string size)
        {
            // The windows max path.
            const int maxPath = 260;
            const char whitespace = ' ';

            // Not expecting strings longer than max path. We will return the error
            var fileNameBuilder = new string(whitespace, maxPath);
            var colorBuilder = new string(whitespace, maxPath);
            var sizeBuilder = new string(whitespace, maxPath);

            fixed (char* fileNameReference = fileNameBuilder)
            fixed (char* colorNameReference = colorBuilder)
            fixed (char* sizeReference = sizeBuilder)
            {
                var fileNamePwString = new PWSTR(fileNameReference);
                var colorPwString = new PWSTR(colorNameReference);
                var sizePwString = new PWSTR(sizeReference);

                PInvoke.GetCurrentThemeName(
                    fileNamePwString,
                    fileNamePwString.Length,
                    colorPwString,
                    colorPwString.Length,
                    sizePwString,
                    sizePwString.Length)
                    .ThrowOnFailure();

                themeFileName = fileNamePwString.ToString();
                color = colorPwString.ToString();
                size = sizePwString.ToString();
            }
        }

        /// <summary>
        /// Retrieves the owner window of the specified window.
        /// </summary>
        /// <param name="childHandle">A handle to the window.</param>
        /// <returns>A handle to the owner window.</returns>
        public static IntPtr GetOwner(IntPtr childHandle)
        {
            return GetWindowLongPtr(childHandle, WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT);
        }

        /// <summary>
        /// Retrieves information about the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved.</param>
        /// <returns>The requested value.</returns>
        /// <exception cref="Win32Exception">Thrown when the function fails.</exception>
        public static IntPtr GetWindowLongPtr(IntPtr hwnd, WINDOW_LONG_PTR_INDEX nIndex)
        {
            var ret = IntPtr.Zero;
            ret = IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, nIndex) : new IntPtr(GetWindowLongPtr32(hwnd, nIndex));
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        /// <summary>
        /// Retrieves the Z order of the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window.</param>
        /// <param name="zOrder">The Z order of the window.</param>
        /// <returns>True if the Z order was successfully retrieved; otherwise, false.</returns>
        public static bool GetWindowZOrder(IntPtr hwnd, out int zOrder)
        {
            var lowestHwnd = PInvoke.GetWindow(new HWND(hwnd), GET_WINDOW_CMD.GW_HWNDLAST);

            var z = 0;
            var hwndTmp = lowestHwnd;
            while (hwndTmp != IntPtr.Zero)
            {
                if (hwnd == hwndTmp)
                {
                    zOrder = z;
                    return true;
                }

                hwndTmp = PInvoke.GetWindow(hwndTmp, GET_WINDOW_CMD.GW_HWNDPREV);
                z++;
            }

            zOrder = int.MinValue;
            return false;
        }

        /// <summary>
        /// Offsets the specified rectangle by the specified amounts.
        /// </summary>
        /// <param name="rect">The rectangle to be offset.</param>
        /// <param name="dx">The amount to offset the rectangle horizontally.</param>
        /// <param name="dy">The amount to offset the rectangle vertically.</param>
        /// <returns>The offset rectangle.</returns>
        public static RECT Offset(this RECT rect, int dx, int dy)
        {
            rect.left += dx;
            rect.top += dy;
            rect.right += dx;
            rect.bottom += dy;

            return rect;
        }

        /// <summary>
        /// Sets the owner window of the specified window.
        /// </summary>
        /// <param name="childHandle">A handle to the window.</param>
        /// <param name="ownerHandle">A handle to the owner window.</param>
        public static void SetOwner(IntPtr childHandle, IntPtr ownerHandle)
        {
            SetWindowLongPtr(
                childHandle,
                WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT,
                ownerHandle);
        }

        /// <summary>
        /// Sets information about the specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to be set.</param>
        /// <param name="dwNewLong">The new value.</param>
        /// <returns>The previous value.</returns>
        public static IntPtr SetWindowLongPtr(IntPtr hwnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, nIndex, dwNewLong) : new IntPtr(SetWindowLongPtr32(hwnd, nIndex, dwNewLong.ToInt32()));

        /// <summary>
        /// Retrieves the high contrast settings for the system.
        /// </summary>
        /// <returns>The high contrast settings.</returns>
        /// <exception cref="HRESULT">Thrown when the function fails.</exception>
        public unsafe static HIGHCONTRASTA SystemParameterInfoGetHighContrast()
        {
            var hc = new HIGHCONTRASTA { cbSize = (uint)Marshal.SizeOf<HIGHCONTRASTA>() };
            if (!PInvoke.SystemParametersInfo(
                SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST,
                hc.cbSize,
                &hc,
                0))
            {
                new HRESULT(Marshal.GetLastWin32Error()).ThrowOnFailure();
            }

            return hc;
        }

        /// <summary>
        /// Returns a rectangle that represents the union of two rectangles.
        /// </summary>
        /// <param name="rect1">The first rectangle.</param>
        /// <param name="rect2">The second rectangle.</param>
        /// <returns>A rectangle that represents the union of the two rectangles.</returns>
        public static RECT Union(this RECT rect1, RECT rect2)
        {
            return new RECT
            {
                left = Math.Min(rect1.left, rect2.left),
                top = Math.Min(rect1.top, rect2.top),
                right = Math.Max(rect1.right, rect2.right),
                bottom = Math.Max(rect1.bottom, rect2.bottom),
            };
        }

        /// <summary>
        /// Retrieves the current mouse cursor position.
        /// </summary>
        /// <returns>The current mouse cursor position.</returns>
        internal static Point GetMousePosition()
        {
            PInvoke.GetCursorPos(out System.Drawing.Point point);
            return new Point(point.X, point.Y);
        }

        /// <summary>
        /// Converts a string to a PCWSTR.
        /// </summary>
        /// <param name="text">The string to be converted.</param>
        /// <returns>The PCWSTR representation of the string.</returns>
        internal unsafe static PCWSTR ToPCWStr(this string text)
        {
            fixed (char* chars = text)
            {
                return new PCWSTR(chars);
            }
        }

        // CS Win32 cannot source gen the 64 and 32 bit versions of the GetWindowLongPtr and SetWindowLongPtr functions without additional work without x86 and x64 versions.

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLongPtr32(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLongPtr32(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr dwNewLong);
    }
}