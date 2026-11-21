using System.Runtime.InteropServices;

namespace MediaPilot.Platform.Interop;

/// <summary>
/// P/Invoke declarations of the Win32 APIs used by the application.
/// </summary>
/// <remarks>
/// <see cref="LibraryImportAttribute"/> is used instead of
/// <see cref="DllImportAttribute"/> so that the compiler generates the
/// marshalling: there is no reflection cost and no <c>System.Reflection.Emit</c>.
/// </remarks>
internal static partial class NativeMethods
{
    // ─────────────────────────── user32 ───────────────────────────

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(nint hWnd, int id);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint LoadImageW(nint hinst, string lpszName, uint fuType, int cx, int cy, uint fuLoad);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint hIcon);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

    // ─────────────────────────── shell32 ───────────────────────────

    // Shell_NotifyIconW receives a structure with fixed-size string fields,
    // which is not blittable and therefore cannot use the P/Invoke generator
    // (SYSLIB1051). It is declared with DllImport; the call is infrequent
    // (icon creation, state changes, notifications) and does not affect performance.
    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);
}

/// <summary>Types and constants for <c>user32</c> interop.</summary>
internal static class User32
{    /// <summary><c>WM_HOTKEY</c> message.</summary>
    internal const int WmHotkey = 0x0312;

    /// <summary>Alt modifier.</summary>
    internal const uint ModAlt = 0x0001;

    /// <summary>Control modifier.</summary>
    internal const uint ModControl = 0x0002;

    /// <summary>Shift modifier.</summary>
    internal const uint ModShift = 0x0004;

    /// <summary>Windows modifier.</summary>
    internal const uint ModWin = 0x0008;

    /// <summary><c>IMAGE_ICON</c> image type.</summary>
    internal const uint ImageIcon = 1;

    /// <summary>Load the image from a file.</summary>
    internal const uint LoadFromFile = 0x0010;

    /// <summary>Use the default system size (16x16 and 32x32).</summary>
    internal const uint DefaultSize = 0x0040;

    /// <summary>Key pressed.</summary>
    internal const uint KeyEventKeyDown = 0x0000;

    /// <summary>Key released.</summary>
    internal const uint KeyEventKeyUp = 0x0002;

    /// <summary>Extended key flag (arrows, media keys).</summary>
    internal const uint KeyEventExtended = 0x0001;

    /// <summary>Hides the window without minimizing it.</summary>
    internal const int SwHide = 0;
}

/// <summary>Types and constants for <c>shell32</c> interop.</summary>
internal static class Shell32
{
    /// <summary>Adds the icon to the notification area.</summary>
    internal const uint NimAdd = 0x00000000;

    /// <summary>Modifies the already registered icon.</summary>
    internal const uint NimModify = 0x00000001;

    /// <summary>Removes the icon from the notification area.</summary>
    internal const uint NimDelete = 0x00000002;

    /// <summary>Sets the version of the icon.</summary>
    internal const uint NimSetVersion = 0x00000004;

    /// <summary>The icon notifies mouse messages.</summary>
    internal const uint NifMessage = 0x00000001;

    /// <summary>The icon has an associated image resource.</summary>
    internal const uint NifIcon = 0x00000002;

    /// <summary>The icon has a tooltip.</summary>
    internal const uint NifTip = 0x00000004;

    /// <summary>The icon shows an informational balloon.</summary>
    internal const uint NifInfo = 0x00000010;

    /// <summary>Version 4 of the icon: the event and the identifier are packed into <c>lParam</c>.</summary>
    internal const uint NotifyIconVersion4 = 4;
}
