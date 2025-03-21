﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using AvalonDock.Diagnostics;

using Microsoft.Win32.SafeHandles;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace AvalonDock.Controls.Shell.Standard;

/// <summary>
/// Represents a safe device context (DC) handle.
/// </summary>
#nullable enable
internal sealed class SafeDC : SafeHandleZeroOrMinusOneIsInvalid
{
    private HWND? _hWND;

    private SafeDC(HDC hDC)
        : base(true)
    {
        HDC = hDC;
        SetHandle(HDC.Value);
    }

    /// <summary>
    /// Gets the handle to the device context (DC).
    /// </summary>
    public HDC HDC { get; private set; }

#if NET5_0_OR_GREATER
    [MemberNotNull(nameof(_hWND))]
#endif
    internal static SafeDC GetDC(HWND hWND)
    {
        HDC hDC = PInvoke.GetDC(hWND);
        if (hDC.Value == IntPtr.Zero)
        {
            Trace.TraceError("Failed to get device context.");
            new HRESULT(HResultCodes.E_FAIL).ThrowOnFailure();
        }

        SafeDC safeDC = new(hDC)
        {
            _hWND = hWND
        };

        if (safeDC.IsInvalid)
        {
            Trace.TraceError("Invalid SafeDC object detected.");
            new HRESULT(HResultCodes.E_FAIL).ThrowOnFailure();
        }

        return safeDC;
    }

    /// <summary>
    /// Retrieves a safe device context (DC) handle for the desktop.
    /// </summary>
    /// <returns>A safe device context (DC) handle for the desktop.</returns>
    internal static SafeDC GetDesktop()
    {
        return GetDC(HWND.Null);
    }

    /// <inheritdoc/>
    protected override bool ReleaseHandle()
    {
        if (!_hWND.HasValue || _hWND.Value == IntPtr.Zero)
        {
            return false;
        }

        return PInvoke.ReleaseDC(_hWND.Value, HDC) == 1;
    }
}
