namespace MediaPilot.Core;

/// <summary>
/// Persistent application settings.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Settings schema version.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema version this instance was serialized with.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Indicates whether global hotkeys are enabled.</summary>
    public bool HotkeysEnabled { get; init; } = true;

    /// <summary>Indicates whether the application should start with Windows.</summary>
    public bool StartWithWindows { get; init; }

    /// <summary>Indicates whether volume notifications are displayed.</summary>
    public bool ShowNotifications { get; init; } = true;

    /// <summary>
    /// Delay, in milliseconds, before showing the volume notification.
    /// </summary>
    /// <remarks>
    /// 350 ms is the value inherited from the original project and it prevents
    /// repeated notifications from being sent while a key is held down.
    /// </remarks>
    public int NotificationDebounceMs { get; init; } = 350;

    /// <summary>User interface theme preference.</summary>
    public ThemePreference Theme { get; init; } = ThemePreference.Auto;

    /// <summary>Global hotkey and command bindings.</summary>
    public HotkeyBinding[] Hotkeys { get; init; } = DefaultHotkeys.All;
}
