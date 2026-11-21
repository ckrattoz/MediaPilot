using System.Runtime.InteropServices;
using MediaPilot.Core.Contracts;
using MediaPilot.Platform.Interop;

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
/// </remarks>
public sealed class WindowsVolumeController : IVolumeController, IDisposable
{
    private IAudioEndpointVolume? _endpointVolume;

    /// <inheritdoc />
    public bool TryGetVolume(out int volume)
    {
        volume = 0;

        var endpoint = GetEndpointVolume();
        if (endpoint is null)
        {
            return false;
        }

        if (endpoint.GetMasterVolumeLevelScalar(out var scalar) != AudioConstants.SOk)
        {
            return false;
        }

        volume = (int)Math.Round(Math.Clamp(scalar, 0f, 1f) * 100f, MidpointRounding.AwayFromZero);
        return true;
    }

    /// <inheritdoc />
    public bool TryGetMuted(out bool muted)
    {
        muted = false;

        var endpoint = GetEndpointVolume();
        if (endpoint is null)
        {
            return false;
        }

        if (endpoint.GetMute(out var state) != AudioConstants.SOk)
        {
            return false;
        }

        muted = state;
        return true;
    }

    /// <inheritdoc />
    public bool TrySetMuted(bool muted)
    {
        var endpoint = GetEndpointVolume();
        if (endpoint is null)
        {
            return false;
        }

        var context = Guid.Empty;
        return endpoint.SetMute(muted, ref context) == AudioConstants.SOk;
    }

    /// <inheritdoc />
    public bool TryIncreaseVolume()
    {
        var endpoint = GetEndpointVolume();
        if (endpoint is null)
        {
            return false;
        }

        var context = Guid.Empty;
        return endpoint.VolumeStepUp(ref context) == AudioConstants.SOk;
    }

    /// <inheritdoc />
    public bool TryDecreaseVolume()
    {
        var endpoint = GetEndpointVolume();
        if (endpoint is null)
        {
            return false;
        }

        var context = Guid.Empty;
        return endpoint.VolumeStepDown(ref context) == AudioConstants.SOk;
    }

    private IAudioEndpointVolume? GetEndpointVolume()
    {
        if (_endpointVolume is not null)
        {
            return _endpointVolume;
        }

        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorClass();

            if (enumerator.GetDefaultAudioEndpoint(
                    AudioConstants.EDataFlowRender,
                    AudioConstants.ERoleMultimedia,
                    out var device) != AudioConstants.SOk)
            {
                return null;
            }

            if (device.Activate(
                    typeof(IAudioEndpointVolume).GUID,
                    AudioConstants.ClsctxAll,
                    IntPtr.Zero,
                    out var instance) != AudioConstants.SOk)
            {
                return null;
            }

            _endpointVolume = (IAudioEndpointVolume)Marshal.GetObjectForIUnknown(instance);
            return _endpointVolume;
        }
        catch (Exception)
        {
            // No output device or no permissions: the notification simply
            // will not show the volume level.
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_endpointVolume is not null)
        {
            Marshal.ReleaseComObject(_endpointVolume);
            _endpointVolume = null;
        }

        GC.SuppressFinalize(this);
    }
}
