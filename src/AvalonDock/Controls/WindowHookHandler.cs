﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AvalonDock.Controls
{
    internal class FocusChangeEventArgs : EventArgs
    {
        public FocusChangeEventArgs(IntPtr gotFocusWinHandle, IntPtr lostFocusWinHandle)
        {
            GotFocusWinHandle = gotFocusWinHandle;
            LostFocusWinHandle = lostFocusWinHandle;
        }

        public IntPtr GotFocusWinHandle { get; private set; }

        public IntPtr LostFocusWinHandle { get; private set; }
    }

    internal class WindowHookHandler
    {
        private Win32Helper.HookProc _hookProc;
        private ReentrantFlag _insideActivateEvent = new ReentrantFlag();
        private IntPtr _windowHook;

        public WindowHookHandler()
        {
        }

        public event EventHandler<FocusChangeEventArgs> FocusChanged;

        //public event EventHandler<WindowActivateEventArgs> Activate;

        public void Attach()
        {
            _hookProc = new Win32Helper.HookProc(this.HookProc);
            _windowHook = Win32Helper.SetWindowsHookEx(
                Win32Helper.HookType.WH_CBT,
                _hookProc,
                IntPtr.Zero,
                (int)PInvoke.GetCurrentThreadId());
        }

        public void Detach()
        {
            Win32Helper.UnhookWindowsHookEx(_windowHook);
        }

        public int HookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == Win32Helper.HCBT_SETFOCUS)
            {
                if (FocusChanged != null)
                {
                    FocusChanged(this, new FocusChangeEventArgs(wParam, lParam));
                }
            }
            else if (code == Win32Helper.HCBT_ACTIVATE)
            {
                if (_insideActivateEvent.CanEnter)
                {
                    using (_insideActivateEvent.Enter())
                    {
                        //if (Activate != null)
                        //    Activate(this, new WindowActivateEventArgs(wParam));
                    }
                }
            }

            return Win32Helper.CallNextHookEx(_windowHook, code, wParam, lParam);
        }
    }
}