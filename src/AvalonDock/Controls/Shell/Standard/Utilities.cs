/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using AvalonDock.Controls.Shell.Standard;
using AvalonDock.Diagnostics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace Standard
{
    internal static partial class Utility
    {
        private static readonly Version _osVersion = Environment.OSVersion.Version;
        private static readonly Version _presentationFrameworkVersion = Assembly.GetAssembly(typeof(Window)).GetName().Version;

        // This can be cached.  It's not going to change under reasonable circumstances.
        private static int s_bitDepth;

        public static bool IsOSVistaOrNewer => _osVersion >= new Version(6, 0);

        public static bool IsOSWindows7OrNewer => _osVersion >= new Version(6, 1);

        public static bool IsOSWindows8OrNewer => _osVersion >= new Version(6, 2);

        /// <summary>
        /// Is this using WPF4?
        /// </summary>
        /// <remarks>
        /// There are a few specific bugs in Window in 3.5SP1 and below that require workarounds
        /// when handling WM_NCCALCSIZE on the HWND.
        /// </remarks>
        public static bool IsPresentationFrameworkVersionLessThan4 => _presentationFrameworkVersion < new Version(4, 0);

        public static void AddDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }

            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);
            var dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.AddValueChanged(component, listener);
        }

        /// <summary>Convert a native integer that represent a color with an alpha channel into a Color struct.</summary>
        /// <param name="color">The integer that represents the color.  Its bits are of the format 0xAARRGGBB.</param>
        /// <returns>A Color representation of the parameter.</returns>
        public static Color ColorFromArgbDword(uint color) => Color.FromArgb((byte)((color & 0xFF000000) >> 24), (byte)((color & 0x00FF0000) >> 16), (byte)((color & 0x0000FF00) >> 8), (byte)((color & 0x000000FF) >> 0));

        public static int GET_X_LPARAM(IntPtr lParam) => LOWORD(lParam.ToInt32());

        public static int GET_Y_LPARAM(IntPtr lParam) => HIWORD(lParam.ToInt32());

        public static int HIWORD(int i) => (short)(i >> 16);

        public static bool IsCornerRadiusValid(CornerRadius cornerRadius)
        {
            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopRight))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomRight))
            {
                return false;
            }

            return true;
        }

        public static bool IsDoubleFiniteAndNonNegative(double d) => !double.IsNaN(d) && !double.IsInfinity(d) && !(d < 0);

        public static bool IsFlagSet(int value, int mask) => (value & mask) != 0;

        public static bool IsFlagSet(uint value, uint mask) => (value & mask) != 0;

        public static bool IsFlagSet(long value, long mask) => (value & mask) != 0;

        public static bool IsFlagSet(ulong value, ulong mask) => (value & mask) != 0;

        public static bool IsThicknessNonNegative(Thickness thickness)
        {
            if (!IsDoubleFiniteAndNonNegative(thickness.Top))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Left))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Bottom))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Right))
            {
                return false;
            }

            return true;
        }

        public static int LOWORD(int i) => (short)(i & 0xFFFF);

        public static void RemoveDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }

            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);
            var dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.RemoveValueChanged(component, listener);
        }

        /// <summary>The native RGB macro.</summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int RGB(Color c) => c.R | (c.G << 8) | (c.B << 16);
        public static void SafeDestroyWindow(ref IntPtr hwnd)
        {
            HWND p = new(hwnd);
            if (PInvoke.IsWindow(p))
            {
                PInvoke.DestroyWindow(p);
            }
        }

        public static void SafeDispose<T>(ref T disposable) where T : IDisposable
        {
            // Dispose can safely be called on an object multiple times.
            IDisposable t = disposable;
            disposable = default;
            t?.Dispose();
        }

        public static void SafeFreeHGlobal(ref IntPtr hglobal)
        {
            var p = hglobal;
            hglobal = IntPtr.Zero;
            if (p != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(p);
            }
        }

        /// From a list of BitmapFrames find the one that best matches the requested dimensions.
        /// The methods used here are copied from Win32 sources.  We want to be consistent with
        /// system behaviors.
        private static BitmapFrame _GetBestMatch(IList<BitmapFrame> frames, int bitDepth, int width, int height)
        {
            var bestScore = int.MaxValue;
            var bestBpp = 0;
            var bestIndex = 0;
            var isBitmapIconDecoder = frames[0].Decoder is IconBitmapDecoder;
            for (var i = 0; i < frames.Count && bestScore != 0; ++i)
            {
                var currentIconBitDepth = isBitmapIconDecoder ? frames[i].Thumbnail.Format.BitsPerPixel : frames[i].Format.BitsPerPixel;
                if (currentIconBitDepth == 0)
                {
                    currentIconBitDepth = 8;
                }

                var score = _MatchImage(frames[i], bitDepth, width, height, currentIconBitDepth);
                if (score < bestScore)
                {
                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                    bestScore = score;
                }
                else if (score == bestScore)
                {
                    // Tie breaker: choose the higher color depth.  If that fails, choose first one.
                    if (bestBpp >= currentIconBitDepth)
                    {
                        continue;
                    }

                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                }
            }
            return frames[bestIndex];
        }

        private static int _GetBitDepth()
        {
            if (s_bitDepth != 0)
            {
                return s_bitDepth;
            }

            using (var safeDC = SafeDC.GetDesktop())
            {

                s_bitDepth =
                    PInvoke.GetDeviceCaps(safeDC.HDC, GET_DEVICE_CAPS_INDEX.BITSPIXEL)
                    * PInvoke.GetDeviceCaps(safeDC.HDC, GET_DEVICE_CAPS_INDEX.PLANES);
            }

            return s_bitDepth;
        }

        private static byte _IntToHex(int n)
        {
            Assert.BoundedInteger(0, n, 16);
            return n <= 9 ? (byte)(n + '0') : (byte)(n - 10 + 'A');
        }

        private static bool _IsAsciiAlphaNumeric(byte b) => b >= 'a' && b <= 'z' || b >= 'A' && b <= 'Z' || b >= '0' && b <= '9';

        private static int _MatchImage(BitmapFrame frame, int bitDepth, int width, int height, int bpp)
        {
            return 2 * _WeightedAbs(bpp, bitDepth, false) +
                            _WeightedAbs(frame.PixelWidth, width, true) +
                            _WeightedAbs(frame.PixelHeight, height, true);
        }

        private static int _WeightedAbs(int valueHave, int valueWant, bool fPunish)
        {
            var diff = valueHave - valueWant;
            return diff >= 0 ? diff : (fPunish ? -2 : -1) * diff;
        }
 // = 0;
        private class _UrlDecoder
        {
            private readonly byte[] _byteBuffer;
            private readonly char[] _charBuffer;
            private readonly Encoding _encoding;
            private int _byteCount;
            private int _charCount;

            public _UrlDecoder(int size, Encoding encoding)
            {
                _encoding = encoding;
                _charBuffer = new char[size];
                _byteBuffer = new byte[size];
            }

            public void AddByte(byte b) => _byteBuffer[_byteCount++] = b;

            public void AddChar(char ch)
            {
                _FlushBytes();
                _charBuffer[_charCount++] = ch;
            }

            public string GetString()
            {
                _FlushBytes();
                return _charCount > 0 ? new string(_charBuffer, 0, _charCount) : "";
            }

            private void _FlushBytes()
            {
                if (_byteCount <= 0)
                {
                    return;
                }

                _charCount += _encoding.GetChars(_byteBuffer, 0, _byteCount, _charBuffer, _charCount);
                _byteCount = 0;
            }
        }
    }
}