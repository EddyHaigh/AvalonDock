﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Standard
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.Dwm;
    using Windows.Win32.Graphics.Gdi;
    using Windows.Win32.UI.Accessibility;
    using Windows.Win32.UI.WindowsAndMessaging;
    using Windows.Win32.Graphics.GdiPlus;

    using static AvalonDock.Controls.Shell.Standard.NativeStructs;
    using static AvalonDock.Win32Helper;
    // Some COM interfaces and Win32 structures are already declared in the framework.
    // Interesting ones to remember in System.Runtime.InteropServices.ComTypes are:
    using IStream = System.Runtime.InteropServices.ComTypes.IStream;

    // Some native methods are shimmed through public versions that handle converting failures into thrown exceptions.
    internal static class NativeMethods
    {
        public static IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect)
        {
            var ret = _CreateRectRgn(nLeftRect, nTopRect, nRightRect, nBottomRect);
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        public static IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse)
        {
            var ret = _CreateRoundRectRgn(nLeftRect, nTopRect, nRightRect, nBottomRect, nWidthEllipse, nHeightEllipse);
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

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

        public static DWM_TIMING_INFO? DwmGetCompositionTimingInfo(IntPtr hwnd)
        {
            if (!Utility.IsOSVistaOrNewer)
            {
                return null; // API was new to Vista.
            }

            var hr = PInvoke.DwmGetCompositionTimingInfo(
                new HWND(Utility.IsOSWindows8OrNewer ? IntPtr.Zero : hwnd),
                out DWM_TIMING_INFO pTimingInfo);

            if (hr.Value == HRESULT.E_PENDING.Code)
            {
                return null; // The system isn't yet ready to respond.  Return null rather than throw.
            }

            hr.ThrowOnFailure();
            return pTimingInfo;
        }

        public static bool DwmIsCompositionEnabled()
        {
            // Make this call safe to make on downlevel OSes...
            if (!Utility.IsOSVistaOrNewer)
            {
                return false;
            }

            return PInvoke.DwmIsCompositionEnabled(out BOOL pfEnabled).Succeeded && pfEnabled;
        }

        public static MENU_ITEM_FLAGS EnableMenuItem(SafeHandle hMenu, SC uIDEnableItem, MENU_ITEM_FLAGS uEnable)
        {
            var result = (MENU_ITEM_FLAGS)(int)global::Windows.Win32.PInvoke.EnableMenuItem(hMenu, (uint)uIDEnableItem, uEnable);
            // Returns the previous state of the menu item, or -1 if the menu item does not exist.
            return result;
        }

        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateBitmapFromStream(IStream stream, out IntPtr bitmap);

        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr hbmReturn);

        [DllImport("gdiplus.dll")]
        public static extern Status GdipDisposeImage(IntPtr image);

        public static void GetCurrentThemeName(out string themeFileName, out string color, out string size)
        {
            // Not expecting strings longer than MAX_PATH.  We will return the error
            var fileNameBuilder = new StringBuilder((int)Win32Value.MAX_PATH);
            var colorBuilder = new StringBuilder((int)Win32Value.MAX_PATH);
            var sizeBuilder = new StringBuilder((int)Win32Value.MAX_PATH);

            // This will throw if the theme service is not active (e.g. not UxTheme!IsThemeActive).
            _GetCurrentThemeName(fileNameBuilder, fileNameBuilder.Capacity,
                                 colorBuilder, colorBuilder.Capacity,
                                 sizeBuilder, sizeBuilder.Capacity)
                .ThrowIfFailed();

            themeFileName = fileNameBuilder.ToString();
            color = colorBuilder.ToString();
            size = sizeBuilder.ToString();
        }

        public static FreeLibrarySafeHandle GetModuleHandle(string lpModuleName)
        {
            var retPtr = PInvoke.GetModuleHandle(lpModuleName);
            if (retPtr.IsInvalid is true)
            {
                HRESULT.ThrowLastError();
            }

            return retPtr;
        }

        public static DeleteObjectSafeHandle GetStockObject(GET_STOCK_OBJECT_FLAGS fnObject)
        {
            var retPtr = PInvoke.GetStockObject_SafeHandle(fnObject);
            if (retPtr.IsInvalid is true)
            {
                HRESULT.ThrowLastError();
            }

            return retPtr;
        }

        // This is aliased as a macro in 32bit Windows.
        public static IntPtr GetWindowLongPtr(IntPtr hwnd, GWL nIndex)
        {
            var ret = IntPtr.Zero;
            ret = IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, nIndex) : new IntPtr(GetWindowLongPtr32(hwnd, nIndex));
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        // Note that this will throw HRESULT_FROM_WIN32(ERROR_CLASS_ALREADY_EXISTS) on duplicate registration.
        // If needed, consider adding a Try* version of this function that returns the error code since that
        // may be ignorable.
        public static short RegisterClassEx(ref WNDCLASSEXW lpwcx)
        {
            var ret = PInvoke.RegisterClassEx(in lpwcx);
            if (ret == 0)
            {
                HRESULT.ThrowLastError();
            }

            return (short)ret;
        }

        // This is aliased as a macro in 32bit Windows.
        public static IntPtr SetWindowLongPtr(IntPtr hwnd, GWL nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, nIndex, dwNewLong) : new IntPtr(SetWindowLongPtr32(hwnd, nIndex, dwNewLong.ToInt32()));

        public static void SetWindowRgn(HWND hWnd, HRGN hRgn, bool bRedraw)
        {
            if (PInvoke.SetWindowRgn(hWnd, hRgn, bRedraw) == 0)
            {
                throw new Win32Exception();
            }
        }

        public unsafe static HIGHCONTRASTA SystemParameterInfoGetHighContrast()
        {
            var hc = new HIGHCONTRASTA { cbSize = (uint)Marshal.SizeOf<HIGHCONTRASTA>() };
            if (!PInvoke.SystemParametersInfo(
                SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETHIGHCONTRAST,
                hc.cbSize,
                &hc,
                0))
            {
                HRESULT.ThrowLastError();
            }

            return hc;
        }

        [DllImport("gdi32.dll", EntryPoint = "CreateRectRgn", SetLastError = true)]
        private static extern IntPtr _CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn", SetLastError = true)]
        private static extern IntPtr _CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("uxtheme.dll", EntryPoint = "GetCurrentThemeName", CharSet = CharSet.Unicode)]
        private static extern HRESULT _GetCurrentThemeName(
            StringBuilder pszThemeFileName,
            int dwMaxNameChars,
            StringBuilder pszColorBuff,
            int cchMaxColorChars,
            StringBuilder pszSizeBuff,
            int cchMaxSizeChars);

        [DllImport("user32.dll", EntryPoint = "SetWindowRgn", SetLastError = true)]
        private static extern int _SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLongPtr32(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLongPtr32(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, GWL nIndex, IntPtr dwNewLong);
    }

    internal static class Win32Value
    {
        public const uint FALSE = 0;
        public const uint INFOTIPSIZE = 1024;
        public const uint MAX_PATH = 260;
        public const uint sizeof_BOOL = 4;
        public const uint sizeof_CHAR = 1;
        public const uint sizeof_WCHAR = 2;
        public const uint TRUE = 1;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal class PROPVARIANT : IDisposable
    {
        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            internal static extern HRESULT PropVariantClear(PROPVARIANT pvar);
        }

        [FieldOffset(0)]
        private ushort vt;
        [FieldOffset(8)]
        private IntPtr pointerVal;
        [FieldOffset(8)]
        private byte byteVal;
        [FieldOffset(8)]
        private long longVal;
        [FieldOffset(8)]
        private short boolVal;

        public VarEnum VarType => (VarEnum)vt;

        // Right now only using this for strings.
        public string GetValue() => vt == (ushort)VarEnum.VT_LPWSTR ? Marshal.PtrToStringUni(pointerVal) : null;

        public void SetValue(bool f)
        {
            Clear();
            vt = (ushort)VarEnum.VT_BOOL;
            boolVal = (short)(f ? -1 : 0);
        }

        public void SetValue(string val)
        {
            Clear();
            vt = (ushort)VarEnum.VT_LPWSTR;
            pointerVal = Marshal.StringToCoTaskMemUni(val);
        }

        public void Clear()
        {
            var hr = NativeMethods.PropVariantClear(this);
            Assert.IsTrue(hr.Succeeded);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PROPVARIANT()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            Clear();
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal class RefRECT
    {
        private int _left;
        private int _top;
        private int _right;
        private int _bottom;

        public RefRECT(int left, int top, int right, int bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        public int Width => _right - _left;

        public int Height => _bottom - _top;

        public int Left { get => _left; set => _left = value; }

        public int Right { get => _right; set => _right = value; }

        public int Top { get => _top; set => _top = value; }

        public int Bottom { get => _bottom; set => _bottom = value; }

        public void Offset(int dx, int dy)
        {
            _left += dx;
            _top += dy;
            _right += dx;
            _bottom += dy;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [BestFitMapping(false)]
    internal class WIN32_FIND_DATAW
    {
        public FileAttributes dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}