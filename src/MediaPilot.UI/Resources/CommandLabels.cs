using MediaPilot.Core;
using MediaPilot.Localization.Resources;

namespace MediaPilot.UI.Resources;

/// <summary>
/// Display names for the multimedia commands.
/// </summary>
/// <remarks>
/// The mapping lives here so the settings panel and the about window show the
/// same wording for the same command, and so a command label is translated in
/// exactly one place.
/// </remarks>
public static class CommandLabels
{
    /// <summary>
    /// Gets the display name of a command.
    /// </summary>
    /// <param name="command">The command to describe.</param>
    /// <returns>The localized display name.</returns>
    public static string Describe(MediaCommand command) => command switch
    {
        MediaCommand.Previous => Strings.Command_Previous,
        MediaCommand.PlayPause => Strings.Command_PlayPause,
        MediaCommand.Next => Strings.Command_Next,
        MediaCommand.VolumeDown => Strings.Command_VolumeDown,
        MediaCommand.VolumeMute => Strings.Command_VolumeMute,
        MediaCommand.VolumeUp => Strings.Command_VolumeUp,
        MediaCommand.ShowInformation => Strings.Command_ShowInformation,
        _ => command.ToString(),
    };
}
