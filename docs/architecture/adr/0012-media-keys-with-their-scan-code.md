# ADR 0012 - The media keys are injected with their scan code

- **Status:** Accepted
- **Date:** 2026-11-23
- **Decides:** How the media keys reach the application that is playing.

## Context

The playback commands were not reliable. The change between playing and paused
did not happen, and the user had to press the shortcut several times, in a
sequence that went through another command, to make it work. The next track
and previous track commands behaved better, but the erratic behaviour was
there.

The commands are sent to the system as the media keys of the keyboard, the
same way the original AutoHotkey script did with `Send "{Media_Next}"`. The
events were injected with `SendInput` describing the **virtual key**:

```csharp
ki = new KEYBDINPUT
{
    wVk = 0xB3,          // VK_MEDIA_PLAY_PAUSE
    wScan = 0,
    dwFlags = KEYEVENTF_EXTENDEDKEY,
}
```

The measurement, done with a real media session and the injector in a process
that does not own it, is unambiguous:

| Injection | Changes of state in four presses |
| --- | --- |
| Virtual key, with the extended flag | 0 of 4 |
| Virtual key, without the extended flag | 0 of 4 |
| **Scan code** | **4 of 4** |

An event injected as a virtual key is delivered as an ordinary key to the
focused window. The media key router of Windows, the one that turns a media
key into the command of the session that owns the media, watches the scan
code path of the keyboard: it never sees the event. The result depends on
which window has the focus, which is why the behaviour looked random and why
it changed when another key was pressed in between.

An event injected with the scan code follows the same path as a physical media
key, so the router sees it and the session receives the command.

The scan code is not hardcoded: it is the one that Windows itself uses, asked
through `MapVirtualKey`. For the media keys it is `0x0022` for play/pause,
`0x0019` for next track and `0x0010` for previous track, all of them extended
keys, so the event carries `KEYEVENTF_SCANCODE` and `KEYEVENTF_EXTENDEDKEY`.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Scan code** | It is what a physical key produces; the router of the system sees it; it works with every player | It needs the scan code of the key, which is asked to the system |
| Virtual key | It is the simplest description of the event | It never reaches the media key router |
| Control the session with `TryControlAsync` of SMTC | It is direct and it reports whether the command was accepted | It only reaches the applications that publish a session; it is asynchronous, and the domain of the application is synchronous |

## Decision

**Inject the media keys with their scan code.**

- `KeyInputService` asks `MapVirtualKey` for the scan code of the key and
  builds the events with `wVk = 0`, `wScan` and the flags
  `KEYEVENTF_SCANCODE | KEYEVENTF_EXTENDEDKEY`.
- The press and the release are sent in the same call, which is what the
  measurement confirmed as correct.
- A key without a scan code in the current layout falls back to the virtual
  key, so a future command never ends up doing nothing.
- The tests assert that the events of the media keys carry the scan code and
  the flag, and not the virtual key.

## Consequences

- **Positive:** the playback commands reach the media session, exactly as a
  physical media key would.
- **Positive:** the behaviour does not depend on which window has the focus.
- **Positive:** the events are consumed by the media key router, so they do
  not reach the focused window as stray keys. It was measured: a window in
  the foreground does not receive any key when the media keys are injected.
- **Negative:** the events depend on the scan code of the key, which is
  obtained from the system on every call. The call is cheap and happens once
  per command.
- **Accepted:** the media keys are routed by the system to the application
  that owns the media session. An application that does not publish a session
  does not receive them, exactly as with a physical keyboard.
