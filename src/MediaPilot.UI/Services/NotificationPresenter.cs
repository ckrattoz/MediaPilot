using System.Globalization;
using System.Windows;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Localization;
using MediaPilot.Localization.Resources;
using MediaPilot.UI.Views;

namespace MediaPilot.UI.Services;

/// <summary>
/// Presents notifications through the floating window of the user interface.
/// </summary>
public sealed class NotificationPresenter : INotificationPresenter
{
    private static readonly TimeSpan VolumeDisplayDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MessageDisplayDuration = TimeSpan.FromSeconds(3);

    private NotificationOverlay? _overlay;

    /// <inheritdoc />
    public void ShowVolume(int volume, bool muted, bool increased, string? trackTitle)
    {
        var title = trackTitle ?? Strings.App_Name;
        var direction = muted ? "🔇" : increased ? "🔊" : "🔉";

        string body;
        if (muted)
        {
            body = Strings.Notification_Muted;
        }
        else
        {
            body = LocalizedFormat.Format(
                CultureInfo.CurrentCulture,
                Strings.Notification_VolumeFormat,
                direction,
                volume);
        }

        if (!string.IsNullOrWhiteSpace(trackTitle))
        {
            body = muted
                ? LocalizedFormat.Format(CultureInfo.CurrentCulture, Strings.Notification_TrackFormat, direction, trackTitle)
                : LocalizedFormat.Format(CultureInfo.CurrentCulture, Strings.Notification_TrackVolumeFormat, direction, trackTitle, volume);
        }

        // With the sound muted the level is zero: the bar has to agree with
        // what the user hears.
        Show(title, body, VolumeDisplayDuration, muted ? 0 : volume);
    }

    /// <inheritdoc />
    public void ShowMessage(string title, string message) => Show(title, message, MessageDisplayDuration, null);

    /// <summary>
    /// Shows an informational message for the given amount of time.
    /// </summary>
    /// <remarks>
    /// The notices that have to be read, like the one of the first run, stay
    /// on screen longer than the ones that only inform about a change.
    /// </remarks>
    /// <param name="title">Message title.</param>
    /// <param name="message">Message body.</param>
    /// <param name="duration">Time visible before closing.</param>
    public void ShowMessage(string title, string message, TimeSpan duration) => Show(title, message, duration, null);

    private void Show(string title, string message, TimeSpan duration, int? level)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            Present(title, message, duration, level);
            return;
        }

        dispatcher.BeginInvoke(() => Present(title, message, duration, level));
    }

    /// <summary>
    /// Shows the notification, or updates the one that is already on screen.
    /// </summary>
    /// <remarks>
    /// There is only one notification at a time: the one that is visible is
    /// updated in place, so the volume is seen going up while the key is
    /// held down and the notifications never pile up.
    /// </remarks>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body.</param>
    /// <param name="duration">Time visible before closing.</param>
    /// <param name="level">Level to show in the bar, from 0 to 100, or null to hide it.</param>
    private void Present(string title, string message, TimeSpan duration, int? level)
    {
        if (_overlay is not null && _overlay.IsVisible)
        {
            _overlay.Update(title, message, duration, level);
            return;
        }

        var overlay = new NotificationOverlay();
        overlay.Closed += (_, _) =>
        {
            if (ReferenceEquals(_overlay, overlay))
            {
                _overlay = null;
            }
        };

        _overlay = overlay;

        overlay.Show(title, message, duration, level);
    }
}
