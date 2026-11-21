namespace MediaPilot.Core.Contracts;

/// <summary>
/// Control over the system output volume.
/// </summary>
public interface IVolumeController
{
    /// <summary>
    /// Gets the current volume, as a percentage (0-100).
    /// </summary>
    /// <param name="volume">Current volume.</param>
    /// <returns><see langword="true"/> if the volume could be read.</returns>
    bool TryGetVolume(out int volume);

    /// <summary>
    /// Gets the mute state.
    /// </summary>
    /// <param name="muted">Indicates whether the system is muted.</param>
    /// <returns><see langword="true"/> if the state could be read.</returns>
    bool TryGetMuted(out bool muted);

    /// <summary>
    /// Enables or disables mute.
    /// </summary>
    /// <param name="muted">Desired mute state.</param>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool TrySetMuted(bool muted);

    /// <summary>
    /// Increases the volume by one step.
    /// </summary>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool TryIncreaseVolume();

    /// <summary>
    /// Decreases the volume by one step.
    /// </summary>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool TryDecreaseVolume();
}
