using System.Runtime.InteropServices;

namespace MediaPilot.Platform.Interop;

/// <summary>
/// <c>NOTIFYICONDATAW</c> structure of <c>shell32</c>.
/// </summary>
/// <remarks>
/// Exact replica of the native structure, including the alignment, so that
/// <c>Shell_NotifyIconW</c> interprets the fields correctly.
/// </remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NOTIFYICONDATAW
{
    /// <summary>Size of the structure in bytes.</summary>
    public int cbSize;

    /// <summary>Window that receives the icon messages.</summary>
    public nint hWnd;

    /// <summary>Icon identifier.</summary>
    public int uID;

    /// <summary>Flags of the valid fields.</summary>
    public uint uFlags;

    /// <summary>Message sent to the window on mouse events.</summary>
    public uint uCallbackMessage;

    /// <summary>Image resource of the icon.</summary>
    public nint hIcon;

    /// <summary>Tooltip of the icon.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szTip;

    /// <summary>State of the icon.</summary>
    public int dwState;

    /// <summary>Mask of the icon state.</summary>
    public int dwStateMask;

    /// <summary>Body of the informational balloon.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string szInfo;

    /// <summary>Icon version or balloon display time.</summary>
    public int uTimeoutOrVersion;

    /// <summary>Title of the informational balloon.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string szInfoTitle;

    /// <summary>Flags of the informational balloon.</summary>
    public int dwInfoFlags;

    /// <summary>Unique icon identifier.</summary>
    public Guid guidItem;

    /// <summary>Icon shown in the informational balloon.</summary>
    public nint hBalloonIcon;
}

/// <summary><c>POINT</c> structure of <c>user32</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    /// <summary>Horizontal coordinate in pixels.</summary>
    public int X;

    /// <summary>Vertical coordinate in pixels.</summary>
    public int Y;
}

/// <summary><c>INPUT</c> structure of <c>user32</c> for <c>SendInput</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    /// <summary>Input type: 1 for keyboard.</summary>
    public uint type;

    /// <summary>Union with the input data.</summary>
    public INPUTUNION U;
}

/// <summary>Union of keyboard input data.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
    /// <summary>Keyboard input data.</summary>
    [FieldOffset(0)]
    public KEYBDINPUT ki;
}

/// <summary><c>KEYBDINPUT</c> structure of <c>user32</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    /// <summary>Virtual key code.</summary>
    public ushort wVk;

    /// <summary>Scan code.</summary>
    public ushort wScan;

    /// <summary>Event flags.</summary>
    public uint dwFlags;

    /// <summary>Event timestamp.</summary>
    public uint time;

    /// <summary>Additional information associated with the event.</summary>
    public nint dwExtraInfo;
}
