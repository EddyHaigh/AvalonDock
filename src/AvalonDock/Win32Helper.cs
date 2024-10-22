/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Windows;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AvalonDock
{
    internal static class Win32Helper
    {
        internal const int WS_CHILD = 0x40000000;
        internal const int WS_VISIBLE = 0x10000000;
        internal const int WS_VSCROLL = 0x00200000;
        internal const int WS_BORDER = 0x00800000;
        internal const int WS_CLIPSIBLINGS = 0x04000000;
        internal const int WS_CLIPCHILDREN = 0x02000000;
        internal const int WS_TABSTOP = 0x00010000;
        internal const int WS_GROUP = 0x00020000;

        internal const int WM_WINDOWPOSCHANGED = 0x0047;
        internal const int WM_WINDOWPOSCHANGING = 0x0046;
        internal const int WM_NCMOUSEMOVE = 0xa0;
        internal const int WM_NCLBUTTONDOWN = 0xA1;
        internal const int WM_NCLBUTTONUP = 0xA2;
        internal const int WM_NCLBUTTONDBLCLK = 0xA3;
        internal const int WM_NCRBUTTONDOWN = 0xA4;
        internal const int WM_NCRBUTTONUP = 0xA5;
        internal const int WM_CAPTURECHANGED = 0x0215;
        internal const int WM_EXITSIZEMOVE = 0x0232;
        internal const int WM_ENTERSIZEMOVE = 0x0231;
        internal const int WM_MOVE = 0x0003;
        internal const int WM_MOVING = 0x0216;
        internal const int WM_KILLFOCUS = 0x0008;
        internal const int WM_SETFOCUS = 0x0007;
        internal const int WM_ACTIVATE = 0x0006;
        internal const int WM_NCHITTEST = 0x0084;
        internal const int WM_INITMENUPOPUP = 0x0117;
        internal const int WM_KEYDOWN = 0x0100;
        internal const int WM_KEYUP = 0x0101;
        internal const int WM_CLOSE = 0x10;

        internal const int WA_INACTIVE = 0x0000;

        internal const int WM_SYSCOMMAND = 0x0112;

        // These are the wParam of WM_SYSCOMMAND
        internal const int SC_MAXIMIZE = 0xF030;

        internal const int SC_RESTORE = 0xF120;

        internal const int WM_CREATE = 0x0001;

        internal const int HT_CAPTION = 0x2;

        /// <summary>
        /// The system is about to activate a window <seealso href="https://learn.microsoft.com/en-us/windows/win32/winmsg/cbtproc"/>.
        /// </summary>
        public const int HCBT_SETFOCUS = 9;

        /// <summary>
        /// A window is about to receive the keyboard focus <seealso href="https://learn.microsoft.com/en-us/windows/win32/winmsg/cbtproc"/>.
        /// </summary>
        public const int HCBT_ACTIVATE = 5;

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

        internal const int WM_MOUSEMOVE = 0x200;
        internal const int WM_LBUTTONDOWN = 0x201;
        internal const int WM_LBUTTONUP = 0x202;
        internal const int WM_LBUTTONDBLCLK = 0x203;
        internal const int WM_RBUTTONDOWN = 0x204;
        internal const int WM_RBUTTONUP = 0x205;
        internal const int WM_RBUTTONDBLCLK = 0x206;
        internal const int WM_MBUTTONDOWN = 0x207;
        internal const int WM_MBUTTONUP = 0x208;
        internal const int WM_MBUTTONDBLCLK = 0x209;
        internal const int WM_MOUSEWHEEL = 0x20A;
        internal const int WM_MOUSEHWHEEL = 0x20E;

        internal static Point GetMousePosition()
        {
            PInvoke.GetCursorPos(out System.Drawing.Point point);
            return new Point(point.X, point.Y);
        }

        // CS Win32 cannot source gen the 64 and 32 bit versions of the GetWindowLongPtr and SetWindowLongPtr functions

        internal static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        internal static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static void SetOwner(IntPtr childHandle, IntPtr ownerHandle)
        {
            SetWindowLongPtr(
                childHandle,
                -8, // GWL_HWNDPARENT
                ownerHandle);
        }

        public static IntPtr GetOwner(IntPtr childHandle)
        {
            return GetWindowLongPtr(childHandle, -8);
        }
    }
}
