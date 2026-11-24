# ADR 0013 - The volume moves one point at a time and the notification is live

- **Status:** Accepted
- **Date:** 2026-11-23
- **Decides:** How the volume is changed and how the volume notification is shown.

## Context

Two behaviors of the original AutoHotkey script did not survive the migration
to the native application, and the user asked for both of them to be changed.

**The step of the volume.** The script changed the volume with the step of the
system. The native application did the same, through `VolumeStepUp` and
`VolumeStepDown` of `IAudioEndpointVolume`. The step of the system is **two
percentage points**, so the volume moved in jumps of two, and the number that
the notification showed was never the number that the user could reach with a
single press.

**The delay of the notification.** The script did not notify on every press.
It waited 350 ms, and it only notified when the key was released, because every
notification was a new balloon and the balloons piled up on the screen while the
key was held down. The native application inherited the same delay, with the
same `Timer` and the same `NotificationDebounceMs` setting.

The delay is no longer needed: the notification is a single overlay window that
is updated in place, so there is nothing to pile up. The delay was therefore
removing the feedback that the user asked to see.

Two more facts came out of the measurements that were made against the real
system:

| Behavior of the API | Measured |
| --- | --- |
| `VolumeStepUp` | Moves the volume by the step of the system, 2 points |
| `VolumeStepUp` | **Does not** lift the silence |
| The multimedia volume key of the keyboard | Moves the volume by 2 points and **does** lift the silence |
| `SetMasterVolumeLevelScalar` | Sets the level to an absolute value, with any precision |

The script raised the volume with the multimedia key of the keyboard, so the
silence was lifted when the volume went up. The native application, which used
`VolumeStepUp`, left the device silent: the volume was raised and nothing could
be heard.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Absolute level of one point, with the silence lifted when the volume goes up** | It gives the step that the user asked for; it matches what the keyboard key does; the notification can be shown on every press | The level is written, not stepped: a change of the volume made by another application in the same instant can be overwritten |
| Keep `VolumeStepUp` | It is the call that the system provides for this | The step is two points, and the silence is not lifted |
| Keep the step of the system and divide it in two notifications | Nothing changes in the system | The volume still moves two points; the notification would lie about the level |
| Keep the 350 ms delay | It is what the original script did | The user does not see the volume going up while the key is held down, and the setting was never even exposed in the user interface |

## Decision

**Change the volume by one point at a time, and show the notification on every
change, updating the one that is already on screen.**

- `WindowsVolumeController` works with a constant step of one percentage point.
  It reads the level with `GetMasterVolumeLevelScalar`, rounds it to whole
  points, adds the step, limits the result to the range from 0 to 100 and
  writes it with `SetMasterVolumeLevelScalar`. Working in whole points makes
  every step land exactly on a number that the interface can show.
- **Raising the volume lifts the silence**, exactly like the multimedia key of
  the keyboard and like the original script did. Lowering the volume keeps the
  silence as it is.
- `MediaCommandExecutor` notifies **immediately**, with no timer and no pending
  state. The `NotificationDebounceMs` setting is removed: it was never exposed
  in the user interface, and the deserializer ignores unknown members, so the
  existing `settings.json` files keep working.
- `NotificationPresenter` keeps **one** overlay window. When it is on screen,
  it is updated in place with the new text instead of opening another one.
- `NotificationOverlay.Update` replaces the content and starts the dismiss
  timer again, so the notification stays visible while the value it shows keeps
  changing, and it closes two seconds after the last change.
- The setting `ShowNotifications` is now **applied**: it was exposed in the
  settings window but it was never read by the executor, so the notification
  appeared even with it turned off. It is also refreshed when the user saves
  the settings, without restarting the application.

## Consequences

- **Positive:** the volume moves one point at a time, and the notification
  shows the same number that the system has.
- **Positive:** the volume is seen going up while the key is held down. The
  measured latency between the shortcut and the text on screen is **237 ms**,
  and most of it is the creation of the window itself.
- **Positive:** the notifications never pile up: the measurement found **one**
  visible notification during a sequence of six presses.
- **Positive:** the setting `Show notifications` finally works.
- **Negative:** raising the volume lifts the silence. It is a behavior change
  with respect to the previous versions of the application, and it is the
  behavior of the original script and of the keyboard.
- **Negative:** the level is written instead of stepped, so a change of the
  volume made by another application in the same instant can be overwritten.
  The window of the race is a few milliseconds wide.
- **Accepted:** `NotificationDebounceMs` is no longer a setting. The files that
  still contain it are read without any problem and the member is dropped the
  next time the settings are saved.

## Measurements

Taken against the published application, with the notification window read
through UI Automation:

| Measurement | Result |
| --- | --- |
| Volume per press | 31 %, 32 %, 33 %, 34 %, 35 %, 36 %: one point each |
| Latency between the shortcut and the notification | 237 ms (the inherited delay was 350 ms) |
| Visible notifications during six presses | 1 |
| Time on screen after the last change | About 2 s |
| Silence when the volume goes up | Lifted: 36 % and silent becomes 37 % and audible |
