# ADR 0002 - Explicit theme resolution and an application-owned palette

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** How the UI theme is determined and where the colors come from.

## Context

The original project forced a white background. On top of the Windows 11 dark
theme that produced a white window with white text: the UI was illegible. A
native replacement has to follow the system theme and let the user pin it.

During Phase 3 an unforeseen problem was found: **the development machine runs
in a mixed configuration**. In
`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`:

- `SystemUsesLightTheme = 0` (the system shell is dark)
- `AppsUseLightTheme = 1` (applications are light)

With `ThemeMode="System"` in XAML, WPF read "System" as *the application*
theme and painted a light background, while the title bar and the taskbar
stayed dark. The result was an incoherent window, with light input controls
and white text on white inside them: illegible.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| `ThemeMode="System"` | Less code | Ambiguous under a mixed configuration; produces an illegible UI |
| Fixed `ThemeMode="Dark"` | Simple and coherent | Ignores the user's preference |
| **Read `AppsUseLightTheme` and resolve the theme in code** | Deterministic, honors the real application preference, allows pinning light or dark | Requires reacting to live changes |
| Use only the Fluent theme resource keys | Leverages the framework | Microsoft's internal resource names may change between versions; without `BasedOn` on `StaticResource` the palettes cannot be derived cleanly |

## Decision

**1. The theme is always resolved in code, never with `ThemeMode="System"`.**

`WindowsThemeService` reads
`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
directly (`1` = light, `0` = dark) and exposes `IsDark`. `ThemeManager` then
decides:

- `ThemePreference.Light` -> light
- `ThemePreference.Dark` -> dark
- `ThemePreference.Auto` -> the value of `AppsUseLightTheme`

Live switching happens through `WM_SETTINGCHANGE`, which is intercepted by the
0x0 host window.

**2. The application owns the palette, not the framework.**

`MediaPilot.UI/Themes/Palette.Light.xaml` and `Palette.Dark.xaml` hold the
application's own color keys (`Surface.*`, `Text.*`, `Accent.*`, `Border.*`),
and `ThemeManager` swaps the active dictionary. The control styles
(`Themes/Controls.xaml`) are built on top of those keys.

## Consequences

- **Positive:** the UI is coherent under any system configuration, and the user
  can force light or dark. MediaPilot's colors do not depend on Microsoft
  internal resources.
- **Negative:** two palette files have to be maintained; a new color has to be
  added to both, and forgetting a key in one of them makes the UI fail at
  runtime with `MissingResourceException`.

### Known limitation

Today the synchronization between the two palettes is **manual**: there is no
automated check that both define the same set of keys. This is a consciously
accepted risk because it affects a single file and only shows up when the theme
changes.

The natural mitigation - validating the keys at startup and reporting through
the diagnostic log instead of failing silently - is recorded as a pending
improvement for a later phase.

## Note on the experimental API

`ThemeMode` (`WPF0001`) is an experimental API and the diagnostic only appears
when it is accessed **from code**. Therefore:

- `App.xaml` declares `ThemeMode="Light"` as the initial value: being a XAML
  attribute it produces no warning and guarantees a coherent theme from the
  first frame, before `ThemeManager` resolves the real preference.
- `ThemeManager.SetThemeMode` does reassign the property on every theme change,
  which enables live switching. That call is wrapped in
  `#pragma warning disable WPF0001` with the justification inline.
