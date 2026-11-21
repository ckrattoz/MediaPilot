# ADR 0001 - C# on .NET 10 LTS with WPF and the native Fluent theme

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** The language, runtime, and UI framework for MediaPilot.

## Context

MediaPilot replaces a 318-line AutoHotkey v2 script. The requirement is not
only to port the behavior: the application must also look and feel like a
native Windows 11 app, while avoiding carrying dead weight in the form of
dependencies.

The constraints set by the project owner were: no *underkill* (a poor UI or a
Win32 skeleton with hand-rolled controls does not qualify) and no *overkill*
(a third-party UI framework with its own lifecycle, packaging, and release
cadence is not being sought).

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **C# + .NET 10 + WPF (native Fluent)** | Full Win32 API surface, `PresentationFramework.Fluent` delivers the Windows 11 look with no dependencies, single self-contained executable, no WinAppSDK | WPF has no `ToggleSwitch` nor `NavigationView`; they have to be composed by hand |
| C# + WinUI 3 | Microsoft's "modern" framework, more capable XAML | Windows App SDK 2.x, semver with frequent breaking changes, ~35 MB of extra runtime, package lifecycle that complicates distribution |
| C++ / raw Win32 | Maximum control, minimal binary | Hand-maintained UI with no accessibility or styling; development cost disproportionate for this case |
| Rust + `windows-rs` | Small binary, no runtime | Immature UI ecosystem, steep learning curve, hand-written interop maintenance |
| C# + Native AOT + Win32 | Instant startup, no runtime | No WPF: the UI would be hand-rolled Win32 controls, exactly the *underkill* to avoid |

## Decision

**C# on .NET 10 LTS with WPF, using the Fluent theme shipped inside the
framework and no third-party UI library at all.**

Concrete reasons:

1. `PresentationFramework.Fluent` ships with the Windows Desktop Runtime. It
   was verified on the development machine that it exposes **63 BAML templates**
   and that the resource dictionary resolves as
   `pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml`.
   In other words: the modern look without adding a single package.
2. .NET 10 is LTS: a long support cycle without the noise of semiannual releases.
3. A *self-contained* publish produces an executable that runs on a machine
   without .NET installed, which is the common case for anyone who relies on
   multimedia hotkeys.
4. WPF gives direct access to `HwndSource`, `DwmSetWindowAttribute`, and the
   resource system: that is what makes Mica, rounded corners, and a dark title
   bar possible without tricks.

## Consequences

- **Positive:** native look with no dependencies, a single publishable artifact,
  direct P/Invoke to Win32, and domain tests that run without Windows.
- **Negative:** controls that WPF does not ship have to be built by hand (the
  Windows 11 style switch is a `CheckBox` with a custom template); WPF is in
  maintenance, although its API is stable.
- **Accepted:** both of the framework's omissions are addressed in
  `MediaPilot.UI/Themes/Controls.xaml`.

## Verification

Phase 3 of the project validated, with a real WPF application, that the Fluent
theme loads, that DWM accepts the Mica backdrop (attribute 38), the dark title
bar (20), and the rounded corners (33), and that `RegisterHotKey` and
`Shell_NotifyIcon` respond correctly.

## Revisited alternatives

If Microsoft ever ships a Windows UI framework with active support and without
the WinAppSDK weight, it is worth reevaluating. Until then, WPF with native
Fluent covers the case without adding dependencies.
