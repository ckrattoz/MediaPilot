# ADR 0004 - MMDevice volume instead of the legacy mixer

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** Which API is used to read and adjust the system volume.
- **Partially superseded:** the endpoint is no longer cached, see
  [ADR 0014](0014-default-output-device-is-resolved-every-time.md); the step of
  the system is no longer used, see
  [ADR 0013](0013-volume-step-and-live-notification.md).

## Context

The original script adjusted volume with the Wave Audio mixer
(`waveOutGetVolume` / `waveOutSetVolume`). That API belongs to the Windows 95
era audio stack and **does not represent the system volume**: on Windows 11 the
volume is owned by the Audio Engine, and the Wave mixer is no longer the
current path.

## Empirical verification

Before writing a single line of code, the real behavior was measured on the
development machine. The result was conclusive:

```
waveOutGetVolume(...) -> 0xFFFFFFFF
```

The value `0xFFFFFFFF` is not a volume: it is the error returned when the Wave
mixing device is unavailable or does not represent the render endpoint. On the
same machine, the keyboard volume keys adjusted the volume correctly and that
change was reflected in the system UI.

Conclusion: with the legacy API, MediaPilot **would not have been able to
read the real volume**, and the volume notification would have shown a
meaningless number.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| `waveOutGetVolume` / `waveOutSetVolume` | Simple, what the original used | Returns `0xFFFFFFFF` on Windows 11: unusable |
| MMDevice `IAudioEndpointVolume` | It is the current API; reflects the real render endpoint volume and mute state | Requires COM interop: the interfaces are hand-implemented by vtable |
| Simulate the multimedia key presses | Nothing to write | Depends on the keyboard vendor exposing those keys; not portable |
| Third-party audio library | Less code | External dependency for something the system API already solves |

## Decision

**Use MMDevice `IAudioEndpointVolume` through hand-written COM interop.**

- `IMMDeviceEnumerator` (`A95664D2-9614-4F35-A746-DE8DB63617E6`), instantiated
  from the `MMDeviceEnumeratorClass` class
  (`BCDE0395-E52F-467C-8E3D-C4579291692E`).
- `GetDefaultAudioEndpoint` with flow `eRender` (0) and role `eMultimedia` (1):
  the device where applications play their sound. The console role is tried
  first now, with the multimedia role as the fallback. See
  [ADR 0014](0014-default-output-device-is-resolved-every-time.md).
- `IMMDevice.Activate` instantiates `IAudioEndpointVolume`
  (`5CDF2C82-841E-4546-9722-0CF74078229A`) with `CLSCTX_ALL` (0x7).
- The returned pointer is wrapped with `Marshal.GetObjectForIUnknown`.

Operations used:

| Operation | Interface |
| --- | --- |
| Read volume | `GetMasterVolumeLevelScalar` -> scalar 0..1 -> percentage |
| Raise / lower volume | `VolumeStepUp` / `VolumeStepDown` |
| Read and change mute | `GetMute` / `SetMute` |

The step (`VolumeStepUp`) is used instead of `SetMasterVolumeLevelScalar`
because it reproduces exactly the behavior of the system multimedia keys: one
discrete step per press, with the same granularity as Windows itself.

## Consequences

- **Positive:** the volume notification shows the real value, matching what the
  user sees in the Windows mixer and what the keyboard volume keys display. It
  is the same API the system uses.
- **Negative:** the hand-written COM interop is the most delicate fragment of
  the project: the vtable must stay in sync with the native interface, and a
  signature change would break silently.
- **Mitigation:** the interface is isolated in
  `MediaPilot.Platform/Interop/AudioDeviceInterop.cs` and the controller in
  `MediaPilot.Platform/Volume/WindowsVolumeController.cs`. No other layer
  knows that COM exists.

## Other implementation details

- **Fail-open.** `GetEndpointVolume` catches any exception and returns `null`;
  the public methods return `false` in that case. With no output device the
  application keeps working and simply does not show the volume level. An
  audio failure must never prevent a command from running.
- **The endpoint is not cached.** It was resolved once and reused, and that
  made the volume commands reach the device that was the default when the
  application started, even after the user changed the default output device.
  It is resolved on every operation now. See
  [ADR 0014](0014-default-output-device-is-resolved-every-time.md).
- **The constants are centralized** in `AudioConstants` (`EDataFlowRender`,
  `ERoleMultimedia`, `ClsctxAll`, `SOk`), so the vtable remains the only place
  where method order matters.
