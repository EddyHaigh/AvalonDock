/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

using Windows.Win32.Foundation;

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace AvalonDock.Diagnostics
{
    /// <summary>Wrapper for HRESULT status codes.</summary>
    [StructLayout(LayoutKind.Explicit)]
    internal static class HResultCodes
    {
        // NOTE: These public field declarations are automatically
        // picked up by ToString through reflection.
        /// <summary>S_OK</summary>
        public const int S_OK = 0x00000000;

        /// <summary>S_FALSE</summary>
        public const int S_FALSE = 0x00000001;

        /// <summary>E_PENDING</summary>
        public const int E_PENDING = unchecked((int)0x8000000A);

        /// <summary>E_NOTIMPL</summary>
        public const int E_NOTIMPL = unchecked((int)0x80004001);

        /// <summary>E_NOINTERFACE</summary>
        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        /// <summary>E_POINTER</summary>
        public const int E_POINTER = unchecked((int)0x80004003);

        /// <summary>E_ABORT</summary>
        public const int E_ABORT = unchecked((int)0x80004004);

        /// <summary>E_FAIL</summary>
        public const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>E_UNEXPECTED</summary>
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

        /// <summary>STG_E_INVALIDFUNCTION</summary>
        public const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);

        /// <summary>REGDB_E_CLASSNOTREG</summary>
        public const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        /// <summary>DESTS_E_NO_MATCHING_ASSOC_HANDLER.  Win7 internal error code for Jump Lists.</summary>
        /// <remarks>There is no Assoc Handler for the given item registered by the specified application.</remarks>
        public const int DESTS_E_NO_MATCHING_ASSOC_HANDLER = unchecked((int)0x80040F03);

        /// <summary>DESTS_E_NORECDOCS.  Win7 internal error code for Jump Lists.</summary>
        /// <remarks>The given item is excluded from the recent docs folder by the NoRecDocs bit on its registration.</remarks>
        public const int DESTS_E_NORECDOCS = unchecked((int)0x80040F04);

        /// <summary>DESTS_E_NOTALLCLEARED.  Win7 internal error code for Jump Lists.</summary>
        /// <remarks>Not all of the items were successfully cleared</remarks>
        public const int DESTS_E_NOTALLCLEARED = unchecked((int)0x80040F05);

        /// <summary>E_ACCESSDENIED</summary>
        /// <remarks>Win32Error ERROR_ACCESS_DENIED.</remarks>
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);

        /// <summary>E_OUTOFMEMORY</summary>
        /// <remarks>Win32Error ERROR_OUTOFMEMORY.</remarks>
        public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

        /// <summary>E_INVALIDARG</summary>
        /// <remarks>Win32Error ERROR_INVALID_PARAMETER.</remarks>
        public const int E_INVALIDARG = unchecked((int)0x80070057);

        /// <summary>INTSAFE_E_ARITHMETIC_OVERFLOW</summary>
        public const int INTSAFE_E_ARITHMETIC_OVERFLOW = unchecked((int)0x80070216);

        /// <summary>COR_E_OBJECTDISPOSED</summary>
        public const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);

        /// <summary>WC_E_GREATERTHAN</summary>
        public const int WC_E_GREATERTHAN = unchecked((int)0xC00CEE23);

        /// <summary>WC_E_SYNTAX</summary>
        public const int WC_E_SYNTAX = unchecked((int)0xC00CEE2D);
    }
}