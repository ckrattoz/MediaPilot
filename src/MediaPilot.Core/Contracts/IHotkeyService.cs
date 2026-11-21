namespace MediaPilot.Core.Contracts;

/// <summary>
/// Registration and dispatch of system-wide hotkeys.
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Raised when a registered hotkey is triggered.
    /// </summary>
    event EventHandler<MediaCommand>? CommandTriggered;

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="gesture">Gesture to register.</param>
    /// <param name="command">Command to execute when it is triggered.</param>
    /// <returns><see langword="true"/> if the registration was accepted by the system.</returns>
    bool Register(HotkeyGesture gesture, MediaCommand command);

    /// <summary>
    /// Removes the registration of a global hotkey.
    /// </summary>
    /// <param name="gesture">Gesture to remove.</param>
    /// <returns><see langword="true"/> if there was a registration and it was removed.</returns>
    bool Unregister(HotkeyGesture gesture);

    /// <summary>Removes all registered hotkeys.</summary>
    void UnregisterAll();

    /// <summary>
    /// Indicates whether a gesture is currently registered.
    /// </summary>
    /// <param name="gesture">Gesture to query.</param>
    /// <returns><see langword="true"/> if the gesture is registered.</returns>
    bool IsRegistered(HotkeyGesture gesture);
}
