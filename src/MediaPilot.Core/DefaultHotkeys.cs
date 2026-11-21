namespace MediaPilot.Core;

/// <summary>
/// Default hotkey bindings.
/// </summary>
/// <remarks>
/// They reproduce exactly the functional contract of the original
/// <c>MultimediaHotkeys</c> project, so the first version of the native
/// application is a drop-in replacement with no surprises for the user.
/// </remarks>
public static class DefaultHotkeys
{
    private const ModifierKeys Common = ModifierKeys.Control | ModifierKeys.Alt;

    /// <summary>Gets a copy of the default bindings.</summary>
    public static HotkeyBinding[] All =>
    [
        new HotkeyBinding(new HotkeyGesture(Common, VirtualKey.Left), MediaCommand.Previous),
        new HotkeyBinding(new HotkeyGesture(Common, VirtualKey.Down), MediaCommand.PlayPause),
        new HotkeyBinding(new HotkeyGesture(Common, VirtualKey.Right), MediaCommand.Next),
        new HotkeyBinding(new HotkeyGesture(Common | ModifierKeys.Shift, VirtualKey.Left), MediaCommand.VolumeDown),
        new HotkeyBinding(new HotkeyGesture(Common | ModifierKeys.Shift, VirtualKey.Down), MediaCommand.VolumeMute),
        new HotkeyBinding(new HotkeyGesture(Common | ModifierKeys.Shift, VirtualKey.Right), MediaCommand.VolumeUp),
        new HotkeyBinding(new HotkeyGesture(Common | ModifierKeys.Shift, VirtualKey.OemPeriod), MediaCommand.ShowInformation),
    ];
}
