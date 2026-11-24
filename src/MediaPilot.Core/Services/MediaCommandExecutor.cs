using System.Globalization;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Localization;
using MediaPilot.Localization.Resources;

namespace MediaPilot.Core.Services;

/// <summary>
/// Turns multimedia commands into system actions and user notifications.
/// </summary>
/// <remarks>
/// It concentrates the orchestration logic (which command produces which
/// effect, and how long to wait before notifying) so that the user interface
/// layer and the platform layer do not have to coordinate with each other.
/// </remarks>
public sealed class MediaCommandExecutor : IDisposable
{
    private readonly IMediaController _mediaController;
    private readonly IVolumeController _volumeController;
    private readonly INotificationPresenter _notifications;

    /// <summary>
    /// Initializes the executor.
    /// </summary>
    /// <param name="mediaController">Media playback control.</param>
    /// <param name="volumeController">Volume control.</param>
    /// <param name="notifications">Notification presenter.</param>
    public MediaCommandExecutor(
        IMediaController mediaController,
        IVolumeController volumeController,
        INotificationPresenter notifications)
    {
        ArgumentNullException.ThrowIfNull(mediaController);
        ArgumentNullException.ThrowIfNull(volumeController);
        ArgumentNullException.ThrowIfNull(notifications);

        _mediaController = mediaController;
        _volumeController = volumeController;
        _notifications = notifications;
    }

    /// <summary>
    /// Indicates whether the volume notifications are displayed.
    /// </summary>
    /// <remarks>
    /// It follows the setting of the same name, which the user can change
    /// from the settings window while the application is running.
    /// </remarks>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Executes a command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    public void Execute(MediaCommand command)
    {
        switch (command)
        {
            case MediaCommand.Previous:
            case MediaCommand.PlayPause:
            case MediaCommand.Next:
                _mediaController.TryExecute(command);
                break;

            case MediaCommand.VolumeDown:
                AdjustVolume(increased: false);
                break;

            case MediaCommand.VolumeUp:
                AdjustVolume(increased: true);
                break;

            case MediaCommand.VolumeMute:
                ToggleMute();
                break;

            case MediaCommand.ShowInformation:
                ShowInformation();
                break;

            default:
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void AdjustVolume(bool increased)
    {
        var applied = increased
            ? _volumeController.TryIncreaseVolume()
            : _volumeController.TryDecreaseVolume();

        if (!applied)
        {
            return;
        }

        ShowVolumeNotification(increased: increased, muted: false);
    }

    private void ToggleMute()
    {
        if (!_volumeController.TryGetMuted(out var muted))
        {
            return;
        }

        if (!_volumeController.TrySetMuted(!muted))
        {
            return;
        }

        ShowVolumeNotification(increased: false, muted: !muted);
    }

    /// <summary>
    /// Shows the volume that the system has right now.
    /// </summary>
    /// <remarks>
    /// The notification is shown right away and the presenter updates the one
    /// that is already on screen, so the user sees the volume going up while
    /// the key is held down. The delay of the original project was needed
    /// because every notification was a new balloon and they piled up.
    /// </remarks>
    /// <param name="increased">Whether the volume was raised.</param>
    /// <param name="muted">Whether the device is silent now.</param>
    private void ShowVolumeNotification(bool increased, bool muted)
    {
        if (!ShowNotifications)
        {
            return;
        }

        var volume = 0;

        if (!muted && _volumeController.TryGetVolume(out var current))
        {
            volume = current;
        }

        var track = _mediaController.GetCurrentTrack();

        _notifications.ShowVolume(
            volume,
            muted,
            increased,
            track is null || track.IsEmpty ? null : DescribeTrack(track));
    }

    private void ShowInformation()
    {
        var track = _mediaController.GetCurrentTrack();

        _notifications.ShowMessage(
            Strings.App_Name,
            track is null || track.IsEmpty
                ? Strings.Executor_NoTrackPlaying
                : DescribeTrack(track));
    }

    /// <summary>
    /// Builds the text that identifies a track: the title and, when the media
    /// session publishes it, the artist.
    /// </summary>
    /// <param name="track">The track to describe.</param>
    /// <returns>The text of the track.</returns>
    private static string DescribeTrack(MediaTrack track)
    {
        if (string.IsNullOrWhiteSpace(track.Artist))
        {
            return track.Title;
        }

        if (string.IsNullOrWhiteSpace(track.Title))
        {
            return track.Artist;
        }

        return LocalizedFormat.Format(
            CultureInfo.CurrentCulture,
            Strings.Track_TitleArtistFormat,
            track.Title,
            track.Artist);
    }
}
