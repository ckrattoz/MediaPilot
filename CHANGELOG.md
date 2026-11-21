# Changelog

All relevant MediaPilot changes are recorded in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en-US/1.1.0/) and
the project adheres to [Semantic Versioning](https://semver.org/lang/en/).

## [Unreleased]

## [0.1.0] - 2026-11-21

First functional milestone of MediaPilot: the native Windows 11 application
that replaces the AutoHotkey script.

### Added

- Solution structure in five layers (`Core`, `Configuration`,
  `Platform`, `UI`, `App`) plus two test projects.
- Domain: `MediaCommand`, `HotkeyGesture`, `HotkeyBinding`, `AppSettings` and
  `DefaultHotkeys` with the seven hotkeys inherited from the original project.
- Global hotkey registration with `RegisterHotKey`, without a global keyboard
  hook.
- Native tray icon (`Shell_NotifyIcon`) with a Win32 context menu, a
  differentiated active/inactive state and notifications through `NIF_INFO`.
- Volume control over `IAudioEndpointVolume` (MMDevice), which reflects the
  actual volume of the playback endpoint.
- Detection of the system theme through `AppsUseLightTheme` and hot switching
  with the application's own palette.
- Settings window to reassign hotkeys, choose a theme and enable start with
  Windows.
- Custom notification overlay for the volume and the mute state.
- On-screen notice when a hotkey combination is already taken by another
  application: without it, the hotkey would fail silently.
- Configuration persistence in JSON with serialization generated at build time
  and schema migration.
- Diagnostic logging to a file with daily rotation.
- Internationalization from the first commit: a `MediaPilot.Localization`
  project with a neutral resource file (Spanish) and a satellite file per
  language (English today). The interface language follows the Windows UI
  language and can be forced with the `MEDIAPILOT_LANGUAGE` environment
  variable.
- `build/i18n.ps1` validates the resource files and creates the skeleton of a
  new language, so adding one is a copy and a translation.
- Documented git workflow: `main` as the only long-lived branch, Conventional
  Commits, Semantic Versioning and the release procedure.

### Fixed

Defects inherited from the original AutoHotkey project:

- The active and inactive tray icon was **the same file**; they are now two
  different icons, so the state is perceived at a glance.
- The window background was forced white and illegible in dark mode; the
  interface now follows the system theme and can be forced to light or dark.
- The position of the elements was computed with absolute coordinates; the
  layout is now adaptive and uses reusable styles.
- The track title was obtained with `WinGetTitle` over the active window, a
  fragile method that applied to any window of the system; the information
  command now shows an explicit state.
- The configuration was stored in two `REG_DWORD` registry values; it now
  lives in a readable, versionable and migratable JSON file.

Defects found while building the native application:

- The hotkey gesture was defined as a `record struct` with read-only
  properties, which `System.Text.Json` cannot populate: the saved hotkeys came
  back empty. It is now a positional record with `init` properties.
- The notification debounce configured in the settings was ignored by the
  command executor, which always used 350 ms.
- The notification overlay was positioned before its first layout, so it
  appeared off screen. It is now positioned once it is loaded, with its real
  size.
- The *Close* button of the About window was clipped by a fixed window height.
- The test that asserted that `DefaultHotkeys.All` returns an independent copy
  asserted the opposite of what it intended.

## [0.0.0] - 2021

Initial version of the AutoHotkey v2 script that gave rise to this project. The
full history of that repository cannot be recovered: its first Git commit was
left empty.
