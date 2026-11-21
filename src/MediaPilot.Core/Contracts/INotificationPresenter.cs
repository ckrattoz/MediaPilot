namespace MediaPilot.Core.Contracts;

/// <summary>
/// Presentation of notices to the user.
/// </summary>
public interface INotificationPresenter
{
    /// <summary>
    /// Shows the volume state.
    /// </summary>
    /// <param name="volume">Current volume as a percentage (0-100).</param>
    /// <param name="muted">Indicates whether the system is muted.</param>
    /// <param name="increased">
    /// <see langword="true"/> if the volume increased, <see langword="false"/> if it decreased.
    /// </param>
    /// <param name="trackTitle">Title of the track being played, if known.</param>
    void ShowVolume(int volume, bool muted, bool increased, string? trackTitle);

    /// <summary>
    /// Shows an informational message.
    /// </summary>
    /// <param name="title">Message title.</param>
    /// <param name="message">Message body.</param>
    void ShowMessage(string title, string message);
}
