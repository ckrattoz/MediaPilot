namespace MediaPilot.Platform.Volume;

/// <summary>
/// Volume control of an audio output device.
/// </summary>
/// <remarks>
/// It hides the COM interface of the platform, so that the logic of the
/// controller can be tested without an audio device.
/// </remarks>
internal interface IAudioEndpointVolumeControl
{
    /// <summary>
    /// Reads the master level of the device.
    /// </summary>
    /// <param name="level">Level, as a value from 0 to 1.</param>
    /// <returns><see langword="true"/> if the level could be read.</returns>
    bool TryGetLevel(out float level);

    /// <summary>
    /// Reads the mute state of the device.
    /// </summary>
    /// <param name="muted">Indicates whether the device is silent.</param>
    /// <returns><see langword="true"/> if the state could be read.</returns>
    bool TryGetMuted(out bool muted);

    /// <summary>
    /// Sets the mute state of the device.
    /// </summary>
    /// <param name="muted">Desired mute state.</param>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool SetMuted(bool muted);

    /// <summary>
    /// Sets the master level of the device.
    /// </summary>
    /// <param name="level">Level, as a value from 0 to 1.</param>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool SetLevel(float level);
}
