namespace MediaPilot.Platform.Volume;

/// <summary>
/// Resolves the output device that the application controls.
/// </summary>
internal interface IAudioEndpointResolver
{
    /// <summary>
    /// Resolves the volume control of the default output device.
    /// </summary>
    /// <remarks>
    /// It is resolved on every call and never kept: the user can change the
    /// default output device at any moment, and the volume commands have to
    /// follow the device that is the default now, the same way the volume key
    /// of the keyboard does.
    /// </remarks>
    /// <returns>
    /// The control of the device, or <see langword="null"/> when there is no
    /// output device or it cannot be opened.
    /// </returns>
    IAudioEndpointVolumeControl? Resolve();
}
