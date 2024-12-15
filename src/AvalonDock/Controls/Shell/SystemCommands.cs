/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using Standard;

namespace Microsoft.Windows.Shell
{
    public static class SystemCommands
    {
        public static RoutedCommand CloseWindowCommand { get; }
        public static RoutedCommand MaximizeWindowCommand { get; }
        public static RoutedCommand MinimizeWindowCommand { get; }
        public static RoutedCommand RestoreWindowCommand { get; }
        public static RoutedCommand ShowSystemMenuCommand { get; }

        static SystemCommands()
        {
            CloseWindowCommand = new RoutedCommand(nameof(CloseWindow), typeof(SystemCommands));
            MaximizeWindowCommand = new RoutedCommand(nameof(MaximizeWindow), typeof(SystemCommands));
            MinimizeWindowCommand = new RoutedCommand(nameof(MinimizeWindow), typeof(SystemCommands));
            RestoreWindowCommand = new RoutedCommand(nameof(RestoreWindow), typeof(SystemCommands));
            ShowSystemMenuCommand = new RoutedCommand(nameof(ShowSystemMenu), typeof(SystemCommands));
        }

        private static void PostSystemCommand(Window window, uint command)
        {
            var hWnd = new HWND(new WindowInteropHelper(window).Handle);
            if (hWnd == IntPtr.Zero || !PInvoke.IsWindow(hWnd))
            {
                return;
            }

            PInvoke.PostMessage(hWnd, PInvoke.WM_SYSCOMMAND, new WPARAM(command), IntPtr.Zero);
        }

        public static void CloseWindow(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            PostSystemCommand(window, PInvoke.SC_CLOSE);
        }

        public static void MaximizeWindow(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            PostSystemCommand(window, PInvoke.SC_MAXIMIZE);
        }

        public static void MinimizeWindow(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            PostSystemCommand(window, PInvoke.SC_MINIMIZE);
        }

        public static void RestoreWindow(Window window)
        {
            Verify.IsNotNull(window, nameof(window));
            PostSystemCommand(window, PInvoke.SC_RESTORE);
        }

        /// <summary>Display the system menu at a specified location.</summary>
        /// <param name="window"></param>
        /// <param name="screenLocation">The location to display the system menu, in logical screen coordinates.</param>
        public static void ShowSystemMenu(Window window, Point screenLocation)
        {
            Verify.IsNotNull(window, nameof(window));
            ShowSystemMenuPhysicalCoordinates(window, DpiHelper.LogicalPixelsToDevice(screenLocation));
        }

        internal static void ShowSystemMenuPhysicalCoordinates(Window window, Point physicalScreenLocation)
        {
            Verify.IsNotNull(window, nameof(window));
            var hWnd = new HWND(new WindowInteropHelper(window).Handle);
            if (hWnd == IntPtr.Zero || !PInvoke.IsWindow(hWnd))
            {
                return;
            }

            // Reseting seems to fix an issue where the system menu would not open a second time
            // TODO: Figure out why the state is not being reset properly
            using var _ = PInvoke.GetSystemMenu_SafeHandle(hWnd, true);
            using SafeHandle hMenu = PInvoke.GetSystemMenu_SafeHandle(hWnd, false);
            var cmd = PInvoke.TrackPopupMenuEx(
                hMenu,
                (uint)(TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD),
                (int)physicalScreenLocation.X,
                (int)physicalScreenLocation.Y,
                hWnd,
                null).Value;

            if (cmd != 0)
            {
                PInvoke.PostMessage(hWnd, PInvoke.WM_SYSCOMMAND, new WPARAM((nuint)cmd), IntPtr.Zero);
            }
        }
    }
}