# ADR 0014 - The default output device is resolved on every operation

- **Status:** Accepted
- **Date:** 2026-11-24
- **Decides:** Which output device the volume commands act on.
- **Partially supersedes:** [ADR 0004](0004-mmdevice-volume-control.md), the rule
  that cached the endpoint.

## Context

The user changed the default output device, from the default one of the sound
card to its front panel, and the volume commands stopped having any effect on
the sound. The notification appeared, the level inside it went down, and
nothing could be heard: the command was reaching a device that was not the one
playing.

The cause was in `WindowsVolumeController`: it resolved the endpoint once and
kept it in a field for the whole life of the application. From that moment on,
every command went to the device that had been the default when the
application started, and the change of the default device was never noticed.

The measurement, done with the published application and two real output
devices, reproduces it exactly:

| Build | Device that was the default | Device that became the default |
| --- | --- | --- |
| 0.2.4 | 99 % -> 96 % | 5 % -> 5 % |
| With the fix | 99 % -> 99 % | 5 % -> 2 % |

The reference is the behavior of the system itself. The multimedia volume key
of the keyboard always follows the device that is the default at that moment,
and it was measured twice on the same machine:

| Default device | Press of the multimedia volume key |
| --- | --- |
| `{dca9a7eb}` (98 %) | It went down to 96 % |
| `{0c58ea66}`, after the change (5 %) | It went down to 4 % |

The original script did not have this problem, because it sent the multimedia
key to the system and the system did the work.

The measurement of the roles also gave a result that decided the
implementation. On the development machine the default device was resolved
for the three roles of Windows, and:

- The **console** and the **multimedia** roles always point to the same
  device. Changing one of them moved the other, because Windows ties both to
  the legacy sound mapper.
- The **communications** role points to a different device and does not follow
  them. It is the device of the calls, and the volume of a call is not the
  volume of the music.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Resolve the default device on every operation** | It follows the device that is the default now, exactly like the multimedia key; it needs no callback and no state | One more COM call per command |
| Keep the endpoint and subscribe to the notifications of the device changes (`IMMNotificationClient`) | It follows the change and it could refresh the notification too | It needs a notification client, a callback that arrives from another thread and the synchronization that comes with it: much more code for the same result |
| Read the default device from the registry | The registry holds the sound mapper of the user | The registry is not the source of truth of the audio engine, and the value is not always there |
| Apply the volume to every output device | The volume would be the same everywhere | The user expects the volume of the device that is playing, and the others would be changed without being asked |

## Decision

**Resolve the default output device on every operation, and never keep it.**

- `WindowsVolumeController` asks an `IAudioEndpointResolver` for the device on
  every call and works with whatever it gets. The resolver is a seam: the
  tests give it a different device on each call and assert that the controller
  follows it.
- `MMDeviceAudioEndpointResolver` asks Windows for the default render endpoint
  with the **console** role, and falls back to the **multimedia** role when the
  console role has no device. The console role is the one that the sound panel
  of Windows shows as the default output and the one that the volume controls
  of the system use. The communications role is not used: it is the device of
  the calls, and it does not follow the device that the user has selected to
  listen to.
- The COM objects are released as soon as the operation finishes, so the
  repeated resolution does not accumulate references.

## Consequences

- **Positive:** the volume commands follow the output device that is the
  default now, the same way the multimedia key of the keyboard does. The
  measurement after the fix shows the new default going down and the previous
  device untouched.
- **Positive:** the notification shows the level of the device that is really
  playing, and not the level of a device that was playing minutes ago.
- **Positive:** the seam between the controller and the platform allows the
  behavior to be tested without an audio device, and the test that asserts it
  would have failed with the cached endpoint.
- **Negative:** every command resolves the device again, which is a call to the
  audio service of Windows. It only happens when the user presses a shortcut,
  and the measurement of the whole operation shows no visible delay.
- **Accepted:** the device is resolved once per public operation, so the
  notification reads the level of the device that is the default at that
  moment. If the user changes the default device in the same instant, the
  notification can show the level of the previous device. The window is a few
  milliseconds wide.

## Measurements

Taken with the published application, with two real output devices, and the
default device changed in the middle of the sequence:

| Measurement | 0.2.4 | With the fix |
| --- | --- | --- |
| Volume of the device that became the default | 5 % -> 5 % | 5 % -> 2 % |
| Volume of the previous default device | 99 % -> 96 % | 99 % -> 99 % |
| Level shown in the notification | The level of the previous device | The level of the current one: 4 % |
