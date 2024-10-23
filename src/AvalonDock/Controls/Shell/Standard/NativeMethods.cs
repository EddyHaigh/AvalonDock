/************************************************************************
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
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Text;

    using AvalonDock.Controls.Shell.Standard;

    using Windows.Win32.Foundation;

    // Some COM interfaces and Win32 structures are already declared in the framework.
    // Interesting ones to remember in System.Runtime.InteropServices.ComTypes are:
    using IStream = System.Runtime.InteropServices.ComTypes.IStream;

    // Native Values

    /// <summary>Delegate declaration that matches managed WndProc signatures.</summary>
    internal delegate IntPtr MessageHandler(WM uMsg, IntPtr wParam, IntPtr lParam, out bool handled);

    /// <summary>Delegate declaration that matches native WndProc signatures.</summary>
    internal delegate IntPtr WndProc(IntPtr hwnd, WM uMsg, IntPtr wParam, IntPtr lParam);

    /// <summary>Delegate declaration that matches native WndProc signatures.</summary>
    internal delegate IntPtr WndProcHook(IntPtr hwnd, WM uMsg, IntPtr wParam, IntPtr lParam, ref bool handled);

    /// <summary>
    /// AC_*
    /// </summary>
    internal enum AC : byte
    {
        SRC_OVER = 0,
        SRC_ALPHA = 1,
    }

    internal enum CombineRgnResult
    {
        ERROR = 0,
        NULLREGION = 1,
        SIMPLEREGION = 2,
        COMPLEXREGION = 3,
    }

    /// <summary>
    /// CS_*
    /// </summary>
    [Flags]
    internal enum CS : uint
    {
        VREDRAW = 0x0001,
        HREDRAW = 0x0002,
        DBLCLKS = 0x0008,
        OWNDC = 0x0020,
        CLASSDC = 0x0040,
        PARENTDC = 0x0080,
        NOCLOSE = 0x0200,
        SAVEBITS = 0x0800,
        BYTEALIGNCLIENT = 0x1000,
        BYTEALIGNWINDOW = 0x2000,
        GLOBALCLASS = 0x4000,
        IME = 0x00010000,
        DROPSHADOW = 0x00020000
    }

    /// <summary>
    /// GetWindowLongPtr values, GWL_*
    /// </summary>
    internal enum GWL
    {
        WNDPROC = (-4),
        HINSTANCE = (-6),
        HWNDPARENT = (-8),
        STYLE = (-16),
        EXSTYLE = (-20),
        USERDATA = (-21),
        ID = (-12)
    }

    /// <summary>
    /// HIGHCONTRAST flags
    /// </summary>
    [Flags]
    internal enum HCF
    {
        HIGHCONTRASTON = 0x00000001,
        AVAILABLE = 0x00000002,
        HOTKEYACTIVE = 0x00000004,
        CONFIRMHOTKEY = 0x00000008,
        HOTKEYSOUND = 0x00000010,
        INDICATOR = 0x00000020,
        HOTKEYAVAILABLE = 0x00000040,
    }

    /// <summary>
    /// Non-client hit test values, HT*
    /// </summary>
    internal enum HT
    {
        ERROR = -2,
        TRANSPARENT = -1,
        NOWHERE = 0,
        CLIENT = 1,
        CAPTION = 2,
        SYSMENU = 3,
        GROWBOX = 4,
        SIZE = GROWBOX,
        MENU = 5,
        HSCROLL = 6,
        VSCROLL = 7,
        MINBUTTON = 8,
        MAXBUTTON = 9,
        LEFT = 10,
        RIGHT = 11,
        TOP = 12,
        TOPLEFT = 13,
        TOPRIGHT = 14,
        BOTTOM = 15,
        BOTTOMLEFT = 16,
        BOTTOMRIGHT = 17,
        BORDER = 18,
        REDUCE = MINBUTTON,
        ZOOM = MAXBUTTON,
        SIZEFIRST = LEFT,
        SIZELAST = BOTTOMRIGHT,
        OBJECT = 19,
        CLOSE = 20,
        HELP = 21
    }

    /// <summary>
    /// EnableMenuItem uEnable values, MF_*
    /// </summary>
    [Flags]
    internal enum MF : uint
    {
        /// <summary>
        /// Possible return value for EnableMenuItem
        /// </summary>
        DOES_NOT_EXIST = unchecked((uint)-1),

        ENABLED = 0,
        BYCOMMAND = 0,
        GRAYED = 1,
        DISABLED = 2,
    }

    /// <summary>
    /// CombingRgn flags.  RGN_*
    /// </summary>
    internal enum RGN
    {
        /// <summary>
        /// Creates the intersection of the two combined regions.
        /// </summary>
        AND = 1,

        /// <summary>
        /// Creates the union of two combined regions.
        /// </summary>
        OR = 2,

        /// <summary>
        /// Creates the union of two combined regions except for any overlapping areas.
        /// </summary>
        XOR = 3,

        /// <summary>
        /// Combines the parts of hrgnSrc1 that are not part of hrgnSrc2.
        /// </summary>
        DIFF = 4,

        /// <summary>
        /// Creates a copy of the region identified by hrgnSrc1.
        /// </summary>
        COPY = 5,
    }

    internal enum SC : uint
    {
        SIZE = 0xF000,
        MOVE = 0xF010,
        MINIMIZE = 0xF020,
        MAXIMIZE = 0xF030,
        NEXTWINDOW = 0xF040,
        PREVWINDOW = 0xF050,
        CLOSE = 0xF060,
        VSCROLL = 0xF070,
        HSCROLL = 0xF080,
        MOUSEMENU = 0xF090,
        KEYMENU = 0xF100,
        ARRANGE = 0xF110,
        RESTORE = 0xF120,
        TASKLIST = 0xF130,
        SCREENSAVE = 0xF140,
        HOTKEY = 0xF150,
        DEFAULT = 0xF160,
        MONITORPOWER = 0xF170,
        CONTEXTHELP = 0xF180,
        SEPARATOR = 0xF00F,

        /// <summary>SCF_ISSECURE</summary>
        F_ISSECURE = 0x00000001,

        ICON = MINIMIZE,
        ZOOM = MAXIMIZE,
    }

    [Flags]
    internal enum SLGP
    {
        SHORTPATH = 0x1,
        UNCPRIORITY = 0x2,
        RAWPATH = 0x4
    }

    /// <summary>
    /// SystemMetrics.  SM_*
    /// </summary>
    internal enum SM
    {
        CXSCREEN = 0,
        CYSCREEN = 1,
        CXVSCROLL = 2,
        CYHSCROLL = 3,
        CYCAPTION = 4,
        CXBORDER = 5,
        CYBORDER = 6,
        CXFIXEDFRAME = 7,
        CYFIXEDFRAME = 8,
        CYVTHUMB = 9,
        CXHTHUMB = 10,
        CXICON = 11,
        CYICON = 12,
        CXCURSOR = 13,
        CYCURSOR = 14,
        CYMENU = 15,
        CXFULLSCREEN = 16,
        CYFULLSCREEN = 17,
        CYKANJIWINDOW = 18,
        MOUSEPRESENT = 19,
        CYVSCROLL = 20,
        CXHSCROLL = 21,
        DEBUG = 22,
        SWAPBUTTON = 23,
        CXMIN = 28,
        CYMIN = 29,
        CXSIZE = 30,
        CYSIZE = 31,
        CXFRAME = 32,
        CXSIZEFRAME = CXFRAME,
        CYFRAME = 33,
        CYSIZEFRAME = CYFRAME,
        CXMINTRACK = 34,
        CYMINTRACK = 35,
        CXDOUBLECLK = 36,
        CYDOUBLECLK = 37,
        CXICONSPACING = 38,
        CYICONSPACING = 39,
        MENUDROPALIGNMENT = 40,
        PENWINDOWS = 41,
        DBCSENABLED = 42,
        CMOUSEBUTTONS = 43,
        SECURE = 44,
        CXEDGE = 45,
        CYEDGE = 46,
        CXMINSPACING = 47,
        CYMINSPACING = 48,
        CXSMICON = 49,
        CYSMICON = 50,
        CYSMCAPTION = 51,
        CXSMSIZE = 52,
        CYSMSIZE = 53,
        CXMENUSIZE = 54,
        CYMENUSIZE = 55,
        ARRANGE = 56,
        CXMINIMIZED = 57,
        CYMINIMIZED = 58,
        CXMAXTRACK = 59,
        CYMAXTRACK = 60,
        CXMAXIMIZED = 61,
        CYMAXIMIZED = 62,
        NETWORK = 63,
        CLEANBOOT = 67,
        CXDRAG = 68,
        CYDRAG = 69,
        SHOWSOUNDS = 70,
        CXMENUCHECK = 71,
        CYMENUCHECK = 72,
        SLOWMACHINE = 73,
        MIDEASTENABLED = 74,
        MOUSEWHEELPRESENT = 75,
        XVIRTUALSCREEN = 76,
        YVIRTUALSCREEN = 77,
        CXVIRTUALSCREEN = 78,
        CYVIRTUALSCREEN = 79,
        CMONITORS = 80,
        SAMEDISPLAYFORMAT = 81,
        IMMENABLED = 82,
        CXFOCUSBORDER = 83,
        CYFOCUSBORDER = 84,
        TABLETPC = 86,
        MEDIACENTER = 87,
        REMOTESESSION = 0x1000,
        REMOTECONTROL = 0x2001,
    }

    /// <summary>
    /// SystemParameterInfo values, SPI_*
    /// </summary>
    internal enum SPI
    {
        GETBEEP = 0x0001,
        SETBEEP = 0x0002,
        GETMOUSE = 0x0003,
        SETMOUSE = 0x0004,
        GETBORDER = 0x0005,
        SETBORDER = 0x0006,
        GETKEYBOARDSPEED = 0x000A,
        SETKEYBOARDSPEED = 0x000B,
        LANGDRIVER = 0x000C,
        ICONHORIZONTALSPACING = 0x000D,
        GETSCREENSAVETIMEOUT = 0x000E,
        SETSCREENSAVETIMEOUT = 0x000F,
        GETSCREENSAVEACTIVE = 0x0010,
        SETSCREENSAVEACTIVE = 0x0011,
        GETGRIDGRANULARITY = 0x0012,
        SETGRIDGRANULARITY = 0x0013,
        SETDESKWALLPAPER = 0x0014,
        SETDESKPATTERN = 0x0015,
        GETKEYBOARDDELAY = 0x0016,
        SETKEYBOARDDELAY = 0x0017,
        ICONVERTICALSPACING = 0x0018,
        GETICONTITLEWRAP = 0x0019,
        SETICONTITLEWRAP = 0x001A,
        GETMENUDROPALIGNMENT = 0x001B,
        SETMENUDROPALIGNMENT = 0x001C,
        SETDOUBLECLKWIDTH = 0x001D,
        SETDOUBLECLKHEIGHT = 0x001E,
        GETICONTITLELOGFONT = 0x001F,
        SETDOUBLECLICKTIME = 0x0020,
        SETMOUSEBUTTONSWAP = 0x0021,
        SETICONTITLELOGFONT = 0x0022,
        GETFASTTASKSWITCH = 0x0023,
        SETFASTTASKSWITCH = 0x0024,

        SETDRAGFULLWINDOWS = 0x0025,
        GETDRAGFULLWINDOWS = 0x0026,
        GETNONCLIENTMETRICS = 0x0029,
        SETNONCLIENTMETRICS = 0x002A,
        GETMINIMIZEDMETRICS = 0x002B,
        SETMINIMIZEDMETRICS = 0x002C,
        GETICONMETRICS = 0x002D,
        SETICONMETRICS = 0x002E,
        SETWORKAREA = 0x002F,
        GETWORKAREA = 0x0030,
        SETPENWINDOWS = 0x0031,
        GETHIGHCONTRAST = 0x0042,
        SETHIGHCONTRAST = 0x0043,
        GETKEYBOARDPREF = 0x0044,
        SETKEYBOARDPREF = 0x0045,
        GETSCREENREADER = 0x0046,
        SETSCREENREADER = 0x0047,
        GETANIMATION = 0x0048,
        SETANIMATION = 0x0049,
        GETFONTSMOOTHING = 0x004A,
        SETFONTSMOOTHING = 0x004B,
        SETDRAGWIDTH = 0x004C,
        SETDRAGHEIGHT = 0x004D,
        SETHANDHELD = 0x004E,
        GETLOWPOWERTIMEOUT = 0x004F,
        GETPOWEROFFTIMEOUT = 0x0050,
        SETLOWPOWERTIMEOUT = 0x0051,
        SETPOWEROFFTIMEOUT = 0x0052,
        GETLOWPOWERACTIVE = 0x0053,
        GETPOWEROFFACTIVE = 0x0054,
        SETLOWPOWERACTIVE = 0x0055,
        SETPOWEROFFACTIVE = 0x0056,
        SETCURSORS = 0x0057,
        SETICONS = 0x0058,
        GETDEFAULTINPUTLANG = 0x0059,
        SETDEFAULTINPUTLANG = 0x005A,
        SETLANGTOGGLE = 0x005B,
        GETWINDOWSEXTENSION = 0x005C,
        SETMOUSETRAILS = 0x005D,
        GETMOUSETRAILS = 0x005E,
        SETSCREENSAVERRUNNING = 0x0061,
        SCREENSAVERRUNNING = SETSCREENSAVERRUNNING,
        GETFILTERKEYS = 0x0032,
        SETFILTERKEYS = 0x0033,
        GETTOGGLEKEYS = 0x0034,
        SETTOGGLEKEYS = 0x0035,
        GETMOUSEKEYS = 0x0036,
        SETMOUSEKEYS = 0x0037,
        GETSHOWSOUNDS = 0x0038,
        SETSHOWSOUNDS = 0x0039,
        GETSTICKYKEYS = 0x003A,
        SETSTICKYKEYS = 0x003B,
        GETACCESSTIMEOUT = 0x003C,
        SETACCESSTIMEOUT = 0x003D,

        GETSERIALKEYS = 0x003E,
        SETSERIALKEYS = 0x003F,
        GETSOUNDSENTRY = 0x0040,
        SETSOUNDSENTRY = 0x0041,
        GETSNAPTODEFBUTTON = 0x005F,
        SETSNAPTODEFBUTTON = 0x0060,
        GETMOUSEHOVERWIDTH = 0x0062,
        SETMOUSEHOVERWIDTH = 0x0063,
        GETMOUSEHOVERHEIGHT = 0x0064,
        SETMOUSEHOVERHEIGHT = 0x0065,
        GETMOUSEHOVERTIME = 0x0066,
        SETMOUSEHOVERTIME = 0x0067,
        GETWHEELSCROLLLINES = 0x0068,
        SETWHEELSCROLLLINES = 0x0069,
        GETMENUSHOWDELAY = 0x006A,
        SETMENUSHOWDELAY = 0x006B,

        GETWHEELSCROLLCHARS = 0x006C,
        SETWHEELSCROLLCHARS = 0x006D,

        GETSHOWIMEUI = 0x006E,
        SETSHOWIMEUI = 0x006F,

        GETMOUSESPEED = 0x0070,
        SETMOUSESPEED = 0x0071,
        GETSCREENSAVERRUNNING = 0x0072,
        GETDESKWALLPAPER = 0x0073,

        GETAUDIODESCRIPTION = 0x0074,
        SETAUDIODESCRIPTION = 0x0075,

        GETSCREENSAVESECURE = 0x0076,
        SETSCREENSAVESECURE = 0x0077,

        GETHUNGAPPTIMEOUT = 0x0078,
        SETHUNGAPPTIMEOUT = 0x0079,
        GETWAITTOKILLTIMEOUT = 0x007A,
        SETWAITTOKILLTIMEOUT = 0x007B,
        GETWAITTOKILLSERVICETIMEOUT = 0x007C,
        SETWAITTOKILLSERVICETIMEOUT = 0x007D,
        GETMOUSEDOCKTHRESHOLD = 0x007E,
        SETMOUSEDOCKTHRESHOLD = 0x007F,
        GETPENDOCKTHRESHOLD = 0x0080,
        SETPENDOCKTHRESHOLD = 0x0081,
        GETWINARRANGING = 0x0082,
        SETWINARRANGING = 0x0083,
        GETMOUSEDRAGOUTTHRESHOLD = 0x0084,
        SETMOUSEDRAGOUTTHRESHOLD = 0x0085,
        GETPENDRAGOUTTHRESHOLD = 0x0086,
        SETPENDRAGOUTTHRESHOLD = 0x0087,
        GETMOUSESIDEMOVETHRESHOLD = 0x0088,
        SETMOUSESIDEMOVETHRESHOLD = 0x0089,
        GETPENSIDEMOVETHRESHOLD = 0x008A,
        SETPENSIDEMOVETHRESHOLD = 0x008B,
        GETDRAGFROMMAXIMIZE = 0x008C,
        SETDRAGFROMMAXIMIZE = 0x008D,
        GETSNAPSIZING = 0x008E,
        SETSNAPSIZING = 0x008F,
        GETDOCKMOVING = 0x0090,
        SETDOCKMOVING = 0x0091,

        GETACTIVEWINDOWTRACKING = 0x1000,
        SETACTIVEWINDOWTRACKING = 0x1001,
        GETMENUANIMATION = 0x1002,
        SETMENUANIMATION = 0x1003,
        GETCOMBOBOXANIMATION = 0x1004,
        SETCOMBOBOXANIMATION = 0x1005,
        GETLISTBOXSMOOTHSCROLLING = 0x1006,
        SETLISTBOXSMOOTHSCROLLING = 0x1007,
        GETGRADIENTCAPTIONS = 0x1008,
        SETGRADIENTCAPTIONS = 0x1009,
        GETKEYBOARDCUES = 0x100A,
        SETKEYBOARDCUES = 0x100B,
        GETMENUUNDERLINES = GETKEYBOARDCUES,
        SETMENUUNDERLINES = SETKEYBOARDCUES,
        GETACTIVEWNDTRKZORDER = 0x100C,
        SETACTIVEWNDTRKZORDER = 0x100D,
        GETHOTTRACKING = 0x100E,
        SETHOTTRACKING = 0x100F,
        GETMENUFADE = 0x1012,
        SETMENUFADE = 0x1013,
        GETSELECTIONFADE = 0x1014,
        SETSELECTIONFADE = 0x1015,
        GETTOOLTIPANIMATION = 0x1016,
        SETTOOLTIPANIMATION = 0x1017,
        GETTOOLTIPFADE = 0x1018,
        SETTOOLTIPFADE = 0x1019,
        GETCURSORSHADOW = 0x101A,
        SETCURSORSHADOW = 0x101B,
        GETMOUSESONAR = 0x101C,
        SETMOUSESONAR = 0x101D,
        GETMOUSECLICKLOCK = 0x101E,
        SETMOUSECLICKLOCK = 0x101F,
        GETMOUSEVANISH = 0x1020,
        SETMOUSEVANISH = 0x1021,
        GETFLATMENU = 0x1022,
        SETFLATMENU = 0x1023,
        GETDROPSHADOW = 0x1024,
        SETDROPSHADOW = 0x1025,
        GETBLOCKSENDINPUTRESETS = 0x1026,
        SETBLOCKSENDINPUTRESETS = 0x1027,

        GETUIEFFECTS = 0x103E,
        SETUIEFFECTS = 0x103F,

        GETDISABLEOVERLAPPEDCONTENT = 0x1040,
        SETDISABLEOVERLAPPEDCONTENT = 0x1041,
        GETCLIENTAREAANIMATION = 0x1042,
        SETCLIENTAREAANIMATION = 0x1043,
        GETCLEARTYPE = 0x1048,
        SETCLEARTYPE = 0x1049,
        GETSPEECHRECOGNITION = 0x104A,
        SETSPEECHRECOGNITION = 0x104B,

        GETFOREGROUNDLOCKTIMEOUT = 0x2000,
        SETFOREGROUNDLOCKTIMEOUT = 0x2001,
        GETACTIVEWNDTRKTIMEOUT = 0x2002,
        SETACTIVEWNDTRKTIMEOUT = 0x2003,
        GETFOREGROUNDFLASHCOUNT = 0x2004,
        SETFOREGROUNDFLASHCOUNT = 0x2005,
        GETCARETWIDTH = 0x2006,
        SETCARETWIDTH = 0x2007,

        GETMOUSECLICKLOCKTIME = 0x2008,
        SETMOUSECLICKLOCKTIME = 0x2009,
        GETFONTSMOOTHINGTYPE = 0x200A,
        SETFONTSMOOTHINGTYPE = 0x200B,

        GETFONTSMOOTHINGCONTRAST = 0x200C,
        SETFONTSMOOTHINGCONTRAST = 0x200D,

        GETFOCUSBORDERWIDTH = 0x200E,
        SETFOCUSBORDERWIDTH = 0x200F,
        GETFOCUSBORDERHEIGHT = 0x2010,
        SETFOCUSBORDERHEIGHT = 0x2011,

        GETFONTSMOOTHINGORIENTATION = 0x2012,
        SETFONTSMOOTHINGORIENTATION = 0x2013,

        GETMINIMUMHITRADIUS = 0x2014,
        SETMINIMUMHITRADIUS = 0x2015,
        GETMESSAGEDURATION = 0x2016,
        SETMESSAGEDURATION = 0x2017,
    }

    /// <summary>
    /// SystemParameterInfo flag values, SPIF_*
    /// </summary>
    [Flags]
    internal enum SPIF
    {
        None = 0,
        UPDATEINIFILE = 0x01,
        SENDCHANGE = 0x02,
        SENDWININICHANGE = SENDCHANGE,
    }

    [Flags]
    internal enum STATE_SYSTEM
    {
        UNAVAILABLE = 0x00000001, // Disabled
        SELECTED = 0x00000002,
        FOCUSED = 0x00000004,
        PRESSED = 0x00000008,
        CHECKED = 0x00000010,
        MIXED = 0x00000020,  // 3-state checkbox or toolbar button
        INDETERMINATE = MIXED,
        READONLY = 0x00000040,
        HOTTRACKED = 0x00000080,
        DEFAULT = 0x00000100,
        EXPANDED = 0x00000200,
        COLLAPSED = 0x00000400,
        BUSY = 0x00000800,
        FLOATING = 0x00001000,  // Children "owned" not "contained" by parent
        MARQUEED = 0x00002000,
        ANIMATED = 0x00004000,
        INVISIBLE = 0x00008000,
        OFFSCREEN = 0x00010000,
        SIZEABLE = 0x00020000,
        MOVEABLE = 0x00040000,
        SELFVOICING = 0x00080000,
        FOCUSABLE = 0x00100000,
        SELECTABLE = 0x00200000,
        LINKED = 0x00400000,
        TRAVERSED = 0x00800000,
        MULTISELECTABLE = 0x01000000,  // Supports multiple selection
        EXTSELECTABLE = 0x02000000,  // Supports extended selection
        ALERT_LOW = 0x04000000,  // This information is of low priority
        ALERT_MEDIUM = 0x08000000,  // This information is of medium priority
        ALERT_HIGH = 0x10000000,  // This information is of high priority
        PROTECTED = 0x20000000,  // access to this is restricted
        VALID = 0x3FFFFFFF,
    }

    /// <summary>GDI+ Status codes</summary>
    internal enum Status
    {
        Ok = 0,
        GenericError = 1,
        InvalidParameter = 2,
        OutOfMemory = 3,
        ObjectBusy = 4,
        InsufficientBuffer = 5,
        NotImplemented = 6,
        Win32Error = 7,
        WrongState = 8,
        Aborted = 9,
        FileNotFound = 10,
        ValueOverflow = 11,
        AccessDenied = 12,
        UnknownImageFormat = 13,
        FontFamilyNotFound = 14,
        FontStyleNotFound = 15,
        NotTrueTypeFont = 16,
        UnsupportedGdiplusVersion = 17,
        GdiplusNotInitialized = 18,
        PropertyNotFound = 19,
        PropertyNotSupported = 20,
        ProfileNotFound = 21,
    }

    internal enum StockObject : int
    {
        WHITE_BRUSH = 0,
        LTGRAY_BRUSH = 1,
        GRAY_BRUSH = 2,
        DKGRAY_BRUSH = 3,
        BLACK_BRUSH = 4,
        NULL_BRUSH = 5,
        HOLLOW_BRUSH = NULL_BRUSH,
        WHITE_PEN = 6,
        BLACK_PEN = 7,
        NULL_PEN = 8,
        SYSTEM_FONT = 13,
        DEFAULT_PALETTE = 15,
    }

    /// <summary>
    /// ShowWindow options
    /// </summary>
    internal enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        NORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        MAXIMIZE = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
        FORCEMINIMIZE = 11,
    }

    /// <summary>SetWindowPos options</summary>
    [Flags]
    internal enum SWP
    {
        ASYNCWINDOWPOS = 0x4000,
        DEFERERASE = 0x2000,
        DRAWFRAME = 0x0020,
        FRAMECHANGED = 0x0020,
        HIDEWINDOW = 0x0080,
        NOACTIVATE = 0x0010,
        NOCOPYBITS = 0x0100,
        NOMOVE = 0x0002,
        NOOWNERZORDER = 0x0200,
        NOREDRAW = 0x0008,
        NOREPOSITION = 0x0200,
        NOSENDCHANGING = 0x0400,
        NOSIZE = 0x0001,
        NOZORDER = 0x0004,
        SHOWWINDOW = 0x0040,
    }

    /// <summary>
    /// Window message values, WM_*
    /// </summary>
    internal enum WM : uint
    {
        NULL = 0x0000,
        CREATE = 0x0001,
        DESTROY = 0x0002,
        MOVE = 0x0003,
        SIZE = 0x0005,
        ACTIVATE = 0x0006,
        SETFOCUS = 0x0007,
        KILLFOCUS = 0x0008,
        ENABLE = 0x000A,
        SETREDRAW = 0x000B,
        SETTEXT = 0x000C,
        GETTEXT = 0x000D,
        GETTEXTLENGTH = 0x000E,
        PAINT = 0x000F,
        CLOSE = 0x0010,
        QUERYENDSESSION = 0x0011,
        QUIT = 0x0012,
        QUERYOPEN = 0x0013,
        ERASEBKGND = 0x0014,
        SYSCOLORCHANGE = 0x0015,
        SHOWWINDOW = 0x0018,
        CTLCOLOR = 0x0019,
        WININICHANGE = 0x001A,
        SETTINGCHANGE = 0x001A,
        ACTIVATEAPP = 0x001C,
        SETCURSOR = 0x0020,
        MOUSEACTIVATE = 0x0021,
        CHILDACTIVATE = 0x0022,
        QUEUESYNC = 0x0023,
        GETMINMAXINFO = 0x0024,

        WINDOWPOSCHANGING = 0x0046,
        WINDOWPOSCHANGED = 0x0047,

        CONTEXTMENU = 0x007B,
        STYLECHANGING = 0x007C,
        STYLECHANGED = 0x007D,
        DISPLAYCHANGE = 0x007E,
        GETICON = 0x007F,
        SETICON = 0x0080,
        NCCREATE = 0x0081,
        NCDESTROY = 0x0082,
        NCCALCSIZE = 0x0083,
        NCHITTEST = 0x0084,
        NCPAINT = 0x0085,
        NCACTIVATE = 0x0086,
        GETDLGCODE = 0x0087,
        SYNCPAINT = 0x0088,
        NCMOUSEMOVE = 0x00A0,
        NCLBUTTONDOWN = 0x00A1,
        NCLBUTTONUP = 0x00A2,
        NCLBUTTONDBLCLK = 0x00A3,
        NCRBUTTONDOWN = 0x00A4,
        NCRBUTTONUP = 0x00A5,
        NCRBUTTONDBLCLK = 0x00A6,
        NCMBUTTONDOWN = 0x00A7,
        NCMBUTTONUP = 0x00A8,
        NCMBUTTONDBLCLK = 0x00A9,

        SYSKEYDOWN = 0x0104,
        SYSKEYUP = 0x0105,
        SYSCHAR = 0x0106,
        SYSDEADCHAR = 0x0107,
        COMMAND = 0x0111,
        SYSCOMMAND = 0x0112,

        MOUSEMOVE = 0x0200,
        LBUTTONDOWN = 0x0201,
        LBUTTONUP = 0x0202,
        LBUTTONDBLCLK = 0x0203,
        RBUTTONDOWN = 0x0204,
        RBUTTONUP = 0x0205,
        RBUTTONDBLCLK = 0x0206,
        MBUTTONDOWN = 0x0207,
        MBUTTONUP = 0x0208,
        MBUTTONDBLCLK = 0x0209,
        MOUSEWHEEL = 0x020A,
        XBUTTONDOWN = 0x020B,
        XBUTTONUP = 0x020C,
        XBUTTONDBLCLK = 0x020D,
        MOUSEHWHEEL = 0x020E,
        PARENTNOTIFY = 0x0210,

        CAPTURECHANGED = 0x0215,
        POWERBROADCAST = 0x0218,
        DEVICECHANGE = 0x0219,

        ENTERSIZEMOVE = 0x0231,
        EXITSIZEMOVE = 0x0232,

        IME_SETCONTEXT = 0x0281,
        IME_NOTIFY = 0x0282,
        IME_CONTROL = 0x0283,
        IME_COMPOSITIONFULL = 0x0284,
        IME_SELECT = 0x0285,
        IME_CHAR = 0x0286,
        IME_REQUEST = 0x0288,
        IME_KEYDOWN = 0x0290,
        IME_KEYUP = 0x0291,

        NCMOUSELEAVE = 0x02A2,

        TABLET_DEFBASE = 0x02C0,
        //WM_TABLET_MAXOFFSET = 0x20,

        TABLET_ADDED = TABLET_DEFBASE + 8,
        TABLET_DELETED = TABLET_DEFBASE + 9,
        TABLET_FLICK = TABLET_DEFBASE + 11,
        TABLET_QUERYSYSTEMGESTURESTATUS = TABLET_DEFBASE + 12,

        CUT = 0x0300,
        COPY = 0x0301,
        PASTE = 0x0302,
        CLEAR = 0x0303,
        UNDO = 0x0304,
        RENDERFORMAT = 0x0305,
        RENDERALLFORMATS = 0x0306,
        DESTROYCLIPBOARD = 0x0307,
        DRAWCLIPBOARD = 0x0308,
        PAINTCLIPBOARD = 0x0309,
        VSCROLLCLIPBOARD = 0x030A,
        SIZECLIPBOARD = 0x030B,
        ASKCBFORMATNAME = 0x030C,
        CHANGECBCHAIN = 0x030D,
        HSCROLLCLIPBOARD = 0x030E,
        QUERYNEWPALETTE = 0x030F,
        PALETTEISCHANGING = 0x0310,
        PALETTECHANGED = 0x0311,
        HOTKEY = 0x0312,
        PRINT = 0x0317,
        PRINTCLIENT = 0x0318,
        APPCOMMAND = 0x0319,
        THEMECHANGED = 0x031A,

        DWMCOMPOSITIONCHANGED = 0x031E,
        DWMNCRENDERINGCHANGED = 0x031F,
        DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        DWMWINDOWMAXIMIZEDCHANGE = 0x0321,

        GETTITLEBARINFOEX = 0x033F,

        // Windows 7

        DWMSENDICONICTHUMBNAIL = 0x0323,
        DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,

        USER = 0x0400,

        // This is the hard-coded message value used by WinForms for Shell_NotifyIcon.
        // It's relatively safe to reuse.
        TRAYMOUSEMESSAGE = 0x800, //WM_USER + 1024

        APP = 0x8000,
    }

    /// <summary>
    /// WindowStyle values, WS_*
    /// </summary>
    [Flags]
    internal enum WS : uint
    {
        OVERLAPPED = 0x00000000,
        POPUP = 0x80000000,
        CHILD = 0x40000000,
        MINIMIZE = 0x20000000,
        VISIBLE = 0x10000000,
        DISABLED = 0x08000000,
        CLIPSIBLINGS = 0x04000000,
        CLIPCHILDREN = 0x02000000,
        MAXIMIZE = 0x01000000,
        BORDER = 0x00800000,
        DLGFRAME = 0x00400000,
        VSCROLL = 0x00200000,
        HSCROLL = 0x00100000,
        SYSMENU = 0x00080000,
        THICKFRAME = 0x00040000,
        GROUP = 0x00020000,
        TABSTOP = 0x00010000,

        MINIMIZEBOX = 0x00020000,
        MAXIMIZEBOX = 0x00010000,

        CAPTION = BORDER | DLGFRAME,
        TILED = OVERLAPPED,
        ICONIC = MINIMIZE,
        SIZEBOX = THICKFRAME,
        TILEDWINDOW = OVERLAPPEDWINDOW,

        OVERLAPPEDWINDOW = OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,
        POPUPWINDOW = POPUP | BORDER | SYSMENU,
        CHILDWINDOW = CHILD,
    }

    /// <summary>
    /// Window style extended values, WS_EX_*
    /// </summary>
    [Flags]
    internal enum WS_EX : uint
    {
        None = 0,
        DLGMODALFRAME = 0x00000001,
        NOPARENTNOTIFY = 0x00000004,
        TOPMOST = 0x00000008,
        ACCEPTFILES = 0x00000010,
        TRANSPARENT = 0x00000020,
        MDICHILD = 0x00000040,
        TOOLWINDOW = 0x00000080,
        WINDOWEDGE = 0x00000100,
        CLIENTEDGE = 0x00000200,
        CONTEXTHELP = 0x00000400,
        RIGHT = 0x00001000,
        LEFT = 0x00000000,
        RTLREADING = 0x00002000,
        LTRREADING = 0x00000000,
        LEFTSCROLLBAR = 0x00004000,
        RIGHTSCROLLBAR = 0x00000000,
        CONTROLPARENT = 0x00010000,
        STATICEDGE = 0x00020000,
        APPWINDOW = 0x00040000,
        LAYERED = 0x00080000,
        NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
        LAYOUTRTL = 0x00400000, // Right to left mirroring
        COMPOSITED = 0x02000000,
        NOACTIVATE = 0x08000000,
        OVERLAPPEDWINDOW = (WINDOWEDGE | CLIENTEDGE),
        PALETTEWINDOW = (WINDOWEDGE | TOOLWINDOW | TOPMOST),
    }

    internal enum WVR
    {
        ALIGNTOP = 0x0010,
        ALIGNLEFT = 0x0020,
        ALIGNBOTTOM = 0x0040,
        ALIGNRIGHT = 0x0080,
        HREDRAW = 0x0100,
        VREDRAW = 0x0200,
        VALIDRECTS = 0x0400,
        REDRAW = HREDRAW | VREDRAW,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BLENDFUNCTION
    {
        // Must be AC_SRC_OVER
        public AC BlendOp;

        // Must be 0.
        public byte BlendFlags;

        // Alpha transparency between 0 (transparent) - 255 (opaque)
        public byte SourceConstantAlpha;

        // Must be AC_SRC_ALPHA
        public AC AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CREATESTRUCT
    {
        public IntPtr lpCreateParams;
        public IntPtr hInstance;
        public IntPtr hMenu;
        public IntPtr hwndParent;
        public int cy;
        public int cx;
        public int y;
        public int x;
        public WS style;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClass;

        public WS_EX dwExStyle;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DWM_TIMING_INFO
    {
        public int cbSize;
        public UNSIGNED_RATIO rateRefresh;
        public ulong qpcRefreshPeriod;
        public UNSIGNED_RATIO rateCompose;
        public ulong qpcVBlank;
        public ulong cRefresh;
        public uint cDXRefresh;
        public ulong qpcCompose;
        public ulong cFrame;
        public uint cDXPresent;
        public ulong cRefreshFrame;
        public ulong cFrameSubmitted;
        public uint cDXPresentSubmitted;
        public ulong cFrameConfirmed;
        public uint cDXPresentConfirmed;
        public ulong cRefreshConfirmed;
        public uint cDXRefreshConfirmed;
        public ulong cFramesLate;
        public uint cFramesOutstanding;
        public ulong cFrameDisplayed;
        public ulong qpcFrameDisplayed;
        public ulong cRefreshFrameDisplayed;
        public ulong cFrameComplete;
        public ulong qpcFrameComplete;
        public ulong cFramePending;
        public ulong qpcFramePending;
        public ulong cFramesDisplayed;
        public ulong cFramesComplete;
        public ulong cFramesPending;
        public ulong cFramesAvailable;
        public ulong cFramesDropped;
        public ulong cFramesMissed;
        public ulong cRefreshNextDisplayed;
        public ulong cRefreshNextPresented;
        public ulong cRefreshesDisplayed;
        public ulong cRefreshesPresented;
        public ulong cRefreshStarted;
        public ulong cPixelsReceived;
        public ulong cPixelsDrawn;
        public ulong cBuffersEmpty;
    }

    // Native Types
    [StructLayout(LayoutKind.Sequential)]
    internal struct HIGHCONTRAST
    {
        public int cbSize;
        public HCF dwFlags;

        //[MarshalAs(UnmanagedType.LPWStr, SizeConst=80)]
        //public String lpszDefaultScheme;
        public IntPtr lpszDefaultScheme;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS
    {
        /// <summary>Width of left border that retains its size.</summary>
        public int cxLeftWidth;

        /// <summary>Width of right border that retains its size.</summary>
        public int cxRightWidth;

        /// <summary>Height of top border that retains its size.</summary>
        public int cyTopHeight;

        /// <summary>Height of bottom border that retains its size.</summary>
        public int cyBottomHeight;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct NONCLIENTMETRICS
    {
        public int cbSize;
        public int iBorderWidth;
        public int iScrollWidth;
        public int iScrollHeight;
        public int iCaptionWidth;
        public int iCaptionHeight;
        public LOGFONT lfCaptionFont;
        public int iSmCaptionWidth;
        public int iSmCaptionHeight;
        public LOGFONT lfSmCaptionFont;
        public int iMenuWidth;
        public int iMenuHeight;
        public LOGFONT lfMenuFont;
        public LOGFONT lfStatusFont;
        public LOGFONT lfMessageFont;

        // Vista only
        public int iPaddedBorderWidth;

        public static NONCLIENTMETRICS VistaMetricsStruct
        {
            get
            {
                var ncm = new NONCLIENTMETRICS
                {
                    cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS))
                };
                return ncm;
            }
        }

        public static NONCLIENTMETRICS XPMetricsStruct
        {
            get
            {
                var ncm = new NONCLIENTMETRICS
                {
                    // Account for the missing iPaddedBorderWidth
                    cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS)) - sizeof(int)
                };
                return ncm;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int x;
        public int y;
    }

    // New to Vista.
    [StructLayout(LayoutKind.Sequential)]
    internal struct TITLEBARINFOEX
    {
        public int cbSize;
        public RECT rcTitleBar;
        public STATE_SYSTEM rgstate_TitleBar;
        public STATE_SYSTEM rgstate_Reserved;
        public STATE_SYSTEM rgstate_MinimizeButton;
        public STATE_SYSTEM rgstate_MaximizeButton;
        public STATE_SYSTEM rgstate_HelpButton;
        public STATE_SYSTEM rgstate_CloseButton;
        public RECT rgrect_TitleBar;
        public RECT rgrect_Reserved;
        public RECT rgrect_MinimizeButton;
        public RECT rgrect_MaximizeButton;
        public RECT rgrect_HelpButton;
        public RECT rgrect_CloseButton;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UNSIGNED_RATIO
    {
        public uint uiNumerator;
        public uint uiDenominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WNDCLASSEX
    {
        public int cbSize;
        public CS style;
        public WndProc lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;

        public IntPtr hIconSm;
    }

    // Some native methods are shimmed through public versions that handle converting failures into thrown exceptions.
    internal static class NativeMethods
    {
        [DllImport("gdi32.dll")]
        public static extern CombineRgnResult CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, RGN fnCombineMode);

        public static IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect)
        {
            var ret = _CreateRectRgn(nLeftRect, nTopRect, nRightRect, nBottomRect);
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        public static IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse)
        {
            var ret = _CreateRoundRectRgn(nLeftRect, nTopRect, nRightRect, nBottomRect, nWidthEllipse, nHeightEllipse);
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        public static IntPtr CreateWindowEx(
            WS_EX dwExStyle,
            string lpClassName,
            string lpWindowName,
            WS dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam)
        {
            var ret = _CreateWindowEx(dwExStyle, lpClassName, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
            if (ret == IntPtr.Zero)
            {
                HRESULT.ThrowLastError();
            }

            return ret;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DefWindowProcW")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, WM Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("dwmapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DwmDefWindowProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam, out IntPtr plResult);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        public static bool DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend)
        {
            // Make this call safe to make on downlevel OSes...
            if (Utility.IsOSVistaOrNewer && IsThemeActive())
            {
                var hr = _DwmGetColorizationColor(out pcrColorization, out pfOpaqueBlend);
                if (hr.Succeeded)
                {
                    return true;
                }
            }

            // Default values.  If for some reason the native DWM API fails it's never enough of a reason
            // to bring down the app.  Empirically it still sometimes returns errors even when the theme service is on.
            // We'll still use the boolean return value to allow the caller to respond if they care.
            pcrColorization = 0xFF000000;
            pfOpaqueBlend = true;

            return false;
        }

        public static DWM_TIMING_INFO? DwmGetCompositionTimingInfo(IntPtr hwnd)
        {
            if (!Utility.IsOSVistaOrNewer)
            {
                return null; // API was new to Vista.
            }

            var dti = new DWM_TIMING_INFO { cbSize = Marshal.SizeOf(typeof(DWM_TIMING_INFO)) };
            var hr = _DwmGetCompositionTimingInfo(Utility.IsOSWindows8OrNewer ? IntPtr.Zero : hwnd, ref dti);
            if (hr == HRESULT.E_PENDING)
            {
                return null; // The system isn't yet ready to respond.  Return null rather than throw.
            }

            hr.ThrowIfFailed();
            return dti;
        }

        public static bool DwmIsCompositionEnabled()
        {
            // Make this call safe to make on downlevel OSes...
            if (!Utility.IsOSVistaOrNewer)
            {
                return false;
            }
            return _DwmIsCompositionEnabled();
        }

        public static MF EnableMenuItem(IntPtr hMenu, SC uIDEnableItem, MF uEnable)
        {
            // Returns the previous state of the menu item, or -1 if the menu item does not exist.
            var iRet = _EnableMenuItem(hMenu, uIDEnableItem, uEnable);
            return (MF)iRet;
        }

        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateBitmapFromStream(IStream stream, out IntPtr bitmap);

        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr hbmReturn);

        [DllImport("gdiplus.dll")]
        public static extern Status GdipDisposeImage(IntPtr image);

        public static void GetCurrentThemeName(out string themeFileName, out string color, out string size)
        {
            // Not expecting strings longer than MAX_PATH.  We will return the error
            var fileNameBuilder = new StringBuilder((int)Win32Value.MAX_PATH);
            var colorBuilder = new StringBuilder((int)Win32Value.MAX_PATH);
            var sizeBuilder = new StringBuilder((int)Win32Value.MAX_PATH);

            // This will throw if the theme service is not active (e.g. not UxTheme!IsThemeActive).
            _GetCurrentThemeName(fileNameBuilder, fileNameBuilder.Capacity,
                                 colorBuilder, colorBuilder.Capacity,
                                 sizeBuilder, sizeBuilder.Capacity)
                .ThrowIfFailed();

            themeFileName = fileNameBuilder.ToString();
            color = colorBuilder.ToString();
            size = sizeBuilder.ToString();
        }

        public static IntPtr GetModuleHandle(string lpModuleName)
        {
            var retPtr = _GetModuleHandle(lpModuleName);
            if (retPtr == IntPtr.Zero)
            {
                HRESULT.ThrowLastError();
            }

            return retPtr;
        }

        public static IntPtr GetStockObject(StockObject fnObject)
        {
            var retPtr = _GetStockObject(fnObject);
            if (retPtr == null)
            {
                HRESULT.ThrowLastError();
            }

            return retPtr;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(SM nIndex);

        // This is aliased as a macro in 32bit Windows.
        public static IntPtr GetWindowLongPtr(IntPtr hwnd, GWL nIndex)
        {
            var ret = IntPtr.Zero;
            ret = IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, nIndex) : new IntPtr(GetWindowLongPtr32(hwnd, nIndex));
            if (ret == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return ret;
        }

        [DllImport("uxtheme.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsThemeActive();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        // Note that this will throw HRESULT_FROM_WIN32(ERROR_CLASS_ALREADY_EXISTS) on duplicate registration.
        // If needed, consider adding a Try* version of this function that returns the error code since that
        // may be ignorable.
        public static short RegisterClassEx(ref WNDCLASSEX lpwcx)
        {
            var ret = _RegisterClassEx(ref lpwcx);
            if (ret == 0)
            {
                HRESULT.ThrowLastError();
            }

            return ret;
        }

        // Depending on the message, callers may want to call GetLastError based on the return value.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WM Msg, IntPtr wParam, IntPtr lParam);

        // This is aliased as a macro in 32bit Windows.
        public static IntPtr SetWindowLongPtr(IntPtr hwnd, GWL nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, nIndex, dwNewLong) : new IntPtr(SetWindowLongPtr32(hwnd, nIndex, dwNewLong.ToInt32()));

        public static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags)
            => _SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);

        public static void SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw)
        {
            if (_SetWindowRgn(hWnd, hRgn, bRedraw) == 0)
            {
                throw new Win32Exception();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hwnd, SW nCmdShow);

        public static HIGHCONTRAST SystemParameterInfo_GetHIGHCONTRAST()
        {
            var hc = new HIGHCONTRAST { cbSize = Marshal.SizeOf(typeof(HIGHCONTRAST)) };
            if (!_SystemParametersInfo_HIGHCONTRAST(SPI.GETHIGHCONTRAST, hc.cbSize, ref hc, SPIF.None))
            {
                HRESULT.ThrowLastError();
            }

            return hc;
        }

        public static void UnregisterClass(string lpClassName, IntPtr hInstance)
        {
            if (!_UnregisterClassName(lpClassName, hInstance))
            {
                HRESULT.ThrowLastError();
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "CreateRectRgn", SetLastError = true)]
        private static extern IntPtr _CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn", SetLastError = true)]
        private static extern IntPtr _CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
        private static extern IntPtr _CreateWindowEx(
            WS_EX dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            WS dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetColorizationColor", PreserveSig = true)]
        private static extern HRESULT _DwmGetColorizationColor(out uint pcrColorization, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfOpaqueBlend);

        [DllImport("dwmapi.dll", EntryPoint = "DwmGetCompositionTimingInfo")]
        private static extern HRESULT _DwmGetCompositionTimingInfo(IntPtr hwnd, ref DWM_TIMING_INFO pTimingInfo);

        [DllImport("dwmapi.dll", EntryPoint = "DwmIsCompositionEnabled", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _DwmIsCompositionEnabled();
        [DllImport("user32.dll", EntryPoint = "EnableMenuItem")]
        private static extern int _EnableMenuItem(IntPtr hMenu, SC uIDEnableItem, MF uEnable);
        [DllImport("uxtheme.dll", EntryPoint = "GetCurrentThemeName", CharSet = CharSet.Unicode)]
        private static extern HRESULT _GetCurrentThemeName(
            StringBuilder pszThemeFileName,
            int dwMaxNameChars,
            StringBuilder pszColorBuff,
            int cchMaxColorChars,
            StringBuilder pszSizeBuff,
            int cchMaxSizeChars);
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr _GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
        [DllImport("gdi32.dll", EntryPoint = "GetStockObject", SetLastError = true)]
        private static extern IntPtr _GetStockObject(StockObject fnObject);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClassExW")]
        private static extern short _RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SWP uFlags);

        [DllImport("user32.dll", EntryPoint = "SetWindowRgn", SetLastError = true)]
        private static extern int _SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

        /// <summary>Overload of SystemParametersInfo for getting and setting HIGHCONTRAST.</summary>
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _SystemParametersInfo_HIGHCONTRAST(SPI uiAction, int uiParam, [In, Out] ref HIGHCONTRAST pvParam, SPIF fWinIni);

        [DllImport("user32.dll", EntryPoint = "UnregisterClass", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool _UnregisterClassName(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLongPtr32(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, GWL nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLongPtr32(IntPtr hWnd, GWL nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, GWL nIndex, IntPtr dwNewLong);
        // Win7 declarations

        // Win7 only.
        // #define DWM_SIT_DISPLAYFRAME    0x00000001  // Display a window frame around the provided bitmap
    }

    internal static class Win32Value
    {
        public const uint FALSE = 0;
        public const uint INFOTIPSIZE = 1024;
        public const uint MAX_PATH = 260;
        public const uint sizeof_BOOL = 4;
        public const uint sizeof_CHAR = 1;
        public const uint sizeof_WCHAR = 2;
        public const uint TRUE = 1;
    }
    [StructLayout(LayoutKind.Explicit)]
    internal class PROPVARIANT : IDisposable
    {
        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            internal static extern HRESULT PropVariantClear(PROPVARIANT pvar);
        }

        [FieldOffset(0)]
        private ushort vt;
        [FieldOffset(8)]
        private IntPtr pointerVal;
        [FieldOffset(8)]
        private byte byteVal;
        [FieldOffset(8)]
        private long longVal;
        [FieldOffset(8)]
        private short boolVal;

        public VarEnum VarType => (VarEnum)vt;

        // Right now only using this for strings.
        public string GetValue() => vt == (ushort)VarEnum.VT_LPWSTR ? Marshal.PtrToStringUni(pointerVal) : null;

        public void SetValue(bool f)
        {
            Clear();
            vt = (ushort)VarEnum.VT_BOOL;
            boolVal = (short)(f ? -1 : 0);
        }

        public void SetValue(string val)
        {
            Clear();
            vt = (ushort)VarEnum.VT_LPWSTR;
            pointerVal = Marshal.StringToCoTaskMemUni(val);
        }

        public void Clear()
        {
            var hr = NativeMethods.PropVariantClear(this);
            Assert.IsTrue(hr.Succeeded);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PROPVARIANT()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            Clear();
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal class RefRECT
    {
        private int _left;
        private int _top;
        private int _right;
        private int _bottom;

        public RefRECT(int left, int top, int right, int bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        public int Width => _right - _left;

        public int Height => _bottom - _top;

        public int Left { get => _left; set => _left = value; }

        public int Right { get => _right; set => _right = value; }

        public int Top { get => _top; set => _top = value; }

        public int Bottom { get => _bottom; set => _bottom = value; }

        public void Offset(int dx, int dy)
        {
            _left += dx;
            _top += dy;
            _right += dx;
            _bottom += dy;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [BestFitMapping(false)]
    internal class WIN32_FIND_DATAW
    {
        public FileAttributes dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }
}