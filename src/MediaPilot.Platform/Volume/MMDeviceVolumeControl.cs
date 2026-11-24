using System.Runtime.InteropServices;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Volume;

/// <summary>
/// Volume control of an output device, over the COM interface of the
/// multimedia audio API of Windows.
/// </summary>
internal sealed class MMDeviceVolumeControl : IAudioEndpointVolumeControl
{
    private readonly IAudioEndpointVolume _volume;

    /// <summary>
    /// Initializes the control.
    /// </summary>
    /// <param name="volume">The volume interface of the device.</param>
    internal MMDeviceVolumeControl(IAudioEndpointVolume volume)
    {
        _volume = volume;
    }

    /// <inheritdoc />
    public bool TryGetLevel(out float level)
    {
        level = 0f;

        return _volume.GetMasterVolumeLevelScalar(out level) == AudioConstants.SOk;
    }

    /// <inheritdoc />
    public bool TryGetMuted(out bool muted)
    {
        muted = false;

        var state = false;
        if (_volume.GetMute(out state) != AudioConstants.SOk)
        {
            return false;
        }

        muted = state;
        return true;
    }

    /// <inheritdoc />
    public bool SetMuted(bool muted)
    {
        var context = Guid.Empty;

        return _volume.SetMute(muted, ref context) == AudioConstants.SOk;
    }

    /// <inheritdoc />
    public bool SetLevel(float level)
    {
        var context = Guid.Empty;

        return _volume.SetMasterVolumeLevelScalar(level, ref context) == AudioConstants.SOk;
    }
}
