using System.Text.Json.Serialization;

namespace MediaPilot.Core;

/// <summary>
/// Modifier keys that can be part of a keyboard gesture.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<ModifierKeys>))]
public enum ModifierKeys
{
    /// <summary>No modifiers.</summary>
    None = 0,

    /// <summary>Alt key.</summary>
    Alt = 1,

    /// <summary>Control key.</summary>
    Control = 2,

    /// <summary>Shift key.</summary>
    Shift = 4,

    /// <summary>Windows key.</summary>
    Win = 8,
}
