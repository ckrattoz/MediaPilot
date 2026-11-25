# Changelog

All relevant MediaPilot changes are recorded in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en-US/1.1.0/) and
the project adheres to [Semantic Versioning](https://semver.org/lang/en/).

## [Unreleased]

## [0.4.0] - 2026-11-25

The installer installs the program for the user who runs it, without asking
for the privileges of an administrator.

### Changed

- **The installation is per user and it does not ask for the elevation.** The
  program belongs to the user in every point: its settings, its shortcut, its
  entry of start-up, its hotkeys and its icon in the notification area. The
  installer asked for the privileges of an administrator only to write in a
  folder that nothing else uses, and it registered the installation for the
  whole computer, so two users of the same machine fought over the same
  hotkeys.
- The program is installed in `%LOCALAPPDATA%\Programs\MediaPilot`, the place
  that Windows reserves for the programs of the user, and the state of the
  installation is registered in the registry of that user.
- **The uninstaller removes the tree of folders.** A per-user installation
  lives in the profile, so the uninstaller has to take the folders with the
  files, including the folders of the languages of the runtime, whose number
  changes with the version of .NET. The action of the Util extension walks the
  installation folder and removes every file and every folder that it finds.

See
[ADR 0016](docs/architecture/adr/0016-per-user-installation.md).

## [0.3.0] - 2026-11-24

The installer speaks the language of the product and leaves nothing behind.

### Fixed

- **The license of the installer is the real one.** The dialog showed the
  literal Lorem ipsum that the WixUI extension carries for the projects that
  do not bring a license. It now shows the MIT license of MediaPilot.
- **The uninstaller removes the entry that starts MediaPilot with Windows.**
  It was left behind, so Windows tried to run an executable that no longer
  existed at every logon.
- **The uninstaller closes the application.** MediaPilot lives in the
  notification area and it was still running, with its files deleted, until
  the user found the icon. The installer asks it to close and the application
  answers.

### Changed

- **The installer is in Spanish**, the neutral language of the product, and
  its texts are the ones of MediaPilot instead of the generic ones of WiX. The
  tone is the same as the one of the application.
- The entry of the installed programs has the icon, a description and the link
  to the repository.

### Added

- The file `installer/Localization/MediaPilot.es-ES.wxl`, which holds the texts
  of the installer, and the file `installer/License.rtf`, which holds the
  license.
- The application answers `WM_CLOSE` and `WM_QUERYENDSESSION`, so it closes
  when the installer asks it to and when the session of Windows ends.

See
[ADR 0015](docs/architecture/adr/0015-installer-texts-and-uninstaller.md).

## [0.2.5] - 2026-11-24

Correction of the defect that left the volume commands without effect when the
user changes the default output device.

### Fixed

- **The volume commands follow the output device that is the default now.** The
  endpoint was resolved once, when the first volume command was run, and it
  was kept for the whole life of the application. Changing the default output
  device, for instance to the front panel of the sound card, left the commands
  reaching the previous device: the notification appeared and the level went
  down inside it, but the sound did not change. The device is resolved on every
  operation now, the same way the multimedia volume key of the keyboard does.
  See
  [ADR 0014](docs/architecture/adr/0014-default-output-device-is-resolved-every-time.md).
- **The notification shows the level of the device that is really playing**, and
  not the level of the device that was playing when the application started.

### Changed

- The default device is resolved with the console role of Windows, which is the
  one that the sound panel shows as the default output, and with the multimedia
  role as the fallback.
- The volume control of the device is behind an interface, so that the
  behavior can be tested without an audio device.

### Added

- Tests that assert that the device is resolved on every operation and that
  the command is applied to the device of the moment.

## [0.2.4] - 2026-11-23

Change in the way the volume is moved and in the way its notification is shown.

### Changed

- **The volume moves one point at a time.** It used the step of the system,
  which is two percentage points. The level is now read, increased by one point
  and written back, so the notification shows exactly the level that the system
  has.
- **The notification of the volume is live.** The 350 ms delay inherited from
  the original script is removed: the notification is shown on every change and
  the one that is on screen is updated in place, so the volume is seen going up
  while the key is held down and the notifications never pile up. The setting
  `NotificationDebounceMs` is removed with it.
- **Raising the volume lifts the silence**, the same way the volume key of the
  keyboard does and the same way the original script did.

### Fixed

- **The setting "Show notifications" works.** It was exposed in the settings
  window but the executor never read it, so the notification appeared even with
  it turned off. It is also applied as soon as the settings are saved.

### Added

- Tests that assert that the notification is shown immediately, that every
  change of the volume notifies again and that nothing is notified when the
  notifications are turned off.

See
[ADR 0013](docs/architecture/adr/0013-volume-step-and-live-notification.md).

## [0.2.3] - 2026-11-23

Correction of the reliability of the playback commands.

### Fixed

- **The pause and play command is reliable.** The media keys were injected
  with their virtual key, so the event arrived as an ordinary key to the
  focused window and never went through the media key router of Windows: the
  change between playing and paused happened only sometimes, and it depended
  on the window that had the focus. The keys are now injected with their scan
  code, the same way a physical media key reaches the system. See
  [ADR 0012](docs/architecture/adr/0012-media-keys-with-their-scan-code.md).
- **The next track and previous track commands use the same mechanism**, with
  the same route through the system.

### Added

- Tests that assert that the media keys are injected with their scan code and
  not with their virtual key.

## [0.2.2] - 2026-11-21

Correction of the defect that left the playback commands without effect.

### Fixed

- **The pause, next track and previous track commands work again.** The
  playback commands are sent to the system as the media keys of the keyboard,
  the same way the original script did. The structure that describes the
  injected events was declared with a single member in its union, so it was 32
  bytes instead of the 40 bytes that the native structure has on x64, and the
  system rejected the whole call with `ERROR_INVALID_PARAMETER` before
  injecting anything. The union now declares every member of the native one.
  See [ADR 0011](docs/architecture/adr/0011-native-size-of-the-injected-structures.md).
- **A rejected key is written to the log.** The result of the injection was
  discarded, so the failure of a playback command was invisible.

### Added

- Tests that assert the size of the injected structure and that the system
  accepts it.

## [0.2.1] - 2026-11-21

Correction of the defect that closed the application when a window was opened
from the tray menu.

### Fixed

- **The windows of the application no longer close the process.** The runtime
  was configured in globalization-invariant mode, which leaves it without
  cultures. WPF needs them: it uses `en-us` as the default language of every
  element and builds real cultures in the font cache, so opening the settings
  window or the about window died with a `CultureNotFoundException` and the
  process was terminated with `STATUS_FATAL_USER_CALLBACK_EXCEPTION`. The
  runtime now uses the cultures of the system. See
  [ADR 0010](docs/architecture/adr/0010-wpf-and-globalization-invariant-mode.md).
- **The context menu of the tray icon closes when the user clicks outside of
  it.** The owner window is notified with `PostMessage(WM_NULL)` right after
  the menu closes, which is the documented way to dismiss a popup menu.
- **An error in the user interface does not terminate the application.** The
  commands of the tray menu are scheduled on the dispatcher instead of running
  inside the window procedure, and every error is written to the log.

### Changed

- The format provider of the localized messages is now the regional culture
  instead of the interface culture. The text of the message is still chosen by
  the resource manager with the interface culture.
- The settings window and the about window are reused instead of opening a new
  copy every time they are requested from the tray menu.

## [0.2.0] - 2026-11-21

Second milestone: the real track information and the installer.

### Added

- **Track information through the media session API.** The information command
  shows the title and the artist of the track that is being played, read from
  the media session of the system (`Windows.Media.Control`). The original
  AutoHotkey script read the title of the foreground window, which showed the
  title of any window of the system, not the title of the track.
- **MSI installer built with the WiX Toolset.** The package installs the
  application in `Program Files`, creates a shortcut in the Start menu and
  registers itself so that Windows can repair or uninstall it. The toolset
  comes from NuGet: there is no software to install on the build machine.
- `build/package.ps1` publishes the application and builds the installer in
  one step.

### Changed

- The projects that target Windows now use `net10.0-windows10.0.19041.0`,
  which brings the Windows SDK projections needed by the media session API.
  The minimum supported system does not change: the application already
  required Windows 11 22H2.

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
