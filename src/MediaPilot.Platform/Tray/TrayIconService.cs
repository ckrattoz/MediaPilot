using System.Runtime.InteropServices;
using MediaPilot.Core.Contracts;
using MediaPilot.Localization.Resources;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Tray;

/// <summary>
/// The application icon in the Windows notification area.
/// </summary>
/// <remarks>
/// Implemented directly on top of <c>Shell_NotifyIconW</c> and the Win32
/// context menus instead of <c>System.Windows.Forms.NotifyIcon</c>: that way
/// the interoperability layer does not depend on Windows Forms and the type
/// clash with WPF is avoided.
/// </remarks>
public sealed class TrayIconService : ITrayIcon, IDisposable
{
    private const uint TrayCallbackMessage = 0x8000 + 1;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonUp = 0x0205;
    private const int WmContextMenu = 0x007B;
    private const int IconId = 1;

    private readonly nint _windowHandle;
    private readonly string _activeIconPath;
    private readonly string _inactiveIconPath;
    private readonly HashSet<nint> _loadedIcons = [];

    private TrayIconState _state = TrayIconState.Active;
    private nint _currentIcon;
    private string _tooltip = string.Empty;
    private bool _added;
    private bool _startupEnabled;

    /// <summary>
    /// Initializes the service and registers the icon in the notification area.
    /// </summary>
    /// <param name="windowHandle">Window that receives the icon events.</param>
    /// <param name="router">Native message router.</param>
    /// <param name="activeIconPath">Path of the icon for the active state.</param>
    /// <param name="inactiveIconPath">Path of the icon for the inactive state.</param>
    public TrayIconService(
        nint windowHandle,
        NativeMessageRouter router,
        string activeIconPath,
        string inactiveIconPath)
    {
        ArgumentNullException.ThrowIfNull(router);

        _windowHandle = windowHandle;
        _activeIconPath = activeIconPath;
        _inactiveIconPath = inactiveIconPath;

        router.Register(HandleMessage);

        Add();
    }

    /// <inheritdoc />
    public event EventHandler? OpenRequested;

    /// <inheritdoc />
    public event EventHandler? AboutRequested;

    /// <inheritdoc />
    public event EventHandler? HotkeysToggled;

    /// <inheritdoc />
    public event EventHandler? StartupToggled;

    /// <inheritdoc />
    public event EventHandler? ExitRequested;

    /// <inheritdoc />
    public void SetState(TrayIconState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        ApplyIcon();
    }

    /// <inheritdoc />
    public void SetStartupEnabled(bool enabled)
    {
        if (_startupEnabled == enabled)
        {
            return;
        }

        _startupEnabled = enabled;
    }

    /// <inheritdoc />
    public void SetTooltip(string tooltip)
    {
        ArgumentNullException.ThrowIfNull(tooltip);

        if (string.Equals(_tooltip, tooltip, StringComparison.Ordinal))
        {
            return;
        }

        _tooltip = tooltip;

        if (!_added)
        {
            return;
        }

        var data = CreateData();
        data.uFlags = Shell32.NifTip;
        NativeMethods.Shell_NotifyIconW(Shell32.NimModify, ref data);
    }

    /// <inheritdoc />
    public void ShowNotification(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);

        if (!_added)
        {
            return;
        }

        var data = CreateData();
        data.uFlags = Shell32.NifInfo;
        data.szInfoTitle = title;
        data.szInfo = message;
        NativeMethods.Shell_NotifyIconW(Shell32.NimModify, ref data);
    }

    /// <summary>
    /// Removes the icon from the notification area.
    /// </summary>
    public void Remove()
    {
        if (!_added)
        {
            return;
        }

        var data = CreateData();
        NativeMethods.Shell_NotifyIconW(Shell32.NimDelete, ref data);
        _added = false;
    }

    private void Add()
    {
        var data = CreateData();
        data.uFlags = Shell32.NifMessage | Shell32.NifIcon | Shell32.NifTip;

        _currentIcon = LoadIcon(_activeIconPath);
        data.hIcon = _currentIcon;

        if (!NativeMethods.Shell_NotifyIconW(Shell32.NimAdd, ref data))
        {
            return;
        }

        _added = true;

        var version = CreateData();
        version.uTimeoutOrVersion = (int)Shell32.NotifyIconVersion4;
        NativeMethods.Shell_NotifyIconW(Shell32.NimSetVersion, ref version);
    }

    private void ApplyIcon()
    {
        if (!_added)
        {
            return;
        }

        var icon = LoadIcon(_state == TrayIconState.Active ? _activeIconPath : _inactiveIconPath);

        var data = CreateData();
        data.uFlags = Shell32.NifIcon;
        data.hIcon = icon;
        NativeMethods.Shell_NotifyIconW(Shell32.NimModify, ref data);

        if (_currentIcon != 0)
        {
            NativeMethods.DestroyIcon(_currentIcon);
            _loadedIcons.Remove(_currentIcon);
        }

        _currentIcon = icon;
    }

    private NOTIFYICONDATAW CreateData() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = _windowHandle,
        uID = IconId,
        uCallbackMessage = TrayCallbackMessage,
        szTip = _tooltip,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private nint LoadIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return 0;
        }

        var icon = NativeMethods.LoadImageW(
            0,
            path,
            User32.ImageIcon,
            0,
            0,
            User32.LoadFromFile | User32.DefaultSize);

        if (icon != 0)
        {
            _loadedIcons.Add(icon);
        }

        return icon;
    }

    private bool HandleMessage(NativeMessage message)
    {
        if (message.Message != (int)TrayCallbackMessage)
        {
            return false;
        }

        // With NOTIFYICON_VERSION_4 the event travels in the low 16 bits of lParam.
        var evt = (int)((long)message.LParam & 0xFFFF);

        switch (evt)
        {
            case WmLButtonDblClk:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case WmRButtonUp:
            case WmContextMenu:
                ShowContextMenu();
                return true;

            default:
                return false;
        }
    }

    private void ShowContextMenu()
    {
        var menu = MenuNativeMethods.CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendItem(menu, MenuConstants.CommandOpen, Strings.Tray_Menu_OpenSettings);
            AppendItem(menu, MenuConstants.CommandAbout, Strings.Tray_Menu_About);
            AppendItem(menu, 0, null);
            AppendItem(menu, MenuConstants.CommandToggleHotkeys, Strings.Tray_Menu_ToggleHotkeys, isChecked: _state == TrayIconState.Active);
            AppendItem(menu, MenuConstants.CommandToggleStartup, Strings.Tray_Menu_ToggleStartup, isChecked: _startupEnabled);
            AppendItem(menu, 0, null);
            AppendItem(menu, MenuConstants.CommandExit, Strings.Tray_Menu_Exit);

            // The menu needs the focus so that it closes when Escape is pressed.
            MenuNativeMethods.SetForegroundWindow(_windowHandle);

            var position = GetCursorPosition();
            var command = MenuNativeMethods.TrackPopupMenuEx(
                menu,
                MenuConstants.TpmReturnCmd | MenuConstants.TpmLeftButton,
                position.X,
                position.Y,
                _windowHandle,
                0);

            MenuNativeMethods.SetForegroundWindow(_windowHandle);

            RaiseCommand(command);
        }
        finally
        {
            MenuNativeMethods.DeleteMenu(menu);
        }
    }

    private static void AppendItem(nint menu, uint command, string? text, bool isChecked = false)
    {
        if (command == 0 && text is null)
        {
            MenuNativeMethods.AppendMenuW(menu, MenuConstants.MfSeparator, 0, 0);
            return;
        }

        var flags = MenuConstants.MfString;
        if (isChecked)
        {
            flags |= MenuConstants.MfChecked;
        }

        var buffer = Marshal.StringToHGlobalUni(text ?? string.Empty);
        try
        {
            MenuNativeMethods.AppendMenuW(menu, flags, command, buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void RaiseCommand(uint command)
    {
        switch (command)
        {
            case MenuConstants.CommandOpen:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                break;

            case MenuConstants.CommandAbout:
                AboutRequested?.Invoke(this, EventArgs.Empty);
                break;

            case MenuConstants.CommandToggleHotkeys:
                HotkeysToggled?.Invoke(this, EventArgs.Empty);
                break;

            case MenuConstants.CommandToggleStartup:
                StartupToggled?.Invoke(this, EventArgs.Empty);
                break;

            case MenuConstants.CommandExit:
                ExitRequested?.Invoke(this, EventArgs.Empty);
                break;

            default:
                break;
        }
    }

    private static POINT GetCursorPosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return point;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Remove();

        foreach (var icon in _loadedIcons)
        {
            NativeMethods.DestroyIcon(icon);
        }

        _loadedIcons.Clear();
        GC.SuppressFinalize(this);
    }
}
