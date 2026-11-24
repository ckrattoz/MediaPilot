using System.Runtime.InteropServices;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Volume;

/// <summary>
/// Resolves the default output device with the multimedia audio API of
/// Windows (MMDevice).
/// </summary>
/// <remarks>
/// The device is resolved on every call. The user can change the default
/// output device at any moment, and a cached device would keep controlling
/// the device that was the default when the application started.
/// </remarks>
internal sealed class MMDeviceAudioEndpointResolver : IAudioEndpointResolver
{
    /// <summary>
    /// Roles of the default device, in the order in which they are tried.
    /// </summary>
    /// <remarks>
    /// The console role is the one that the sound panel of Windows shows as
    /// the default output and the one that the volume controls of the system
    /// use. The multimedia role is the fallback for the machines in which the
    /// console role has no device.
    /// </remarks>
    private static readonly int[] DeviceRoles =
    [
        AudioConstants.ERoleConsole,
        AudioConstants.ERoleMultimedia,
    ];

    /// <inheritdoc />
    public IAudioEndpointVolumeControl? Resolve()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorClass();

            foreach (var role in DeviceRoles)
            {
                if (enumerator.GetDefaultAudioEndpoint(
                        AudioConstants.EDataFlowRender,
                        role,
                        out var device) != AudioConstants.SOk)
                {
                    continue;
                }

                var control = ActivateVolume(device);
                if (control is not null)
                {
                    return control;
                }
            }
        }
        catch (Exception)
        {
            // No output device or no permissions: the notification simply
            // will not show the volume level.
        }

        return null;
    }

    /// <summary>
    /// Activates the volume interface of a device.
    /// </summary>
    /// <param name="device">The device.</param>
    /// <returns>The control of the device, or <see langword="null"/>.</returns>
    private static MMDeviceVolumeControl? ActivateVolume(IMMDevice device)
    {
        var instance = IntPtr.Zero;

        try
        {
            if (device.Activate(
                    typeof(IAudioEndpointVolume).GUID,
                    AudioConstants.ClsctxAll,
                    IntPtr.Zero,
                    out instance) != AudioConstants.SOk)
            {
                return null;
            }

            // The runtime wrapper keeps its own reference to the interface, so
            // the raw pointer is released right away.
            var volume = (IAudioEndpointVolume)Marshal.GetObjectForIUnknown(instance);
            Marshal.Release(instance);

            return new MMDeviceVolumeControl(volume);
        }
        catch (Exception)
        {
            if (instance != IntPtr.Zero)
            {
                Marshal.Release(instance);
            }

            return null;
        }
    }
}
