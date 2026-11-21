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

> **Project status:** under active development. The core functionality and the
> settings window are implemented; reading the actual track title (SMTC) and
> packaging for distribution come in later phases.

---

## Requirements

| Component | Version |
| --- | --- |
| Operating system | Windows 11 22H2 or later (build 22621+) |
| .NET SDK | 10.0.404 or later (LTS) |
| Compiler | MSVC 14.44 or later (Build Tools 2022 or VS 2026) |

The repository pins the SDK version in [`global.json`](global.json). To build
on another machine without the exact SDK, adjust the version number there.

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

## Tray menu

Right-click on the icon opens the native menu:

- **Open settings** - settings window (theme, hotkeys, start with Windows).
- **Active hotkeys** - enables or disables hotkey registration without leaving the application.
- **Start with Windows** - toggles the `Run` key in the user registry.
- **About** - version, copyright and link to the documentation.
- **Exit** - terminates the application.

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
  "NotificationDebounceMs": 350,
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
│   ├── MediaPilot.Platform/       Win32 interop: tray, hotkeys, volume, theme
│   ├── MediaPilot.UI/             Windows, style resources and view models
│   └── MediaPilot.App/            Composition root and entry point
├── tests/
│   ├── MediaPilot.Core.Tests/     Domain and persistence
│   └── MediaPilot.Platform.Tests/ Platform services
├── build/                         Build and publish scripts
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

## Publishing

`build/publish.ps1` produces a self-contained executable (it does not require
installing .NET on the target machine) or a runtime-dependent one, depending on
the `-SelfContained` parameter:

```powershell
./build/publish.ps1 -SelfContained -Runtime win-x64
```

The result ends up in `artifacts/publish/`. Distribution outside the development
environment (installer, Store package) is planned for a later phase.

## License

MIT. See [LICENSE](LICENSE).
