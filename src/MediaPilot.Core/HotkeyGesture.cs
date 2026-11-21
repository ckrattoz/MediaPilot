using System.Text;
using System.Text.Json.Serialization;
using MediaPilot.Localization.Resources;

namespace MediaPilot.Core;

/// <summary>
/// A combination of modifier keys and a main key.
/// </summary>
/// <remarks>
/// It is a value type: two gestures with the same modifiers and key are equal,
/// which allows using it directly as a dictionary key. It is declared with
/// positional parameters so its properties get <c>init</c> accessors; without
/// them, <c>System.Text.Json</c> could not populate a value type and the
/// settings file would lose the gesture.
/// </remarks>
public readonly record struct HotkeyGesture(ModifierKeys Modifiers, VirtualKey Key)
{
    /// <summary>
    /// Gets a value indicating whether the gesture is usable.
    /// </summary>
    /// <remarks>
    /// A gesture requires a main key and at least one modifier: a shortcut
    /// without modifiers would hijack that key across the whole system.
    /// </remarks>
    [JsonIgnore]
    public bool IsValid => Key != VirtualKey.None && Modifiers != ModifierKeys.None;

    /// <summary>
    /// Returns the textual representation of the gesture, for example
    /// <c>Ctrl+Alt+Shift+Left</c>.
    /// </summary>
    public override string ToString()
    {
        if (Key == VirtualKey.None)
        {
            return Strings.Hotkey_Unassigned;
        }

        var builder = new StringBuilder();

        AppendModifier(builder, ModifierKeys.Control, "Ctrl");
        AppendModifier(builder, ModifierKeys.Alt, "Alt");
        AppendModifier(builder, ModifierKeys.Shift, "Shift");
        AppendModifier(builder, ModifierKeys.Win, "Win");

        builder.Append(DescribeKey(Key));

        return builder.ToString();
    }

    /// <summary>
    /// Gets the display name of a key.
    /// </summary>
    /// <param name="key">The key to describe.</param>
    /// <returns>The display name of the key.</returns>
    public static string DescribeKey(VirtualKey key) => key switch
    {
        VirtualKey.None => string.Empty,
        VirtualKey.Space => Strings.Key_Space,
        VirtualKey.Left => Strings.Key_Left,
        VirtualKey.Up => Strings.Key_Up,
        VirtualKey.Right => Strings.Key_Right,
        VirtualKey.Down => Strings.Key_Down,
        VirtualKey.OemPeriod => Strings.Key_OemPeriod,
        VirtualKey.MediaPreviousTrack => Strings.Key_MediaPreviousTrack,
        VirtualKey.MediaPlayPause => Strings.Key_MediaPlayPause,
        VirtualKey.MediaNextTrack => Strings.Key_MediaNextTrack,
        VirtualKey.VolumeMute => Strings.Key_VolumeMute,
        VirtualKey.VolumeDown => Strings.Key_VolumeDown,
        VirtualKey.VolumeUp => Strings.Key_VolumeUp,
        _ => key.ToString(),
    };

    private void AppendModifier(StringBuilder builder, ModifierKeys modifier, string name)
    {
        if ((Modifiers & modifier) == modifier)
        {
            builder.Append(name).Append('+');
        }
    }
}
