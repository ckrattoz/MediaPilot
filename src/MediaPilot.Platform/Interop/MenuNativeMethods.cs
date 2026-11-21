using System.Runtime.InteropServices;

namespace MediaPilot.Platform.Interop;

/// <summary>P/Invoke declarations of the Win32 context menus.</summary>
internal static partial class MenuNativeMethods
{
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AppendMenuW(nint hMenu, uint flags, nuint idNewItem, nint newItem);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteMenu(nint hMenu);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint TrackPopupMenuEx(
        nint hmenu,
        uint flags,
        int x,
        int y,
        nint hwnd,
        nint lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);
}

/// <summary>Constants of the Win32 context menus.</summary>
internal static class MenuConstants
{
    /// <summary>The item is a text string.</summary>
    internal const uint MfString = 0x0000;

    /// <summary>The item is checked.</summary>
    internal const uint MfChecked = 0x0008;

    /// <summary>The item is a separator.</summary>
    internal const uint MfSeparator = 0x0800;

    /// <summary>The command is returned directly in the return value.</summary>
    internal const uint TpmReturnCmd = 0x0100;

    /// <summary>The menu is displayed left-aligned relative to the cursor.</summary>
    internal const uint TpmLeftButton = 0x0001;

    /// <summary>Identifier of the first command of the tray menu.</summary>
    internal const uint CommandBase = 100;

    /// <summary>"Open settings" command.</summary>
    internal const uint CommandOpen = CommandBase + 1;

    /// <summary>"Enable/disable hotkeys" command.</summary>
    internal const uint CommandToggleHotkeys = CommandBase + 2;

    /// <summary>"Start with Windows" command.</summary>
    internal const uint CommandToggleStartup = CommandBase + 3;

    /// <summary>"About" command.</summary>
    internal const uint CommandAbout = CommandBase + 4;

    /// <summary>"Exit" command.</summary>
    internal const uint CommandExit = CommandBase + 5;
}
