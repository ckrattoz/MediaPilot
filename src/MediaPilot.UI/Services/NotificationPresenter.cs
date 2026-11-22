using System.Globalization;
using System.Windows;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Localization;
using MediaPilot.Localization.Resources;

namespace MediaPilot.UI.Services;

/// <summary>
/// Presents notifications through the floating window of the user interface.
/// </summary>
public sealed class NotificationPresenter : INotificationPresenter
{
    private static readonly TimeSpan VolumeDisplayDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MessageDisplayDuration = TimeSpan.FromSeconds(3);

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

        Show(title, body, VolumeDisplayDuration);
    }

    /// <inheritdoc />
    public void ShowMessage(string title, string message) => Show(title, message, MessageDisplayDuration);

    private static void Show(string title, string message, TimeSpan duration)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
        {
            Present(title, message, duration);
            return;
        }

        dispatcher.BeginInvoke(() => Present(title, message, duration));
    }

    private static void Present(string title, string message, TimeSpan duration)
    {
        var overlay = new Views.NotificationOverlay();
        overlay.Show(title, message, duration);
    }
}
