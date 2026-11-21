using System.Text.Json.Serialization;

namespace MediaPilot.Core;

/// <summary>
/// Media actions the application can execute.
/// </summary>
/// <remarks>
/// The set is deliberately closed: it is the functional contract inherited
/// from the original <c>MultimediaHotkeys</c> project (AutoHotkey v2).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<MediaCommand>))]
public enum MediaCommand
{
    /// <summary>Plays the previous track.</summary>
    Previous,

    /// <summary>Toggles between playback and pause.</summary>
    PlayPause,

    /// <summary>Plays the next track.</summary>
    Next,

    /// <summary>Decreases the system volume.</summary>
    VolumeDown,

    /// <summary>Toggles the system mute state.</summary>
    VolumeMute,

    /// <summary>Increases the system volume.</summary>
    VolumeUp,

    /// <summary>Shows information about the track being played.</summary>
    ShowInformation,
}
