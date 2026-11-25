# MediaPilot

Global multimedia control for Windows 11. MediaPilot lives in the notification
area and registers global hotkeys that play, pause and change the volume
without the player window having focus.

It is the native reimplementation of **MultimediaHotkeys**, the AutoHotkey v2
script maintained since 2021. The difference is not only the language:
MediaPilot fixes real defects in the original (indistinguishable tray icons, a
forced white background that is incompatible with dark mode, hotkeys that
cannot be reconfigured) and replaces AutoHotkey's global hook with the system
hotkey registration.

> **Project status:** under active development. The hotkeys, the volume
> control, the notifications, the track information and the installer are
> implemented. The next milestone is the distribution outside the development
> environment.

---

## Requirements

| Component | Version |
| --- | --- |
| Operating system | Windows 11 22H2 or later (build 22621+) |
| .NET SDK | 10.0.404 or later (LTS) |
| Compiler | MSVC 14.44 or later (Build Tools 2022 or VS 2026) |

The repository pins the SDK version in [`global.json`](global.json). To build
on another machine without the exact SDK, adjust the version number there.

The runtime uses the globalization data of Windows, so the application needs a
Windows installation with the standard locales. The invariant globalization
mode is not an option: WPF resolves the default language of every element as
`en-us` and builds real cultures in the font cache, and it dies as soon as a
window shows text. See
[ADR 0010](docs/architecture/adr/0010-wpf-and-globalization-invariant-mode.md).

## Build and run

```powershell
git clone <repository-url>
cd MediaPilot
dotnet restore MediaPilot.slnx
dotnet build MediaPilot.slnx -c Release
dotnet run --project src/MediaPilot.App/MediaPilot.App.csproj
```

Or, with the included scripts:

```powershell
./build/build.ps1        # restores, builds and runs the tests
./build/publish.ps1      # produces the publishable package in artifacts/
```

## Default hotkeys

| Hotkey | Action |
| --- | --- |
| `Ctrl` + `Alt` + `←` | Previous track |
| `Ctrl` + `Alt` + `↓` | Play / Pause |
| `Ctrl` + `Alt` + `→` | Next track |
| `Ctrl` + `Alt` + `Shift` + `←` | Volume − |
| `Ctrl` + `Alt` + `Shift` + `↓` | Mute |
| `Ctrl` + `Alt` + `Shift` + `→` | Volume + |
| `Ctrl` + `Alt` + `Shift` + `.` | Track information |

All seven hotkeys are reconfigurable from **Settings → Global hotkeys**
(opens with a right-click on the tray icon → *Open settings*).

A gesture needs at least one modifier: a hotkey without modifiers would
hijack that key system-wide, which is why validation rejects it.

## Track information

The *Track information* command reads the media session of the system
(`Windows.Media.Control`, the API that every player publishes) and shows the
title and the artist of what is playing.

The original script read the title of the foreground window, which showed the
title of any window of the system. The media session is the supported way and
works with the browser, with Spotify and with the movies and TV application.

## Tray menu

Right-click on the icon opens the native menu:

- **Open settings** - settings window (theme, hotkeys, start with Windows).
- **Active hotkeys** - enables or disables hotkey registration without leaving the application.
- **Start with Windows** - toggles the `Run` key in the user registry.
- **About** - version, copyright and link to the documentation.
- **Exit** - terminates the application.

The menu is the native one of Windows: it closes when the user clicks outside
of it or presses Escape. The settings window and the about window are reused,
so asking for them twice leaves a single window on screen.

The icon changes appearance according to the state: with the hotkeys active it
uses `active.ico`, and with the hotkeys disabled `inactive.ico`. The original
project reused the same file for both states, so the user could not perceive
the difference.

### If a hotkey does not respond

Windows assigns key combinations globally: if another application has already
registered the same combination, `RegisterHotKey` fails and the hotkey does
nothing. This is the most common case when migrating from the original
project, because **both programs register the same seven hotkeys**.

MediaPilot detects this and reports it: a notice appears with the conflicting
combination. The solution is to close the other program or to reassign the
hotkey from *Settings → Keyboard shortcuts*.
## Settings and local data

| Path | Content |
| --- | --- |
| `%LOCALAPPDATA%\MediaPilot\settings.json` | Application settings |
| `%LOCALAPPDATA%\MediaPilot\logs\mediapilot-YYYYMMDD.log` | Diagnostics |

`settings.json` is meant to be readable and editable by hand:

```json
{
  "SchemaVersion": 1,
  "HotkeysEnabled": true,
  "StartWithWindows": false,
  "ShowNotifications": true,
  "Theme": "Auto",
  "Hotkeys": [
    {
      "Gesture": { "Modifiers": "Alt, Control", "Key": "Left" },
      "Command": "Previous"
    }
  ]
}
```

The `SchemaVersion` field allows migrating the file when the schema changes;
migrations are applied on read, and the user's file is never rewritten without
reason.

## Repository structure

```
MediaPilot.slnx
├── src/
│   ├── MediaPilot.Core/           Pure domain and contracts (net10.0)
│   ├── MediaPilot.Configuration/  JSON store and paths (net10.0)
│   ├── MediaPilot.Localization/   UI strings and culture resolution (net10.0)
│   ├── MediaPilot.Platform/       Win32 and WinRT interop: tray, hotkeys, volume, theme, media sessions
│   ├── MediaPilot.UI/             Windows, style resources and view models
│   └── MediaPilot.App/            Composition root and entry point
├── tests/
│   ├── MediaPilot.Core.Tests/     Domain and persistence
│   └── MediaPilot.Platform.Tests/ Platform services
├── installer/                     WiX project that builds the MSI
│   ├── Localization/              Texts of the installer, in Spanish
├── build/                         Build, publish and packaging scripts
└── docs/architecture/             Architecture and decision records
```

Dependencies point inward, never outward: `Core` does not know about Windows,
`Platform` does not know about WPF and `UI` does not know about Win32. The
assembly that ties everything together is `App`, and only in `CompositionRoot`
are the concrete implementations known.

## Languages

The user interface is internationalized from the first commit. All user-facing
text lives in `src/MediaPilot.Localization/Resources/` as `.resx` resources, not
hardcoded in the code.

| File | Language |
| --- | --- |
| `Strings.resx` | Spanish - the neutral resource, also the fallback |
| `Strings.en.resx` | English |

The language follows the Windows UI language of the system. To force a
language for testing or for a specific account, set the `MEDIAPILOT_LANGUAGE`
environment variable to a culture name, for example:

```powershell
$env:MEDIAPILOT_LANGUAGE = 'en-US'
```

Adding a language means copying `Strings.en.resx` to `Strings.<culture>.resx`
and translating the values. The details are in
[`docs/development/internationalization.md`](docs/development/internationalization.md).

## Documentation

- [`AGENTS.md`](AGENTS.md) - conventions for anyone who modifies the code.
- [`docs/architecture/overview.md`](docs/architecture/overview.md) - map of the layers and the flow of a command.
- [`docs/architecture/adr/`](docs/architecture/adr/) - decision records with the *why* behind each technical choice.
- [`docs/development/git-workflow.md`](docs/development/git-workflow.md) - branching, commit format, tags and releases.
- [`docs/development/internationalization.md`](docs/development/internationalization.md) - how to add or change a UI string and how to add a language.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) - style, tests and workflow.
- [`CHANGELOG.md`](CHANGELOG.md) - change history.

## Quality

The repository builds with `TreatWarningsAsErrors` and the .NET analyzers in
`Recommended` mode, so a warning is a build error. The tests run on every
continuous integration:

```powershell
dotnet test MediaPilot.slnx -c Release
```

## Publishing and installation

`build/publish.ps1` produces a self-contained executable (it does not require
installing .NET on the target machine) or a runtime-dependent one, depending on
the `-SelfContained` parameter:

```powershell
./build/publish.ps1 -SelfContained -Runtime win-x64
```

The result ends up in `artifacts/publish/`.

`build/package.ps1` goes one step further: it publishes the application and
builds the MSI installer over that output, with the WiX Toolset (which comes
from NuGet, there is nothing to install on the build machine):

```powershell
./build/package.ps1
```

The installer ends up in `artifacts/packages/`. It installs MediaPilot in
`Program Files`, creates a shortcut in the Start menu and registers itself so
that Windows can repair or uninstall it.

The installer is in **Spanish**, the neutral language of the product, and its
texts live in `installer/Localization/MediaPilot.es-ES.wxl`. The license it
shows is the MIT license of the project, in `installer/License.rtf`. The
uninstaller closes MediaPilot, removes the files and the shortcut, and removes
the entry that starts it with Windows; the settings and the log of the
application are kept, because they belong to the user.

The installer is a separate project and is not part of the solution: it needs
the published output to exist before it can be compiled.

## License

MIT. See [LICENSE](LICENSE).
