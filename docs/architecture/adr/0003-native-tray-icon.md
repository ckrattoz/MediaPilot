# ADR 0003 - System tray with `Shell_NotifyIcon` instead of WinForms

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** How the notification area icon and its context menu are
  implemented.

## Context

MediaPilot has no main window: it lives in the notification area. WPF ships no
tray control, so the Windows API has to be used, and there are two ways to do
it.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| `System.Windows.Forms.NotifyIcon` | Complete and already solved API | Forces a WinForms reference (`<UseWindowsForms>true</UseWindowsForms>`), which **triggers the `CS0104` conflict**: WinForms and WPF both declare an `Application` type |
| **`Shell_NotifyIcon` + Win32 context menu via P/Invoke** | Full control, no dependencies, native messages with `NOTIFYICON_VERSION_4` | The interop has to be written by hand |
| Third-party tray library | Less code | External dependency for something Win32 already solves |

## Decision

**Direct interop with `shell32.dll` and `user32.dll`.**

- `TrayIconService` implements `ITrayIcon` and registers the icon with
  `Shell_NotifyIconW`.
- The `NOTIFYICONDATAW` struct is declared by hand because it contains
  fixed-size string fields and is therefore **not blittable**: the P/Invoke
  source generator (`LibraryImport`) cannot use it, so `DllImport` is used with
  the `SYSLIB1051` warning explicitly suppressed. This is a localized decision:
  the rest of the interop uses `LibraryImport`.
- The context menu is a native Win32 menu (`CreatePopupMenu`, `AppendMenuW`,
  `TrackPopupMenu`), with one command per entry.
- The icon version is `NOTIFYICON_VERSION_4`, which packs the event and the
  icon identifier into the low 16 bits of `lParam`.
- Icons are loaded with `LoadImageW` and destroyed with `DestroyIcon`.

## Consequences

- **Positive:** no type conflict between WPF and WinForms; the menu has the
  native system look; memory usage is lower than with the WinForms
  `NotifyIcon`, which loads an extra assembly.
- **Negative:** more interop code, and the menu cannot hold stateful dynamic
  entries (the enabled state is reflected through the tray icon, not through a
  check mark in the menu).
- **Accepted consequence:** the absence of `System.Windows.Forms` from the
  project is intentional. If it ever becomes necessary, the alias
  `using Application = System.Windows.Application;` resolves the `CS0104`
  conflict, but the preferred option remains not using WinForms.

## Inherited detail that was fixed

The original project reused **the same `.ico` file** for the active and
inactive states, so the user could not perceive the difference. Here there are
two genuinely different icons (`active.ico` and `inactive.ico`), which
`TrayIconService` swaps with `NIM_MODIFY` depending on the state.
