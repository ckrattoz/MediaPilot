using System.Text.Json.Serialization;

namespace MediaPilot.Core;

/// <summary>
/// User interface theme preference.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ThemePreference>))]
public enum ThemePreference
{
    /// <summary>Follow the operating system theme preference.</summary>
    Auto,

    /// <summary>Force the light theme.</summary>
    Light,

    /// <summary>Force the dark theme.</summary>
    Dark,
}
