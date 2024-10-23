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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Threading;

    using static AvalonDock.Controls.Shell.Standard.NativeStructs;

    internal sealed class MessageWindow : DispatcherObject, IDisposable
    {
        // Alias this to a static so the wrapper doesn't get GC'd
        private static readonly WndProc s_WndProc = new WndProc(_WndProc);

        private static readonly Dictionary<IntPtr, MessageWindow> s_windowLookup = new Dictionary<IntPtr, MessageWindow>();

        private WndProc _wndProcCallback;
        private string _className;
        private bool _isDisposed;

        public IntPtr Handle
        {
            get; private set;
        }

        public MessageWindow(
            Windows.Win32.UI.WindowsAndMessaging.WNDCLASS_STYLES classStyle,
            Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE style,
            Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE exStyle,
            Rect location,
            string name,
            WndProc callback)
        {
            // A null callback means just use DefWindowProc.
            _wndProcCallback = callback;
            _className = "MessageWindowClass+" + Guid.NewGuid().ToString();
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                style = classStyle,
                lpfnWndProc = s_WndProc,
                hInstance = NativeMethods.GetModuleHandle(null),
                hbrBackground = NativeMethods.GetStockObject(global::Windows.Win32.Graphics.Gdi.GET_STOCK_OBJECT_FLAGS.NULL_BRUSH),
                lpszMenuName = "",
                lpszClassName = _className,
            };

            NativeMethods.RegisterClassEx(ref wc);

            var gcHandle = default(GCHandle);
            try
            {
                gcHandle = GCHandle.Alloc(this);
                var pinnedThisPtr = (IntPtr)gcHandle;

                Handle = NativeMethods.CreateWindowEx(
                    exStyle,
                    _className,
                    name,
                    style,
                    (int)location.X,
                    (int)location.Y,
                    (int)location.Width,
                    (int)location.Height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pinnedThisPtr);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        ~MessageWindow()
        {
            _Dispose(false, false);
        }

        public void Dispose()
        {
            _Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        // This isn't right if the Dispatcher has already started shutting down.
        // It will wind up leaking the class ATOM...
        private void _Dispose(bool disposing, bool isHwndBeingDestroyed)
        {
            // Block against reentrancy.
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            var hwnd = Handle;
            var className = _className;

            if (isHwndBeingDestroyed)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => _DestroyWindow(IntPtr.Zero, className)));
            }
            else if (Handle != IntPtr.Zero)
            {
                if (CheckAccess())
                {
                    _DestroyWindow(hwnd, className);
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => _DestroyWindow(hwnd, className)));
                }
            }
            s_windowLookup.Remove(hwnd);
            _className = null;
            Handle = IntPtr.Zero;
        }

        private static IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            var ret = IntPtr.Zero;
            MessageWindow hwndWrapper = null;

            if (msg == WM.CREATE)
            {
                var createStruct = Marshal.PtrToStructure<CREATESTRUCT>(lParam);
                var gcHandle = GCHandle.FromIntPtr(createStruct.lpCreateParams);
                hwndWrapper = (MessageWindow)gcHandle.Target;
                s_windowLookup.Add(hwnd, hwndWrapper);
            }
            else
            {
                if (!s_windowLookup.TryGetValue(hwnd, out hwndWrapper))
                {
                    return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }
            Assert.IsNotNull(hwndWrapper);

            var callback = hwndWrapper._wndProcCallback;
            if (callback != null)
            {
                ret = callback(hwnd, msg, wParam, lParam);
            }
            else
            {
                ret = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            if (msg == WM.NCDESTROY)
            {
                hwndWrapper._Dispose(true, true);
                GC.SuppressFinalize(hwndWrapper);
            }

            return ret;
        }

        private static object _DestroyWindow(IntPtr hwnd, string className)
        {
            Utility.SafeDestroyWindow(ref hwnd);
            NativeMethods.UnregisterClass(className, NativeMethods.GetModuleHandle(null));
            return null;
        }
    }
}