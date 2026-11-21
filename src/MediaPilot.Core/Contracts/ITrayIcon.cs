namespace MediaPilot.Core.Contracts;

/// <summary>
/// Visual state of the icon in the notification area.
/// </summary>
public enum TrayIconState
{
    /// <summary>Hotkeys are active.</summary>
    Active,

    /// <summary>Hotkeys are suspended.</summary>
    Inactive,
}

/// <summary>
/// Application icon in the Windows notification area.
/// </summary>
public interface ITrayIcon
{
    /// <summary>Raised when the user chooses "Open settings".</summary>
    event EventHandler? OpenRequested;

    /// <summary>Raised when the user chooses "About".</summary>
    event EventHandler? AboutRequested;

    /// <summary>Raised when the user chooses to enable or disable the hotkeys.</summary>
    event EventHandler? HotkeysToggled;

    /// <summary>Raised when the user chooses to enable or disable automatic startup.</summary>
    event EventHandler? StartupToggled;

    /// <summary>Raised when the user chooses "Exit".</summary>
    event EventHandler? ExitRequested;

    /// <summary>
    /// Changes the displayed icon according to the hotkey state.
    /// </summary>
    /// <param name="state">State to display.</param>
    void SetState(TrayIconState state);

    /// <summary>
    /// Updates the check mark of the "Start with Windows" item.
    /// </summary>
    /// <param name="enabled">Indicates whether automatic startup is enabled.</param>
    void SetStartupEnabled(bool enabled);

    /// <summary>
    /// Updates the icon tooltip.
    /// </summary>
    /// <param name="tooltip">Text to display.</param>
    void SetTooltip(string tooltip);

    /// <summary>
    /// Shows a notification balloon.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body.</param>
    void ShowNotification(string title, string message);
}
