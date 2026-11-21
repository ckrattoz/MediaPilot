using MediaPilot.Core;

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
    /// Gets the track that is being played, if any.
    /// </summary>
    /// <returns>The track, or <see langword="null"/> when there is no information.</returns>
    MediaTrack? GetCurrentTrack();
}
