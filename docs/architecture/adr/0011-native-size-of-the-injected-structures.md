# ADR 0011 - The injected key structures must have the native size

- **Status:** Accepted
- **Date:** 2026-11-21
- **Decides:** How the playback commands reach the media session.

## Context

The playback commands (pause, next track and previous track) did nothing, and
nothing was reported. The volume commands worked.

The difference is in the path of each command: the volume goes through MMDevice
and is applied directly to the endpoint, while the playback commands are sent
as the media keys of the keyboard, exactly as the original AutoHotkey script
did with `Send "{Media_Next}"`.

The injection is done with `SendInput`, and the structure that describes the
events was declared like this:

```csharp
[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint type;
    public INPUTUNION U;
}

[StructLayout(LayoutKind.Explicit)]
internal struct INPUTUNION
{
    [FieldOffset(0)]
    public KEYBDINPUT ki;
}
```

The native union has three members, and its size is the size of the largest
one, `MOUSEINPUT`: 32 bytes on x64. The declared union only had `KEYBDINPUT`,
which is 20 bytes, so the marshalled structure was **32 bytes instead of the 40
bytes that the native `INPUT` has**.

`SendInput` validates the size of the structure and rejects the whole call
when it does not match:

```
SendInput(...cbSize: 32) -> 0 events, Win32 error 87 (ERROR_INVALID_PARAMETER)
SendInput(...cbSize: 40) -> 2 events, Win32 error 0
```

Nothing was injected, so the media keys never reached the system and the
playback commands did nothing. The failure was invisible for three reasons:
the result of `TryExecute` was discarded, the service did not log anything, and
the defect only affected the commands that go through `SendInput`.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Media keys with the native structure** | The same mechanism as the original project; works with every player, including the ones that do not publish a session | Depends on the routing of the system keys |
| Control the session with `TryControlAsync` of SMTC | Direct and precise; it reports whether the command was accepted | Only reaches the application that publishes a session; it fails when the session is not there yet, and the failure is again silent |
| Control the session and fall back to the media keys | The most robust | Two mechanisms to maintain for the same result |

## Decision

**Fix the structure and keep the media keys.**

- The union declares every member of the native one (`MOUSEINPUT`,
  `KEYBDINPUT` and `HARDWAREINPUT`), so the marshalled structure has the size
  that the system expects on every architecture: 40 bytes on x64 and 28 bytes
  on x86.
- The tests assert the size of the structure, so a change that shrinks it
  again fails the build instead of breaking the playback commands silently.
- The service logs the Win32 error when the system rejects the event. The
  result of the call is not discarded anymore.

## Consequences

- **Positive:** the playback commands work, with the same mechanism and the
  same reach as the original script.
- **Positive:** the defect is detectable: a test covers the size of the
  structure and the failure is written to the log.
- **Negative:** the structure carries three members when only one is used.
  That is the price of matching the native layout, and the alternative is
  worse than the extra fields.
- **Accepted:** the media keys are routed by the system to the application that
  owns the media session. An application that does not publish a session does
  not receive them, exactly as with a physical keyboard.
