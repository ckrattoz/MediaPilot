# ADR 0005 - `RegisterHotKey` instead of a global keyboard hook

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** How global hotkey presses are detected.

## Context

The original project used the AutoHotkey global keyboard hook
(`Hook` / `#HotIf`), which intercepts **every** keystroke on the system
looking for the combinations of interest.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| Global keyboard hook | Works with any key, including multimedia keys | Intercepts all system input; idle CPU cost; on some security configurations it requires elevation |
| **Win32 `RegisterHotKey`** | The system delivers the combination directly; the application receives nothing while idle | Only works with combinations that include at least one modifier, and each hotkey consumes a global system combination |
| `Raw Input` | Flexible | Filtering has to be implemented, and filtering is exactly the work to avoid |

## Decision

**`RegisterHotKey` from `user32.dll`.**

- `WindowsHotkeyService` implements `IHotkeyService` and registers every
  mapping in `AppSettings.Hotkeys` with `RegisterHotKey` on the 0x0 host
  window.
- The system posts `WM_HOTKEY` to that window; `NativeMessageRouter` dispatches
  it to the service, which raises `CommandTriggered` with the mapped command.
- Modifiers are translated to the Win32 values: Alt = 1, Control = 2, Shift = 4,
  Win = 8.
- `UnregisterAll` is called before re-registering, so reloading the settings
  never leaves an orphaned hotkey.

## Accepted restriction

`RegisterHotKey` **requires at least one modifier** (Alt, Control, Shift, or
Win). A single-key hotkey is impossible with this API.

This is not an imposed limitation but a product rule: a hotkey without
modifiers would hijack that key across the whole system and make the machine
unusable. `HotkeyGesture.IsValid` states it explicitly and the settings window
enforces it when capturing hotkeys.

## Conflicts with other applications

`RegisterHotKey` is global: the combination is reserved for the process while
it is registered. If another program already owns it, the call returns `false`
with `ERROR_HOTKEY_ALREADY_REGISTERED` (1409) and the hotkey simply does not
respond.

The common case while migrating is the original project itself: **it registers
the same seven hotkeys**, so both programs cannot coexist.

That is why `CompositionRoot.ApplyHotkeys` collects the hotkeys that failed
and warns the user through the notification overlay, naming the conflicting
combination. Without that warning the user would press the combination with no
idea another program is behind it; the failure would only be recorded in the
diagnostic file.

## Consequences

- **Positive:** the application processes no system input while idle; idle CPU
  usage is on the order of tenths; no elevated privileges are required; there
  is no hook that can conflict with another process.
- **Negative:** the hotkey stays registered in the system while the application
  is open, so another application cannot use the same combination.
  `RegisterHotKey` returns `false` in that case and `CompositionRoot.ApplyHotkeys`
  records it in the diagnostic log with a clear warning.
- **Accepted:** when two applications register the same combination, the first
  one to arrive keeps it. That is normal Windows behavior and not something the
  application can force.

## Windows requirement

`RegisterHotKey` reserves the combination for as long as the process lives.
Registering it again after a settings change requires calling
`UnregisterHotKey` first; `WindowsHotkeyService` guarantees this through its
`UnregisterAll`.
