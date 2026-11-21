using MediaPilot.Core;
using MediaPilot.Core.Contracts;
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
    private readonly Timer _debounceTimer;

    private int _pendingVolume;
    private bool _pendingMuted;
    private bool _pendingIncreased;
    private bool _hasPendingVolume;
    private int _debounceTimerDelay = 350;

    /// <summary>
    /// Initializes the executor.
    /// </summary>
    /// <param name="mediaController">Media playback control.</param>
    /// <param name="volumeController">Volume control.</param>
    /// <param name="notifications">Notification presenter.</param>
    /// <param name="notificationDebounceMs">Delay before showing the volume notification.</param>
    public MediaCommandExecutor(
        IMediaController mediaController,
        IVolumeController volumeController,
        INotificationPresenter notifications,
        int notificationDebounceMs = 350)
    {
        ArgumentNullException.ThrowIfNull(mediaController);
        ArgumentNullException.ThrowIfNull(volumeController);
        ArgumentNullException.ThrowIfNull(notifications);

        _mediaController = mediaController;
        _volumeController = volumeController;
        _notifications = notifications;
        _debounceTimerDelay = notificationDebounceMs;

        _debounceTimer = new Timer(
            _ => FlushVolumeNotification(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

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

    /// <summary>
    /// Defines the delay before showing the volume notification.
    /// </summary>
    /// <param name="milliseconds">Delay in milliseconds.</param>
    public void SetDebounceDelay(int milliseconds) => _debounceTimerDelay = milliseconds;

    /// <inheritdoc />
    public void Dispose()
    {
        _debounceTimer.Dispose();
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

        _pendingIncreased = increased;
        _hasPendingVolume = true;

        // The notification is delayed so that holding the key down does not
        // produce one notification per repetition.
        _debounceTimer.Change(_debounceTimerDelay, Timeout.Infinite);
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

        _pendingMuted = !muted;
        _hasPendingVolume = true;
        _pendingIncreased = false;

        _debounceTimer.Change(_debounceTimerDelay, Timeout.Infinite);
    }

    private void ShowInformation()
    {
        var track = _mediaController.GetCurrentTrackTitle();

        _notifications.ShowMessage(
            Strings.App_Name,
            string.IsNullOrWhiteSpace(track) ? Strings.Executor_NoTrackPlaying : track);
    }

    private void FlushVolumeNotification()
    {
        if (!_hasPendingVolume)
        {
            return;
        }

        _hasPendingVolume = false;

        var volume = _pendingMuted ? 0 : _pendingVolume;
        if (!_pendingMuted && _volumeController.TryGetVolume(out var current))
        {
            volume = current;
        }

        _pendingVolume = volume;

        var track = _mediaController.GetCurrentTrackTitle();
        _notifications.ShowVolume(volume, _pendingMuted, _pendingIncreased, track);
    }
}
