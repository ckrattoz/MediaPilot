using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Platform.Input;

namespace MediaPilot.Platform.Media;

/// <summary>
/// Control over media playback through the system keys.
/// </summary>
/// <remarks>
/// It sends the VK_MEDIA_* keys to the system, which is exactly the mechanism
/// of the original project. Reading the track being played requires the media
/// sessions API (SMTC) and is added in a later stage.
/// </remarks>
public sealed class WindowsMediaController : IMediaController
{
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
    /// <remarks>
    /// Always returns <see langword="null"/>: getting the real track title
    /// requires <c>Windows.Media.Control</c>, which is enabled in the media
    /// sessions integration stage.
    /// </remarks>
    public string? GetCurrentTrackTitle() => null;
}
