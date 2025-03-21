﻿/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using AvalonDock;


using Standard;

using global::Windows.Win32;
using global::Windows.Win32.Graphics.Gdi;
using global::Windows.Win32.Foundation;
using global::Windows.Win32.UI.WindowsAndMessaging;
using global::Windows.Win32.UI.Controls;

using AvalonDock.Diagnostics;
using static Microsoft.Windows.Shell.WindowChromeWorker;
using HANDLE_MESSAGE = System.Collections.Generic.KeyValuePair<uint, Microsoft.Windows.Shell.WindowChromeWorker.MessageHandler>;
using System.Diagnostics;

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace Microsoft.Windows.Shell
{
    internal class WindowChromeWorker : DependencyObject
    {
        internal delegate LRESULT MessageHandler(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled);

        // Delegate signature used for Dispatcher.BeginInvoke.
        private delegate void _Action();

        private const SET_WINDOW_POS_FLAGS _SwpFlags =
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED
            | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
            | SET_WINDOW_POS_FLAGS.SWP_NOMOVE
            | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
            | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER
            | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

        private readonly List<HANDLE_MESSAGE> _messageTable;

        /// <summary>The Window that's chrome is being modified.</summary>
        private Window _window;

        /// <summary>Underlying HWND for the _window.</summary>
        private IntPtr _hwnd;

        private HwndSource _hwndSource = null;
        private bool _isHooked = false;

        // These fields are for tracking workarounds for WPF 3.5SP1 behaviors.
        private bool _isFixedUp = false;

        private bool _isUserResizing = false;
        private bool _hasUserMovedWindow = false;
        private Point _windowPosAtStartOfUserMove = default;

        // Field to track attempts to force off Device Bitmaps on Win7.
        private int _blackGlassFixupAttemptCount;

        /// <summary>Object that describes the current modifications being made to the chrome.</summary>
        private WindowChrome _chromeInfo;

        // Keep track of this so we can detect when we need to apply changes.  Tracking these separately
        // as I've seen using just one cause things to get enough out of sync that occasionally the caption will redraw.
        private WindowState _lastRoundingState;

        private WindowState _lastMenuState;
        private bool _isGlassEnabled;

        public WindowChromeWorker()
        {
            _messageTable = new List<HANDLE_MESSAGE>
            {
                new HANDLE_MESSAGE(PInvoke.WM_SETTEXT,               HandleSetTextOrIcon),
                new HANDLE_MESSAGE(PInvoke.WM_SETICON,               HandleSetTextOrIcon),
                new HANDLE_MESSAGE(PInvoke.WM_NCACTIVATE,            HandleNCActivate),
                new HANDLE_MESSAGE(PInvoke.WM_NCCALCSIZE,            HandleNcCalcSize),
                new HANDLE_MESSAGE(PInvoke.WM_NCHITTEST,             HandleNCHitTest),
                new HANDLE_MESSAGE(PInvoke.WM_NCRBUTTONUP,           HandleNCRButtonUp),
                new HANDLE_MESSAGE(PInvoke.WM_SIZE,                  HandleSize),
                new HANDLE_MESSAGE(PInvoke.WM_WINDOWPOSCHANGED,      HandleWindowPosChanged),
                new HANDLE_MESSAGE(PInvoke.WM_DWMCOMPOSITIONCHANGED, HandleDwmCompositionChanged),
            };

            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                _messageTable.AddRange(new[]
                {
                   new HANDLE_MESSAGE(PInvoke.WM_SETTINGCHANGE,         HandleSettingChange),
                   new HANDLE_MESSAGE(PInvoke.WM_ENTERSIZEMOVE,         HandleEnterSizeMove),
                   new HANDLE_MESSAGE(PInvoke.WM_EXITSIZEMOVE,          HandleExitSizeMove),
                   new HANDLE_MESSAGE(PInvoke.WM_MOVE,                  HandleMove),
                });
            }
        }

        public void SetWindowChrome(WindowChrome newChrome)
        {
            VerifyAccess();
            Assert.IsNotNull(_window);

            // Nothing's changed.
            if (newChrome == _chromeInfo)
            {
                return;
            }

            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint -= OnChromePropertyChangedThatRequiresRepaint;
            }

            _chromeInfo = newChrome;
            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint += OnChromePropertyChangedThatRequiresRepaint;
            }

            ApplyNewCustomChrome();
        }

        private void OnChromePropertyChangedThatRequiresRepaint(object sender, EventArgs e) => UpdateFrameState(true);

        public static readonly DependencyProperty WindowChromeWorkerProperty = DependencyProperty.RegisterAttached(nameof(WindowChromeWorker), typeof(WindowChromeWorker), typeof(WindowChromeWorker),
            new PropertyMetadata(null, OnChromeWorkerChanged));

        private static void OnChromeWorkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var w = (Window)d;
            var cw = (WindowChromeWorker)e.NewValue;
            // The WindowChromeWorker object should only be set on the window once, and never to null.
            Assert.IsNotNull(w);
            Assert.IsNotNull(cw);
            Assert.IsNull(cw._window);
            cw.SetWindow(w);
        }

        private void SetWindow(Window window)
        {
            Assert.IsNull(_window);
            Assert.IsNotNull(window);
            _window = window;
            // There are potentially a couple funny states here.
            // The window may have been shown and closed, in which case it's no longer usable.
            // We shouldn't add any hooks in that case, just exit early.
            // If the window hasn't yet been shown, then we need to make sure to remove hooks after it's closed.
            _hwnd = new WindowInteropHelper(_window).Handle;
            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                // On older versions of the framework the client size of the window is incorrectly calculated.
                // We need to modify the template to fix this on behalf of the user.
                Utility.AddDependencyPropertyChangeListener(_window, System.Windows.Controls.Control.TemplateProperty, OnWindowPropertyChangedThatRequiresTemplateFixup);
                Utility.AddDependencyPropertyChangeListener(_window, FrameworkElement.FlowDirectionProperty, OnWindowPropertyChangedThatRequiresTemplateFixup);
            }
            _window.Closed += UnsetWindow;
            // Use whether we can get an HWND to determine if the Window has been loaded.
            if (_hwnd != IntPtr.Zero)
            {
                // We've seen that the HwndSource can't always be retrieved from the HWND, so cache it early.
                // Specifically it seems to sometimes disappear when the OS theme is changing.
                _hwndSource = HwndSource.FromHwnd(_hwnd);
                Assert.IsNotNull(_hwndSource);
                _window.ApplyTemplate();
                if (_chromeInfo != null)
                {
                    ApplyNewCustomChrome();
                }
            }
            else
            {
                _window.SourceInitialized += (sender, e) =>
                {
                    _hwnd = new WindowInteropHelper(_window).Handle;
                    Assert.IsNotDefault(_hwnd);
                    _hwndSource = HwndSource.FromHwnd(_hwnd);
                    Assert.IsNotNull(_hwndSource);
                    if (_chromeInfo != null)
                    {
                        ApplyNewCustomChrome();
                    }
                };
            }
        }

        private void UnsetWindow(object sender, EventArgs e)
        {
            if (Utility.IsPresentationFrameworkVersionLessThan4)
            {
                Utility.RemoveDependencyPropertyChangeListener(_window, System.Windows.Controls.Control.TemplateProperty, OnWindowPropertyChangedThatRequiresTemplateFixup);
                Utility.RemoveDependencyPropertyChangeListener(_window, FrameworkElement.FlowDirectionProperty, OnWindowPropertyChangedThatRequiresTemplateFixup);
            }
            if (_chromeInfo != null)
            {
                _chromeInfo.PropertyChangedThatRequiresRepaint -= OnChromePropertyChangedThatRequiresRepaint;
            }

            RestoreStandardChromeState(true);
        }

        public static WindowChromeWorker GetWindowChromeWorker(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            return (WindowChromeWorker)window.GetValue(WindowChromeWorkerProperty);
        }

        public static void SetWindowChromeWorker(Window window, WindowChromeWorker chrome)
        {
            Verify.IsNotNull(window, nameof(window));
            window.SetValue(WindowChromeWorkerProperty, chrome);
        }

        private void OnWindowPropertyChangedThatRequiresTemplateFixup(object sender, EventArgs e)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (_chromeInfo == null || _hwnd == IntPtr.Zero)
            {
                return;
            }
            // Assume that when the template changes it's going to be applied.
            // We don't have a good way to externally hook into the template
            // actually being applied, so we asynchronously post the fixup operation
            // at Loaded priority, so it's expected that the visual tree will be
            // updated before _FixupFrameworkIssues is called.
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (_Action)FixupFrameworkIssues);
        }

        private void ApplyNewCustomChrome()
        {
            // Not yet hooked.
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            if (_chromeInfo == null)
            {
                RestoreStandardChromeState(false);
                return;
            }
            if (!_isHooked)
            {
                _hwndSource.AddHook(WndProc);
                _isHooked = true;
            }
            FixupFrameworkIssues();
            // Force this the first time.
            UpdateSystemMenu(_window.WindowState);
            UpdateFrameState(true);

            PInvoke.SetWindowPos(
                new HWND(_hwnd),
                new HWND(IntPtr.Zero),
                0,
                0,
                0,
                0,
                _SwpFlags);
        }

        private void FixupFrameworkIssues()
        {
            Assert.IsNotNull(_chromeInfo);
            Assert.IsNotNull(_window);

            // This margin is only necessary if the client rect is going to be calculated incorrectly by WPF.
            // This bug was fixed in V4 of the framework.
            if (!Utility.IsPresentationFrameworkVersionLessThan4)
            {
                return;
            }
            // Nothing to fixup yet.  This will get called again when a template does get set.
            if (_window.Template == null)
            {
                return;
            }
            // Guard against the visual tree being empty.
            if (VisualTreeHelper.GetChildrenCount(_window) == 0)
            {
                // The template isn't null, but we don't have a visual tree.
                // Hope that ApplyTemplate is in the queue and repost this, because there's not much we can do right now.
                _window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (_Action)FixupFrameworkIssues);
                return;
            }
            var rootElement = (FrameworkElement)VisualTreeHelper.GetChild(_window, 0);

            PInvoke.GetWindowRect(new HWND(_hwnd), out RECT rcWindow);
            var rcAdjustedClient = GetAdjustedWindowRect(rcWindow);

            var rcLogicalWindow = DpiHelper.DeviceRectToLogical(new Rect(rcWindow.left, rcWindow.top, rcWindow.Width, rcWindow.Height));
            var rcLogicalClient = DpiHelper.DeviceRectToLogical(new Rect(rcAdjustedClient.left, rcAdjustedClient.top, rcAdjustedClient.Width, rcAdjustedClient.Height));

            var nonClientThickness = new Thickness(
               rcLogicalWindow.Left - rcLogicalClient.Left,
               rcLogicalWindow.Top - rcLogicalClient.Top,
               rcLogicalClient.Right - rcLogicalWindow.Right,
               rcLogicalClient.Bottom - rcLogicalWindow.Bottom);

            if (rootElement != null)
            {
                rootElement.Margin = new Thickness(0, 0, -(nonClientThickness.Left + nonClientThickness.Right), -(nonClientThickness.Top + nonClientThickness.Bottom));
            }

            // The negative thickness on the margin doesn't properly get applied in RTL layouts.
            // The width is right, but there is a black bar on the right.
            // To fix this we just add an additional RenderTransform to the root element.
            // This works fine, but if the window is dynamically changing its FlowDirection then this can have really bizarre side effects.
            // This will mostly work if the FlowDirection is dynamically changed, but there aren't many real scenarios that would call for
            // that so I'm not addressing the rest of the quirkiness.
            if (rootElement != null)
            {
                if (_window.FlowDirection == FlowDirection.RightToLeft)
                {
                    rootElement.RenderTransform = new MatrixTransform(1, 0, 0, 1, -(nonClientThickness.Left + nonClientThickness.Right), 0);
                }
                else
                {
                    rootElement.RenderTransform = null;
                }
            }

            if (_isFixedUp)
            {
                return;
            }

            _hasUserMovedWindow = false;
            _window.StateChanged += FixupRestoreBounds;
            _isFixedUp = true;
        }

        // There was a regression in DWM in Windows 7 with regard to handling WM_NCCALCSIZE to effect custom chrome.
        // When windows with glass are maximized on a multimonitor setup the glass frame tends to turn black.
        // Also when windows are resized they tend to flicker black, sometimes staying that way until resized again.
        //
        // This appears to be a bug in DWM related to device bitmap optimizations.  At least on RTM Win7 we can
        // evoke a legacy code path that bypasses the bug by calling an esoteric DWM function.  This doesn't affect
        // the system, just the application.
        // WPF also tends to call this function anyways during animations, so we're just forcing the issue
        // consistently and a bit earlier.
        private void FixupWindows7Issues()
        {
            if (_blackGlassFixupAttemptCount > 5)
            {
                // Don't keep trying if there's an endemic problem with this.
                return;
            }

            if (!Utility.IsOSWindows7OrNewer || !NativeMethods.DwmIsCompositionEnabled())
            {
                return;
            }

            ++_blackGlassFixupAttemptCount;
            var success = false;
            try
            {
                var dti = NativeMethods.DwmGetCompositionTimingInfo(_hwnd);
                success = dti != null;
            }
            catch (Exception e)
            {
                // We aren't sure of all the reasons this could fail.
                // If we find new ones we should consider making the NativeMethod swallow them as well.
                // Since we have a limited number of retries and this method isn't actually critical, just repost.
                Debug.Fail(e.Message);
            }

            // NativeMethods.DwmGetCompositionTimingInfo swallows E_PENDING.
            // If the call wasn't successful, try again later.
            if (!success)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (_Action)FixupWindows7Issues);
            }
            else
            {
                // Reset this.  We will want to force this again if DWM composition changes.
                _blackGlassFixupAttemptCount = 0;
            }
        }

        private void FixupRestoreBounds(object sender, EventArgs e)
        {
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (_window.WindowState != WindowState.Maximized && _window.WindowState != WindowState.Minimized)
            {
                return;
            }
            // Old versions of WPF sometimes force their incorrect idea of the Window's location
            // on the Win32 restore bounds.  If we have reason to think this is the case, then
            // try to undo what WPF did after it has done its thing.
            if (!_hasUserMovedWindow)
            {
                return;
            }

            _hasUserMovedWindow = false;
            var windowPlacement = new WINDOWPLACEMENT()
            {
                length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
            };

            PInvoke.GetWindowPlacement(new HWND(_hwnd), ref windowPlacement);
            var adjustedDeviceRc = GetAdjustedWindowRect(new RECT { bottom = 100, right = 100 });
            var adjustedTopLeft = DpiHelper.DevicePixelsToLogical(new Point(
                windowPlacement.rcNormalPosition.left - adjustedDeviceRc.left,
                windowPlacement.rcNormalPosition.top - adjustedDeviceRc.top));
            _window.Top = adjustedTopLeft.Y;
            _window.Left = adjustedTopLeft.X;
        }

        private RECT GetAdjustedWindowRect(RECT rcWindow)
        {
            // This should only be used to work around issues in the Framework that were fixed in 4.0
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            var style = (WINDOW_STYLE)NativeMethods.GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            var exstyle = (WINDOW_EX_STYLE)NativeMethods.GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

            PInvoke.AdjustWindowRectEx(ref rcWindow, style, false, exstyle);
            return rcWindow;
        }

        // Windows tries hard to hide this state from applications.
        // Generally you can tell that the window is in a docked position because the restore bounds from GetWindowPlacement
        // don't match the current window location and it's not in a maximized or minimized state.
        // Because this isn't doced or supported, it's also not incredibly consistent.  Sometimes some things get updated in
        // different orders, so this isn't absolutely reliable.
        private bool _IsWindowDocked
        {
            get
            {
                // We're only detecting this state to work around .Net 3.5 issues.
                // This logic won't work correctly when those issues are fixed.
                Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
                if (_window.WindowState != WindowState.Normal)
                {
                    return false;
                }

                var adjustedOffset = GetAdjustedWindowRect(new RECT { bottom = 100, right = 100 });
                var windowTopLeft = new Point(_window.Left, _window.Top);
                windowTopLeft -= (Vector)DpiHelper.DevicePixelsToLogical(new Point(adjustedOffset.left, adjustedOffset.top));
                return _window.RestoreBounds.Location != windowTopLeft;
            }
        }

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            // Only expecting messages for our cached HWND.
            Assert.AreEqual<nint>(hwnd, _hwnd);

            foreach (var handlePair in _messageTable)
            {
                if (handlePair.Key == msg)
                {
                    return handlePair.Value((uint)msg, new WPARAM((nuint)wParam), new LPARAM(lParam), out handled);
                }
            }

            return 0;
        }

        private LRESULT HandleSetTextOrIcon(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            var modified = ModifyStyle(WINDOW_STYLE.WS_VISIBLE, 0);

            // Setting the caption text and icon cause Windows to redraw the caption.
            // Letting the default WndProc handle the message without the WS_VISIBLE
            // style applied bypasses the redraw.
            var lRet = PInvoke.DefWindowProc(new HWND(_hwnd), uMsg, wParam, lParam);

            // Put back the style we removed.
            if (modified)
            {
                ModifyStyle(0, WINDOW_STYLE.WS_VISIBLE);
            }

            handled = true;
            return lRet;
        }

        private LRESULT HandleNCActivate(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // Despite MSDN's documentation of lParam not being used,
            // calling DefWindowProc with lParam set to -1 causes Windows not to draw over the caption.

            // Directly call DefWindowProc with a custom parameter
            // which bypasses any other handling of the message.
            var lRet = PInvoke.DefWindowProc(
                new HWND(_hwnd),
                PInvoke.WM_NCACTIVATE,
                wParam,
                new LPARAM(-1));
            handled = true;
            return lRet;
        }

        private static LRESULT HandleNcCalcSize(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // lParam is an [in, out] that can be either a RECT* (wParam == FALSE) or an NCCALCSIZE_PARAMS*.
            // Since the first field of NCCALCSIZE_PARAMS is a RECT and is the only field we care about
            // we can unconditionally treat it as a RECT.

            // Since we always want the client size to equal the window size, we can unconditionally handle it
            // without having to modify the parameters.
            handled = true;
            return new LRESULT((nint)PInvoke.WVR_REDRAW);
        }

        private LRESULT HandleNCHitTest(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            LRESULT lRet = new LRESULT(IntPtr.Zero);
            handled = false;

            // Give DWM a chance at this first.
            if (Utility.IsOSVistaOrNewer && _chromeInfo.GlassFrameThickness != default && _isGlassEnabled)
            {
                // If we're on Vista, give the DWM a chance to handle the message first.
                handled = PInvoke.DwmDefWindowProc(new HWND(_hwnd), uMsg, wParam, lParam, out lRet);
            }

            // Handle letting the system know if we consider the mouse to be in our effective non-client area.
            // If DWM already handled this by way of DwmDefWindowProc, then respect their call.
            if (lRet != IntPtr.Zero)
            {
                return lRet;
            }

            var mousePosScreen = new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam));
            var windowPosition = GetWindowRect();
            var ht = HitTestNca(DpiHelper.DeviceRectToLogical(windowPosition), DpiHelper.DevicePixelsToLogical(mousePosScreen));
            // Don't blindly respect HTCAPTION.
            // We want UIElements in the caption area to be actionable so run through a hittest first.
            if (ht != PInvoke.HTCLIENT)
            {
                var mousePosWindow = mousePosScreen;
                mousePosWindow.Offset(-windowPosition.X, -windowPosition.Y);
                mousePosWindow = DpiHelper.DevicePixelsToLogical(mousePosWindow);
                var inputElement = _window.InputHitTest(mousePosWindow);
                if (inputElement != null && WindowChrome.GetIsHitTestVisibleInChrome(inputElement))
                {
                    ht = PInvoke.HTCLIENT;
                }
            }
            handled = true;
            lRet = new LRESULT((int)ht);
            return lRet;
        }

        private LRESULT HandleNCRButtonUp(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // Emulate the system behavior of clicking the right mouse button over the caption area
            // to bring up the system menu.
            if (PInvoke.HTCAPTION == wParam.Value)
            {
                if (_window.ContextMenu != null)
                {
                    _window.ContextMenu.Placement = PlacementMode.MousePoint;
                    _window.ContextMenu.IsOpen = true;
                }
                else if (WindowChrome.GetWindowChrome(_window).ShowSystemMenu)
                {
                    SystemCommands.ShowSystemMenuPhysicalCoordinates(_window, new Point(Utility.GET_X_LPARAM(lParam), Utility.GET_Y_LPARAM(lParam)));
                }
            }
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleSize(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            const int SIZE_MAXIMIZED = 2;

            // Force when maximized.
            // We can tell what's happening right now, but the Window doesn't yet know it's
            // maximized.  Not forcing this update will eventually cause the
            // default caption to be drawn.
            WindowState? state = null;
            if ((int)wParam.Value == SIZE_MAXIMIZED)
            {
                state = WindowState.Maximized;
            }

            UpdateSystemMenu(state);

            // Still let the default WndProc handle this.
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleWindowPosChanged(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // http://blogs.msdn.com/oldnewthing/archive/2008/01/15/7113860.aspx
            // The WM_WINDOWPOSCHANGED message is sent at the end of the window
            // state change process. It sort of combines the other state change
            // notifications, WM_MOVE, WM_SIZE, and WM_SHOWWINDOW. But it doesn't
            // suffer from the same limitations as WM_SHOWWINDOW, so you can
            // reliably use it to react to the window being shown or hidden.

            UpdateSystemMenu(null);

            if (!_isGlassEnabled)
            {
                Assert.IsNotDefault(lParam);
                var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                SetRoundingRegion(wp);
            }

            // Still want to pass this to DefWndProc
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleDwmCompositionChanged(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            UpdateFrameState(false);
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleSettingChange(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // There are several settings that can cause fixups for the template to become invalid when changed.
            // These shouldn't be required on the v4 framework.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            FixupFrameworkIssues();
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleEnterSizeMove(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);

            _isUserResizing = true;

            // On Win7 if the user is dragging the window out of the maximized state then we don't want to use that location
            // as a restore point.
            Assert.Implies(_window.WindowState == WindowState.Maximized, Utility.IsOSWindows7OrNewer);

            // Check for the docked window case.  The window can still be restored when it's in this position so
            // try to account for that and not update the start position.
            if (_window.WindowState != WindowState.Maximized && !_IsWindowDocked)
            {
                _windowPosAtStartOfUserMove = new Point(_window.Left, _window.Top);
                // Realistically we also don't want to update the start position when moving from one docked state to another (or to and from maximized),
                // but it's tricky to detect and this is already a workaround for a bug that's fixed in newer versions of the framework.
                // Not going to try to handle all cases.
            }

            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleExitSizeMove(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            _isUserResizing = false;
            // On Win7 the user can change the Window's state by dragging the window to the top of the monitor.
            // If they did that, then we need to try to update the restore bounds or else WPF will put the window at the maximized location (e.g. (-8,-8)).
            if (_window.WindowState == WindowState.Maximized)
            {
                Assert.IsTrue(Utility.IsOSWindows7OrNewer);
                _window.Top = _windowPosAtStartOfUserMove.Y;
                _window.Left = _windowPosAtStartOfUserMove.X;
            }
            handled = false;
            return new LRESULT(0);
        }

        private LRESULT HandleMove(uint uMsg, WPARAM wParam, LPARAM lParam, out bool handled)
        {
            // This is only intercepted to deal with bugs in Window in .Net 3.5 and below.
            Assert.IsTrue(Utility.IsPresentationFrameworkVersionLessThan4);
            if (_isUserResizing)
            {
                _hasUserMovedWindow = true;
            }

            handled = false;
            return new LRESULT(0);
        }

        /// <summary>Add and remove a native WindowStyle from the HWND.</summary>
        /// <param name="removeStyle">The styles to be removed.  These can be bitwise combined.</param>
        /// <param name="addStyle">The styles to be added.  These can be bitwise combined.</param>
        /// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        private bool ModifyStyle(WINDOW_STYLE removeStyle, WINDOW_STYLE addStyle)
        {
            Assert.IsNotDefault(_hwnd);
            var dwStyle = (WINDOW_STYLE)NativeMethods.GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();
            var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
            if (dwStyle == dwNewStyle)
            {
                return false;
            }

            NativeMethods.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new IntPtr((int)dwNewStyle));
            return true;
        }

        /// <summary>
        /// Get the WindowState as the native HWND knows it to be.  This isn't necessarily the same as what Window thinks.
        /// </summary>
        private WindowState GetHwndState()
        {
            var windowPlacement = new WINDOWPLACEMENT()
            {
                length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>()
            };

            PInvoke.GetWindowPlacement(new HWND(_hwnd), ref windowPlacement);
            switch (windowPlacement.showCmd)
            {
                case SHOW_WINDOW_CMD.SW_SHOWMINIMIZED:
                    return WindowState.Minimized;

                case SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED:
                    return WindowState.Maximized;
            }
            return WindowState.Normal;
        }

        /// <summary>
        /// Get the bounding rectangle for the window in physical coordinates.
        /// </summary>
        /// <returns>The bounding rectangle for the window.</returns>
        private Rect GetWindowRect()
        {
            // Get the window rectangle.
            PInvoke.GetWindowRect(new HWND(_hwnd), out RECT windowPosition);
            return new Rect(windowPosition.left, windowPosition.top, windowPosition.Width, windowPosition.Height);
        }

        /// <summary>
        /// Update the items in the system menu based on the current, or assumed, WindowState.
        /// </summary>
        /// <param name="assumeState">
        /// The state to assume that the Window is in.  This can be null to query the Window's state.
        /// </param>
        /// <remarks>
        /// We want to update the menu while we have some control over whether the caption will be repainted.
        /// </remarks>
        private void UpdateSystemMenu(WindowState? assumeState)
        {
            const MENU_ITEM_FLAGS mfEnabled
                = MENU_ITEM_FLAGS.MF_ENABLED | MENU_ITEM_FLAGS.MF_BYCOMMAND;

            const MENU_ITEM_FLAGS mfDisabled
                = MENU_ITEM_FLAGS.MF_GRAYED
                | MENU_ITEM_FLAGS.MF_DISABLED
                | MENU_ITEM_FLAGS.MF_BYCOMMAND;

            var state = assumeState ?? GetHwndState();

            if (null == assumeState && _lastMenuState == state)
            {
                return;
            }

            _lastMenuState = state;

            var modified = ModifyStyle(WINDOW_STYLE.WS_VISIBLE, 0);
            SafeHandle menuSafeHandle = PInvoke.GetSystemMenu_SafeHandle(new HWND(_hwnd), false);
            if (menuSafeHandle.IsInvalid)
            {
                var dwStyle = (WINDOW_STYLE)NativeMethods.GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE).ToInt32();

                var canMinimize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_MINIMIZEBOX);
                var canMaximize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_MAXIMIZEBOX);
                var canSize = Utility.IsFlagSet((int)dwStyle, (int)WINDOW_STYLE.WS_THICKFRAME);

                switch (state)
                {
                    case WindowState.Maximized:
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_RESTORE, mfEnabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MOVE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_SIZE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MAXIMIZE, mfDisabled);
                        break;

                    case WindowState.Minimized:
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_RESTORE, mfEnabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MOVE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_SIZE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MINIMIZE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
                        break;

                    default:
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_RESTORE, mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MOVE, mfEnabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_SIZE, canSize ? mfEnabled : mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MINIMIZE, canMinimize ? mfEnabled : mfDisabled);
                        NativeMethods.EnableMenuItem(menuSafeHandle, PInvoke.SC_MAXIMIZE, canMaximize ? mfEnabled : mfDisabled);
                        break;
                }
            }
            if (modified)
            {
                ModifyStyle(0, WINDOW_STYLE.WS_VISIBLE);
            }
        }

        private void UpdateFrameState(bool force)
        {
            if (_hwnd == IntPtr.Zero)
            {
                return;
            }
            // Don't rely on SystemParameters2 for this, just make the check ourselves.
            var frameState = NativeMethods.DwmIsCompositionEnabled();

            if (!force && frameState == _isGlassEnabled)
            {
                return;
            }

            _isGlassEnabled = frameState && _chromeInfo.GlassFrameThickness != default;

            if (_isGlassEnabled)
            {
                ClearRoundingRegion();
                ExtendGlassFrame();
                FixupWindows7Issues();
            }
            else
            {
                SetRoundingRegion(null);
            }

            PInvoke.SetWindowPos(
                new HWND(_hwnd),
                new HWND(IntPtr.Zero),
                0,
                0,
                0,
                0,
                _SwpFlags);
        }

        private void ClearRoundingRegion()
        {
            var hwnd = new HWND(_hwnd);
            PInvoke.SetWindowRgn(hwnd, HRGN.Null, PInvoke.IsWindowVisible(hwnd));
        }

        private void SetRoundingRegion(WINDOWPOS? wp)
        {
            // We're early - WPF hasn't necessarily updated the state of the window.
            // Need to query it ourselves.
            var windowPlacement = new WINDOWPLACEMENT()
            {
                length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
            };

            PInvoke.GetWindowPlacement(new HWND(_hwnd), ref windowPlacement);

            if (windowPlacement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED)
            {
                int left;
                int top;

                if (wp.HasValue)
                {
                    left = wp.Value.x;
                    top = wp.Value.y;
                }
                else
                {
                    var r = GetWindowRect();
                    left = (int)r.Left;
                    top = (int)r.Top;
                }

                var hMonitor = PInvoke.MonitorFromWindow(new HWND(_hwnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

                var monitorInfo = new MONITORINFO()
                {
                    cbSize = (uint)Marshal.SizeOf<MONITORINFO>(),
                };

                PInvoke.GetMonitorInfo(hMonitor, ref monitorInfo);

                // The location of maximized window takes into account the border that Windows was
                // going to remove, so we also need to consider it.
                var rcMax = monitorInfo.rcWork.Offset(-left, -top);
                using var handle = PInvoke.CreateRectRgnIndirect(in rcMax);
                PInvoke.SetWindowRgn(new HWND(_hwnd), handle, true);
            }
            else
            {
                Size windowSize;

                // Use the size if it's specified.
                if (wp != null && !Utility.IsFlagSet((int)wp.Value.flags, (int)SET_WINDOW_POS_FLAGS.SWP_NOSIZE))
                {
                    windowSize = new Size((double)wp.Value.cx, (double)wp.Value.cy);
                }
                else if (wp != null && (_lastRoundingState == _window.WindowState))
                {
                    return;
                }
                else
                {
                    windowSize = GetWindowRect().Size;
                }

                _lastRoundingState = _window.WindowState;

                var shortestDimension = Math.Min(windowSize.Width, windowSize.Height);
                var topLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.TopLeft, 0)).X;
                topLeftRadius = Math.Min(topLeftRadius, shortestDimension / 2);
                var rect = IsUniform(_chromeInfo.CornerRadius) ?
                    new Rect(windowSize)
                    : new Rect(0, 0, windowSize.Width / 2 + topLeftRadius, windowSize.Height / 2 + topLeftRadius);

                using var hRegion = CreateRoundRectRgn(rect, topLeftRadius);

                var topRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.TopRight, 0)).X;
                topRightRadius = Math.Min(topRightRadius, shortestDimension / 2);
                var topRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + topRightRadius, windowSize.Height / 2 + topRightRadius);
                topRightRegionRect.Offset(windowSize.Width / 2 - topRightRadius, 0);
                Assert.AreEqual(topRightRegionRect.Right, windowSize.Width);

                CreateAndCombineRoundRectRgn(hRegion, topRightRegionRect, topRightRadius);

                var bottomLeftRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.BottomLeft, 0)).X;
                bottomLeftRadius = Math.Min(bottomLeftRadius, shortestDimension / 2);
                var bottomLeftRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomLeftRadius, windowSize.Height / 2 + bottomLeftRadius);
                bottomLeftRegionRect.Offset(0, windowSize.Height / 2 - bottomLeftRadius);
                Assert.AreEqual(bottomLeftRegionRect.Bottom, windowSize.Height);

                CreateAndCombineRoundRectRgn(hRegion, bottomLeftRegionRect, bottomLeftRadius);

                var bottomRightRadius = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.CornerRadius.BottomRight, 0)).X;
                bottomRightRadius = Math.Min(bottomRightRadius, shortestDimension / 2);
                var bottomRightRegionRect = new Rect(0, 0, windowSize.Width / 2 + bottomRightRadius, windowSize.Height / 2 + bottomRightRadius);
                bottomRightRegionRect.Offset(windowSize.Width / 2 - bottomRightRadius, windowSize.Height / 2 - bottomRightRadius);
                Assert.AreEqual(bottomRightRegionRect.Right, windowSize.Width);
                Assert.AreEqual(bottomRightRegionRect.Bottom, windowSize.Height);

                CreateAndCombineRoundRectRgn(hRegion, bottomRightRegionRect, bottomRightRadius);

                var hwnd = new HWND(_hwnd);
                PInvoke.SetWindowRgn(hwnd, hRegion, PInvoke.IsWindowVisible(hwnd));
            }
        }

        private static SafeHandle CreateRoundRectRgn(Rect region, double radius)
        {
            // Round outwards.
            if (DoubleUtilities.AreClose(0, radius))
            {
                return PInvoke.CreateRectRgn_SafeHandle(
                    (int)Math.Floor(region.Left),
                    (int)Math.Floor(region.Top),
                    (int)Math.Ceiling(region.Right),
                    (int)Math.Ceiling(region.Bottom));
            }

            // RoundedRect HRGNs require an additional pixel of padding on the bottom right to look correct.
            return PInvoke.CreateRoundRectRgn_SafeHandle(
                (int)Math.Floor(region.Left),
                (int)Math.Floor(region.Top),
                (int)Math.Ceiling(region.Right) + 1,
                (int)Math.Ceiling(region.Bottom) + 1,
                (int)Math.Ceiling(radius),
                (int)Math.Ceiling(radius));
        }

        private static void CreateAndCombineRoundRectRgn(SafeHandle hrgnSource, Rect region, double radius)
        {
            using SafeHandle hRegion = CreateRoundRectRgn(region, radius);
            var result = PInvoke.CombineRgn(hrgnSource, hrgnSource, hRegion, RGN_COMBINE_MODE.RGN_OR);
            if (result == GDI_REGION_TYPE.NULLREGION)
            {
                throw new InvalidOperationException("Unable to combine two HRGNs.");
            }
        }

        private static bool IsUniform(CornerRadius cornerRadius)
        {
            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.BottomRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.TopLeft, cornerRadius.TopRight))
            {
                return false;
            }

            if (!DoubleUtilities.AreClose(cornerRadius.BottomLeft, cornerRadius.TopRight))
            {
                return false;
            }

            return true;
        }

        private void ExtendGlassFrame()
        {
            Assert.IsNotNull(_window);

            // Expect that this might be called on OSes other than Vista.
            if (!Utility.IsOSVistaOrNewer)
            {
                // Not an error.  Just not on Vista so we're not going to get glass.
                return;
            }
            if (_hwnd == IntPtr.Zero)
            {
                // Can't do anything with this call until the Window has been shown.
                return;
            }
            // Ensure standard HWND background painting when DWM isn't enabled.
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                _hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            }
            else
            {
                // This makes the glass visible at a Win32 level so long as nothing else is covering it.
                // The Window's Background needs to be changed independent of this.

                // Apply the transparent background to the HWND
                _hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                // Thickness is going to be DIPs, need to convert to system coordinates.
                var deviceTopLeft = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.GlassFrameThickness.Left, _chromeInfo.GlassFrameThickness.Top));
                var deviceBottomRight = DpiHelper.LogicalPixelsToDevice(new Point(_chromeInfo.GlassFrameThickness.Right, _chromeInfo.GlassFrameThickness.Bottom));

                var dwmMargin = new MARGINS
                {
                    // err on the side of pushing in glass an extra pixel.
                    cxLeftWidth = (int)Math.Ceiling(deviceTopLeft.X),
                    cxRightWidth = (int)Math.Ceiling(deviceBottomRight.X),
                    cyTopHeight = (int)Math.Ceiling(deviceTopLeft.Y),
                    cyBottomHeight = (int)Math.Ceiling(deviceBottomRight.Y),
                };

                PInvoke.DwmExtendFrameIntoClientArea(new HWND(_hwnd), in dwmMargin);
            }
        }

        /// <summary>
        /// Matrix of the HT values to return when responding to NC window messages.
        /// </summary>
        private static readonly uint[,] HitTestBorders = {
            { PInvoke.HTTOPLEFT,    PInvoke.HTTOP,     PInvoke.HTTOPRIGHT    },
            { PInvoke.HTLEFT,       PInvoke.HTCLIENT,  PInvoke.HTRIGHT       },
            { PInvoke.HTBOTTOMLEFT, PInvoke.HTBOTTOM,  PInvoke.HTBOTTOMRIGHT },
        };

        private uint HitTestNca(Rect windowPosition, Point mousePosition)
        {
            // Determine if hit test is for resizing, default middle (1,1).
            var uRow = 1;
            var uCol = 1;
            var onResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (mousePosition.Y >= windowPosition.Top && mousePosition.Y < windowPosition.Top + _chromeInfo.ResizeBorderThickness.Top + _chromeInfo.CaptionHeight)
            {
                onResizeBorder = (mousePosition.Y < (windowPosition.Top + _chromeInfo.ResizeBorderThickness.Top));
                uRow = 0; // top (caption or resize border)
            }
            else if (mousePosition.Y < windowPosition.Bottom && mousePosition.Y >= windowPosition.Bottom - (int)_chromeInfo.ResizeBorderThickness.Bottom)
            {
                uRow = 2; // bottom
            }

            // Determine if the point is at the left or right of the window.
            if (mousePosition.X >= windowPosition.Left && mousePosition.X < windowPosition.Left + (int)_chromeInfo.ResizeBorderThickness.Left)
            {
                uCol = 0; // left side
            }
            else if (mousePosition.X < windowPosition.Right && mousePosition.X >= windowPosition.Right - _chromeInfo.ResizeBorderThickness.Right)
            {
                uCol = 2; // right side
            }

            // If the cursor is in one of the top edges by the caption bar, but below the top resize border,
            // then resize left-right rather than diagonally.
            if (uRow == 0 && uCol != 1 && !onResizeBorder)
            {
                uRow = 1;
            }

            var ht = HitTestBorders[uRow, uCol];
            if (ht == PInvoke.HTTOP && !onResizeBorder)
            {
                ht = PInvoke.HTCAPTION;
            }

            return ht;
        }

        // Remove Custom Chrome Methods

        private void RestoreStandardChromeState(bool isClosing)
        {
            VerifyAccess();
            UnhookCustomChrome();
            if (isClosing)
            {
                return;
            }

            RestoreFrameworkIssueFixups();
            RestoreGlassFrame();
            RestoreHrgn();
            _window.InvalidateMeasure();
        }

        private void UnhookCustomChrome()
        {
            Assert.IsNotDefault(_hwnd);
            Assert.IsNotNull(_window);
            if (!_isHooked)
            {
                return;
            }

            _hwndSource.RemoveHook(WndProc);
            _isHooked = false;
        }

        private void RestoreFrameworkIssueFixups()
        {
            // This margin is only necessary if the client rect is going to be calculated incorrectly by WPF.
            // This bug was fixed in V4 of the framework.
            if (!Utility.IsPresentationFrameworkVersionLessThan4)
            {
                return;
            }

            Assert.IsTrue(_isFixedUp);
            var rootElement = (FrameworkElement)VisualTreeHelper.GetChild(_window, 0);
            if (rootElement != null)
            {
                // Undo anything that was done before.
                rootElement.Margin = new Thickness();
            }
            _window.StateChanged -= FixupRestoreBounds;
            _isFixedUp = false;
        }

        private void RestoreGlassFrame()
        {
            Assert.IsNull(_chromeInfo);
            Assert.IsNotNull(_window);

            // Expect that this might be called on OSes other than Vista
            // and if the window hasn't yet been shown, then we don't need to undo anything.
            if (!Utility.IsOSVistaOrNewer || _hwnd == IntPtr.Zero)
            {
                return;
            }

            _hwndSource.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
            if (!NativeMethods.DwmIsCompositionEnabled())
            {
                return;
            }
            // If glass is enabled, push it back to the normal bounds.
            var dwmMargin = new MARGINS();
            PInvoke.DwmExtendFrameIntoClientArea(new HWND(_hwnd), in dwmMargin);
        }

        private void RestoreHrgn()
        {
            ClearRoundingRegion();
            PInvoke.SetWindowPos(
                new HWND(_hwnd),
                new HWND(IntPtr.Zero),
                0,
                0,
                0,
                0,
                _SwpFlags);
        }
    }
}