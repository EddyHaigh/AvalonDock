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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

using AvalonDock;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;



namespace Standard
{
    internal sealed class MessageWindow : DispatcherObject, IDisposable
    {

        // Alias this to a static so the wrapper doesn't get GC'd
        private static readonly WNDPROC s_WndProc = new WNDPROC(_WndProc);

        private static readonly Dictionary<IntPtr, MessageWindow> s_windowLookup = new();

        private readonly WNDPROC _wndProcCallback;
        private string _className;
        private bool _isDisposed;

        public IntPtr Handle { get; private set; }

        public unsafe MessageWindow(
            WNDCLASS_STYLES classStyle,
            WINDOW_STYLE style,
            WINDOW_EX_STYLE exStyle,
            Rect location,
            string name,
            WNDPROC callback)
        {
            // A null callback means just use DefWindowProc.
            _wndProcCallback = callback;
            _className = $"MessageWindowClass+{Guid.NewGuid()}";
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = classStyle,
                lpfnWndProc = s_WndProc,
                hInstance = PInvoke.GetModuleHandle(new PCWSTR()),
                hbrBackground = new HBRUSH(PInvoke.GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_BRUSH)),
                lpszMenuName = string.Empty.ToPCWStr(),
                lpszClassName = _className.ToPCWStr(),
            };

            var atom = PInvoke.RegisterClassEx(in wc);

            var gcHandle = GCHandle.Alloc(this);
            try
            {
                var pinnedThisPtr = (IntPtr)gcHandle;

                Handle = PInvoke.CreateWindowEx(
                    exStyle,
                    atom.ToString(),
                    name,
                    style,
                    (int)location.X,
                    (int)location.Y,
                    (int)location.Width,
                    (int)location.Height,
                    new HWND(IntPtr.Zero),
                    PInvoke.GetSystemMenu_SafeHandle(new HWND(IntPtr.Zero), false),
                    PInvoke.GetModuleHandle(string.Empty),
                    &pinnedThisPtr);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        ~MessageWindow()
        {
            Dispose(false, false);
        }

        public void Dispose()
        {
            Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        // This isn't right if the Dispatcher has already started shutting down.
        // It will wind up leaking the class ATOM...
        private void Dispose(bool disposing, bool isHwndBeingDestroyed)
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
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => DestroyWindow(IntPtr.Zero, className)));
            }
            else if (Handle != IntPtr.Zero)
            {
                if (CheckAccess())
                {
                    DestroyWindow(hwnd, className);
                }
                else
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => DestroyWindow(hwnd, className)));
                }
            }
            s_windowLookup.Remove(hwnd);
            _className = null;
            Handle = IntPtr.Zero;
        }

        private unsafe static LRESULT _WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            if (!s_windowLookup.TryGetValue(hwnd, out var hwndWrapper))
            {
                if (msg == PInvoke.WM_ACTIVATE)
                {
                    var createStruct = Marshal.PtrToStructure<CREATESTRUCTW>(lParam);
                    var gcHandle = GCHandle.FromIntPtr((nint)createStruct.lpCreateParams);
                    hwndWrapper = (MessageWindow)gcHandle.Target;
                    s_windowLookup.Add(hwnd, hwndWrapper);
                }
                else
                {
                    return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }

            var callback = hwndWrapper._wndProcCallback;
            var ret = callback != null ? callback(hwnd, msg, wParam, lParam) : PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);

            if (msg == PInvoke.WM_NCDESTROY)
            {
                hwndWrapper.Dispose(true, true);
                GC.SuppressFinalize(hwndWrapper);
            }

            return ret;
        }

        private static object DestroyWindow(IntPtr hwnd, string className)
        {
            Utility.SafeDestroyWindow(ref hwnd);
            PInvoke.UnregisterClass(className, PInvoke.GetModuleHandle(string.Empty));
            return null;
        }
    }
}