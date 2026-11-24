using MediaPilot.Core.Contracts;

namespace MediaPilot.Platform.Volume;

/// <summary>
/// Control over the volume of the default output device.
/// </summary>
/// <remarks>
/// It uses the Windows multimedia audio API (<c>IMMDevice</c> +
/// <c>IAudioEndpointVolume</c>), which is the modern path and the only one
/// that reflects the actual system volume.
/// <para>
/// The legacy <c>waveOutGetVolume</c> API was discarded: on Windows 10/11 it
/// returns <c>0xFFFFFFFF</c> because the legacy mixer no longer exposes the
/// master volume.
/// </para>
/// <para>
/// The device is resolved on every operation and it is never kept: the user
/// can change the default output device at any moment, and the volume
/// commands have to follow the device that is the default now.
/// </para>
/// </remarks>
public sealed class WindowsVolumeController : IVolumeController, IDisposable
{
    /// <summary>Percentage points that a volume command changes.</summary>
    private const int VolumeStepPercent = 1;

    private readonly IAudioEndpointResolver _resolver;

    /// <summary>
    /// Initializes the controller with the resolver of the system.
    /// </summary>
    public WindowsVolumeController()
        : this(new MMDeviceAudioEndpointResolver())
    {
    }

    /// <summary>
    /// Initializes the controller with a resolver.
    /// </summary>
    /// <remarks>
    /// The resolver is asked for the device on every operation, so a test can
    /// return a different device each time and assert that the controller
    /// follows the default one.
    /// </remarks>
    /// <param name="resolver">Resolver of the default output device.</param>
    internal WindowsVolumeController(IAudioEndpointResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _resolver = resolver;
    }

    /// <inheritdoc />
    public bool TryGetVolume(out int volume)
    {
        volume = 0;

        var control = _resolver.Resolve();
        if (control is null)
        {
            return false;
        }

        if (!control.TryGetLevel(out var level))
        {
            return false;
        }

        volume = (int)Math.Round(Math.Clamp(level, 0f, 1f) * 100f, MidpointRounding.AwayFromZero);
        return true;
    }

    /// <inheritdoc />
    public bool TryGetMuted(out bool muted)
    {
        muted = false;

        var control = _resolver.Resolve();
        if (control is null)
        {
            return false;
        }

        return control.TryGetMuted(out muted);
    }

    /// <inheritdoc />
    public bool TrySetMuted(bool muted)
    {
        var control = _resolver.Resolve();
        if (control is null)
        {
            return false;
        }

        return control.SetMuted(muted);
    }

    /// <inheritdoc />
    public bool TryIncreaseVolume() => TryChangeVolume(VolumeStepPercent, liftMute: true);

    /// <inheritdoc />
    public bool TryDecreaseVolume() => TryChangeVolume(-VolumeStepPercent, liftMute: false);

    /// <summary>
    /// Changes the volume of the default output device by a fixed number of
    /// percentage points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The step of the system is not used: it is two points, and the user
    /// asked for the volume to move one point at a time. The level is set
    /// with <c>SetMasterVolumeLevelScalar</c> instead of stepping, and it is
    /// worked in whole points so that the steps land exactly on the numbers
    /// that the interface shows.
    /// </para>
    /// <para>
    /// Raising the volume lifts the silence, which is what the volume key of
    /// the keyboard does, and what the original project did.
    /// </para>
    /// </remarks>
    /// <param name="delta">Points to add, positive or negative.</param>
    /// <param name="liftMute">Whether the silence is lifted.</param>
    /// <returns><see langword="true"/> if the volume could be read.</returns>
    private bool TryChangeVolume(int delta, bool liftMute)
    {
        var control = _resolver.Resolve();
        if (control is null)
        {
            return false;
        }

        if (!control.TryGetLevel(out var level))
        {
            return false;
        }

        var current = (int)Math.Round(Math.Clamp(level, 0f, 1f) * 100f, MidpointRounding.AwayFromZero);
        var target = Math.Clamp(current + delta, 0, 100);

        if (liftMute && control.TryGetMuted(out var muted) && muted)
        {
            control.SetMuted(false);
        }

        if (target == current)
        {
            // The volume is already at the limit: there is nothing to change,
            // but the caller still shows the level that the system has.
            return true;
        }

        return control.SetLevel(target / 100f);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
