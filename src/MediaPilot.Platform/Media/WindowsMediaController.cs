using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Platform.Input;

namespace MediaPilot.Platform.Media;

/// <summary>
/// Control over media playback.
/// </summary>
/// <remarks>
/// The playback commands are sent through the system keys, which is exactly
/// the mechanism of the original project: the system routes them to the
/// application that owns the media session.
/// </remarks>
public sealed class WindowsMediaController : IMediaController
{
    private readonly MediaSessionMonitor _sessionMonitor;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    /// <param name="sessionMonitor">Monitor of the media sessions of the system.</param>
    public WindowsMediaController(MediaSessionMonitor sessionMonitor)
    {
        ArgumentNullException.ThrowIfNull(sessionMonitor);
        _sessionMonitor = sessionMonitor;
    }

    /// <inheritdoc />
    public bool TryExecute(MediaCommand command)
    {
        var key = command switch
        {
            MediaCommand.Previous => VirtualKey.MediaPreviousTrack,
            MediaCommand.PlayPause => VirtualKey.MediaPlayPause,
            MediaCommand.Next => VirtualKey.MediaNextTrack,
            _ => VirtualKey.None,
        };

        return key != VirtualKey.None && KeyInputService.Send(key);
    }

    /// <inheritdoc />
    public MediaTrack? GetCurrentTrack() => _sessionMonitor.CurrentTrack;
}
