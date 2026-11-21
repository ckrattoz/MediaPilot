namespace MediaPilot.Core.Contracts;

/// <summary>
/// Control over system media playback.
/// </summary>
public interface IMediaController
{
    /// <summary>
    /// Executes a playback command on the active media session.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <returns><see langword="true"/> if the command executed successfully.</returns>
    bool TryExecute(MediaCommand command);

    /// <summary>
    /// Gets the title of the track being played, if any.
    /// </summary>
    /// <returns>The track title, or <see langword="null"/> if there is no information.</returns>
    string? GetCurrentTrackTitle();
}
