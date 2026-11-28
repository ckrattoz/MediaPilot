# AGENTS.md

Working notes for anyone - human or automated - that modifies this repository.

## What this repository is

MediaPilot is a native Windows 11 application that replaces the AutoHotkey v2
script `MultimediaHotkeys`. It lives in the notification area and registers
seven global hotkeys for playback and volume control.

The original AutoHotkey repository is a read-only reference. It is never
modified, and it is not a submodule of this one.

## Build, test and run

```powershell
./build/build.ps1                              # restore, validate, build, test
dotnet build MediaPilot.slnx -c Release        # build only
dotnet test  MediaPilot.slnx -c Release        # tests only
./build/i18n.ps1 -Check                        # validate the resource files
./build/publish.ps1 -SelfContained -Runtime win-x64
./build/package.ps1                            # publish + build the MSI installer
dotnet run --project src/MediaPilot.App/MediaPilot.App.csproj
```

The installer is a WiX project under `installer/`. It is **not** part of the
solution: it needs the published output to exist before it can be compiled, so
`build/package.ps1` publishes the application first and then builds it.

The repository builds with `TreatWarningsAsErrors` and the .NET analyzers in
`Recommended` mode. **A warning is an error.** Never silence a warning to make
the build pass without understanding it.

The tests are 72 in total: 33 in `MediaPilot.Core.Tests` (no Windows needed)
and 39 in `MediaPilot.Platform.Tests` (they touch the real platform APIs).

## Language rules

| Content | Language |
| --- | --- |
| Code, comments, XML documentation | English (American) |
| Documentation | English (American) |
| User interface strings | Spanish, and only in the resource files |

Three rules that are easy to get wrong:

1. **No user-facing text is hardcoded.** Every string lives in
   `src/MediaPilot.Localization/Resources/Strings.resx` or in a satellite file
   `Strings.<culture>.resx`. From XAML, resolve it with the `loc` markup
   extension: `{loc:Localize Settings_Heading}`.
2. **Use the simple hyphen** in any user-facing text. The em dash and the en
   dash are not used anywhere in the interface.
3. **Log messages are in English.** The diagnostic log is a developer
   artifact, not part of the interface.

## Internationalization

The neutral resource file holds the primary language (Spanish) and is the
fallback. Every other language is a satellite file. Adding a language is:

```powershell
./build/i18n.ps1 -Sync -Culture pt-BR    # creates the skeleton
./build/i18n.ps1 -Check                   # validates it
```

No project file has to be edited: the SDK turns every `Strings.<culture>.resx`
into a satellite assembly on its own, and the publish step includes them all.

The full procedure is in
[`docs/development/internationalization.md`](docs/development/internationalization.md).

## Versioning

The versioning of this repository is administered end to end by the automated
maintainer that works on it: the branch layout, the commit messages, the
tags and the releases. The rules are:

- `main` is the only long-lived branch. Work happens in
  `feature/<topic>`, `fix/<topic>`, `refactor/<topic>`, `docs/<topic>` or
  `chore/<topic>` branches.
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/):
  `<type>(<scope>): <subject>`, in English, in the imperative mood.
- Versioning is [Semantic Versioning](https://semver.org). The version lives in
  the `VersionPrefix` property of `Directory.Build.props`.
- Releases are tagged `vX.Y.Z` with an annotated tag.

The complete procedure is in
[`docs/development/git-workflow.md`](docs/development/git-workflow.md).

## Layer rules

The dependency direction is one way, and each layer knows exactly one
platform type:

| Project | Target | Knows |
| --- | --- | --- |
| `MediaPilot.Core` | `net10.0` | Nothing platform specific |
| `MediaPilot.Configuration` | `net10.0` | The file system |
| `MediaPilot.Localization` | `net10.0` | The resource manager |
| `MediaPilot.Platform` | `net10.0-windows10.0.19041.0` | Win32 and WinRT, not WPF |
| `MediaPilot.UI` | `net10.0-windows10.0.19041.0` | WPF, not Win32 |
| `MediaPilot.App` | `net10.0-windows10.0.19041.0` | Everything, in `CompositionRoot` |

`Core` must never reference Windows. If the domain needs to know about the
operating system, the design is wrong.

The Windows specific target framework is required by the media session API:
the WinRT projections only exist there. `Platform`, `UI`, `App` and
`Platform.Tests` all use it, and a project that references `Platform` has to
use it too, otherwise the restore fails with NU1201.

## Things that are easy to break

- **`RegisterHotKey` needs at least one modifier.** `HotkeyGesture.IsValid`
  enforces it, and the settings window applies it.
- **Hotkeys are global.** If another application owns the combination,
  `RegisterHotKey` returns `false` with `ERROR_HOTKEY_ALREADY_REGISTERED`
  (1409). `CompositionRoot.ApplyHotkeys` reports the conflict to the user; do
  not make it fail silently.
- **`waveOutGetVolume` does not work on Windows 11.** It returns
  `0xFFFFFFFF`. The volume goes through MMDevice.
- **The track information comes from the media session API**, not from the
  title of the foreground window. `MediaSessionMonitor` keeps the snapshot and
  the domain only sees `MediaTrack`. The WinRT types stay in the platform
  layer.
- **The theme is resolved in code, never with `ThemeMode="System"`.** On a
  machine in a mixed configuration it produces an illegible UI. See
  [ADR 0002](docs/architecture/adr/0002-explicit-theme-resolution.md).
- **The drop-down of the theme has a template of its own.** The Fluent theme of
  WPF paints the combo box with the colors of the light mode even when the
  application runs in dark mode, so `MediaPilot.ThemeComboBox` draws it with the
  palette of the application. A style that leans on the keys of the framework
  comes back white on the dark theme.
- **The scroll bar has a template of its own, too.** The Fluent theme of WPF
  paints it with the colors of the light mode as well, so the implicit style of
  the scroll bar draws it with the palette of the application. A control of the
  framework that is not covered comes back white on the dark theme.
- **The culture is applied before the first window is created.** XAML resolves
  the strings while the visual tree is built.
- **Never enable the invariant globalization mode.** WPF uses `en-us` as the
  default language of every element and builds real cultures in the font cache:
  in invariant mode every window that shows text dies with a
  `CultureNotFoundException` and the process ends with
  `STATUS_FATAL_USER_CALLBACK_EXCEPTION`. `GlobalizationTests` guards it. See
  [ADR 0010](docs/architecture/adr/0010-wpf-and-globalization-invariant-mode.md).
- **Nothing that can fail runs inside the window procedure.** The tray menu is
  shown from the host window procedure, that is, from inside a native callback:
  an exception that escapes it terminates the process. The commands are
  scheduled on the dispatcher and every call of the tray service is guarded.
- **The popup menu is dismissed with `PostMessage(WM_NULL)`.** It is the
  documented way to tell the owner window that the menu is over. Without it the
  menu stays on screen when the user clicks outside of it.
- **The injected structures must have the size of the native ones.** The union
  of `INPUT` declares every member of the native union, not only the one that
  is used: its size is the size of the largest member, and `SendInput` rejects
  the whole call with `ERROR_INVALID_PARAMETER` when the structure does not
  match. `KeyInputTests` guards it. See
  [ADR 0011](docs/architecture/adr/0011-native-size-of-the-injected-structures.md).
- **The media keys are injected with their scan code.** An event injected as a
  virtual key arrives as an ordinary key to the focused window and never
  reaches the media key router of Windows, so the playback commands behave in
  a random way. `KeyInputService` asks `MapVirtualKey` for the scan code and
  builds the events with `wVk = 0`. See
  [ADR 0012](docs/architecture/adr/0012-media-keys-with-their-scan-code.md).
- **The volume moves one point at a time and the notification is live.** The
  level is read with `GetMasterVolumeLevelScalar`, increased by one point and
  written back with `SetMasterVolumeLevelScalar`: the step of the system is two
  points and it does not lift the silence. Raising the volume has to lift it.
  The notification is shown on every change, without any debounce, and the
  overlay that is on screen is updated in place instead of opening another one.
  See
  [ADR 0013](docs/architecture/adr/0013-volume-step-and-live-notification.md).
- **The default output device is resolved on every operation.** It must never
  be cached: the user can change the default output device at any moment, and
  a cached endpoint keeps controlling the device that was the default when the
  application started. `WindowsVolumeController` asks an
  `IAudioEndpointResolver` for the device on every call, and the console role
  is tried first, with the multimedia role as the fallback. See
  [ADR 0014](docs/architecture/adr/0014-default-output-device-is-resolved-every-time.md).
- **Do not use `string.Format` with a resource string.** Use
  `LocalizedFormat.Format`, which keeps a parsed `CompositeFormat` and
  satisfies CA1863. The format provider is `CultureInfo.CurrentCulture`: the
  text of the message is already chosen by the resource manager with the
  interface culture.
- **The installer does not declare a `<Compile>` item.** The WiX SDK already
  includes the `.wxs` files of the folder: declaring them again compiles the
  package twice and the files are harvested twice.
- **The installer does not declare an `<EmbeddedResource>` item either.** The
  SDK includes the `.wxl` files of the folder on its own, and declaring them
  again makes the build fail with a duplicated localization identifier.
- **The texts of the installer live in
  `installer/Localization/MediaPilot.es-ES.wxl`.** The installer is built for
  one culture, the one declared in the `Cultures` property of the WiX project.
  A string that does not exist in the extension is a build error: check the
  identifiers of the extension before adding one.
- **The license of the installer is `installer/License.rtf`**, bound to the
  variable `WixUILicenseRtf`. The extension carries a placeholder license with
  a Latin filler text, and it is what the user sees if the binding is missing.
- **The uninstaller has to close the application.** MediaPilot lives in the
  notification area: `util:CloseApplication` asks it to close, and the
  application answers `WM_CLOSE` and exits. See
  [ADR 0015](docs/architecture/adr/0015-installer-texts-and-uninstaller.md).
- **The package is installed for the current user.** It is declared with
  `Scope="perUser"` and it installs into `%LOCALAPPDATA%\Programs\MediaPilot`,
  so it never asks for the elevation of an administrator. The key path of the
  component that registers the program is a value of `HKCU`, so the state of
  the installation belongs to the user. See
  [ADR 0016](docs/architecture/adr/0016-per-user-installation.md).
- **The uninstaller removes the tree of folders of the installation.** The
  files live in the profile of the user and the folders of the languages of the
  runtime change with the version of .NET, so the rows of the `RemoveFile`
  table cannot be written when the package is built:
  `util:RemoveFolderEx` writes them when the program is uninstalled, with the
  folders that exist at that moment. The folder is read back from the registry
  (`HKCU\Software\Antonio Núñez\MediaPilot\InstallFolder`), because the property of
  the installation directory is not resolved anymore at that point.
- **The installer writes the entry of the start-up, and the application keeps
  it in sync.** Windows Installer only removes the registry values that the
  package itself wrote, so the installer writes the value `MediaPilot` of the
  `Run` key, with the argument `--installer-startup`, and the uninstaller
  takes it away with the program. The application reconciles the entry with
  the setting of the user when it starts, and it ends without running when
  the installer started it and the user has not enabled the automatic start.
  The argument is a constant of `WindowsStartupService` and the installer
  writes the same text: if one of the two changes, the other one has to
  change with it. See
  [ADR 0017](docs/architecture/adr/0017-startup-entry-ownership.md).
- **The master of the mark is code, and the files it writes are
  versioned.** The geometry and the palette live in `tools/MediaPilot.Brand`,
  and the PNG and the ICO that the product uses are committed. After changing
  the master, regenerate the files with the `assets` command of the tool and
  commit the result: a change of the master without its files leaves the
  product showing the old mark.
- **A window shows the 32 pixel PNG, not the ICO.** WPF builds the icon of a
  window from the first frame of the ICO, and the first frame is the
  smallest one, so the icon of the title bar ends up being a 16 pixel image
  stretched. The system does not have that problem: it picks the frame that
  fits the size it needs, which is why the ICO is the right format for the
  executable and for the notification area.
- **The images of the installer are BMP and they are opaque.** The `Bitmap`
  control of Windows Installer does not read PNG and it does not blend the
  image, so the alpha channel of the files is forced to 255. Their size is
  the one of the control of the extension: 370x234 for the welcome dialog
  and 370x44 for the band.
- **The wizard images are replaced through the strings of the
  localization.** The value of `WelcomeDlgBitmap` and of every
  `*BannerBitmap` is the name of a binary of the package, not a file. See
  [ADR 0018](docs/architecture/adr/0018-the-identity-of-the-product.md).
- **The final dialog of the wizard cannot be scheduled around.** It is a
  termination action and its sequence number is negative, so the compiler
  rejects with `WIX0177` any action that is scheduled before or after it.
  Whatever has to happen when the user closes the dialog is a **control event**
  on its *Finish* button (`DoAction`), with an order that is greater than the
  999 of the extension, so that it runs after the event that closes the
  dialog. See
  [ADR 0019](docs/architecture/adr/0019-one-instance-and-the-first-run-notice.md).
- **The property of a type 50 action holds the path of the executable, and
  the target holds the line of commands.** The path must not be quoted: the
  installer uses it as the name of the program to start, and `CreateProcess`
  fails with `ERROR_CREATE_FAILED` (1631) when the quotes are part of it. The
  target needs the quotes, because it is the command line.
- **The text of a property cannot be the value of another property.** The
  texts of the check box and of the note of the final dialog are written with
  `!(loc.Id)`, which is resolved at build time against the localization file
  of the package.
- **Only one instance of MediaPilot runs at a time.** The composition root
  takes the mutex `Local\MediaPilot.SingleInstance` before it builds anything
  else, and the second instance sends `WM_APP + 2` to the hidden window of the
  running one and leaves. The window is looked for **by its title**: WPF gives
  its hidden window the class of its wrapper, which changes with every build.
- **The notice of the first run is shown once.** The flag
  `AppSettings.OnboardingCompleted` lives with the settings, and **every place
  that rebuilds `AppSettings` has to carry it over**: `SettingsViewModel.Save`,
  `JsonSettingsStore.Migrate`, `CompositionRoot.ToggleHotkeys`,
  `CompositionRoot.ToggleStartup` and the notice of the first run itself. A
  place that forgets it makes the notice appear again. See
  [ADR 0019](docs/architecture/adr/0019-one-instance-and-the-first-run-notice.md).
- **The link of the about window opens the browser with the shell.** The
  address is a web address, and `UseShellExecute` is what lets the default
  browser of the user take it: without it `Process.Start` fails and the link
  does nothing.
- **The public page lives in `site/` and it is published by the workflow
  `pages.yml`.** The folder **is** the site: there is no build step, and the
  REST API of GitHub does not accept a folder other than the root or `/docs`,
  so the publication goes through GitHub Actions. The download button points to
  `releases/latest/download/MediaPilot-x64.msi`, so the asset of the latest
  release has to keep that name, without the version inside, and the SHA-256
  that the page publishes has to be updated with every release, because the
  installer is built again and its bytes change. See
  [the guide of the public site](docs/development/public-site.md).

## Decision records

Every technical choice with discarded alternatives is recorded in
`docs/architecture/adr/`. When a change introduces a new architectural
decision, add a new ADR instead of editing an existing one.
