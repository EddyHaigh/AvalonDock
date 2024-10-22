/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AvalonDock.Controls
{
    /// <summary>
    /// The event arguments for the FocusChanged event.
    /// </summary>
    internal class FocusChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FocusChangeEventArgs"/> class.
        /// </summary>
        /// <param name="gotFocusWinHandle">The handle for the window that is gaining focus.</param>
        /// <param name="lostFocusWinHandle">The handle for the window that is losing focus.</param>
        public FocusChangeEventArgs(IntPtr gotFocusWinHandle, IntPtr lostFocusWinHandle)
        {
            GotFocusWinHandle = gotFocusWinHandle;
            LostFocusWinHandle = lostFocusWinHandle;
        }

        /// <summary>
        /// Gets the handle for the window that is gaining focus.
        /// </summary>
        public IntPtr GotFocusWinHandle { get; private set; }

        /// <summary>
        /// Gets the handle for the window that is losing focus.
        /// </summary>
        public IntPtr LostFocusWinHandle { get; private set; }
    }

    /// <summary>
    /// The window hook handler.
    /// </summary>
    internal class WindowHookHandler : IDisposable
    {
        private HOOKPROC _hookProc;
        private ReentrantFlag _insideActivateEvent = new ReentrantFlag();
        private HHOOK _windowHook;
        private bool _disposed;

        public WindowHookHandler()
        {
        }

        public event EventHandler<FocusChangeEventArgs> FocusChanged;

        public void Attach()
        {
            _hookProc = new HOOKPROC(this.HookProc);
            _windowHook = PInvoke.SetWindowsHookEx(
                WINDOWS_HOOK_ID.WH_CBT,
                _hookProc,
                new HINSTANCE(IntPtr.Zero),
                PInvoke.GetCurrentThreadId());
        }

        public void Detach()
        {
            PInvoke.UnhookWindowsHookEx(_windowHook);
        }

        public LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam)
        {
            if (code == Win32Helper.HCBT_SETFOCUS)
            {
                FocusChanged?.Invoke(this, new FocusChangeEventArgs(new IntPtr((int)wParam.Value), lParam.Value));
            }
            else if (code == Win32Helper.HCBT_ACTIVATE
                && _insideActivateEvent.CanEnter)
            {
                using (_insideActivateEvent.Enter())
                {
                    //if (Activate != null)
                    //    Activate(this, new WindowActivateEventArgs(wParam));
                }
            }

            return PInvoke.CallNextHookEx(_windowHook, code, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Detach();
                    _hookProc = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}