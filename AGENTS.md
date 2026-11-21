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
./build/publish.ps1 -SingleFile -Runtime win-x64
dotnet run --project src/MediaPilot.App/MediaPilot.App.csproj
```

The repository builds with `TreatWarningsAsErrors` and the .NET analyzers in
`Recommended` mode. **A warning is an error.** Never silence a warning to make
the build pass without understanding it.

The tests are 39 in total: 23 in `MediaPilot.Core.Tests` (no Windows needed)
and 16 in `MediaPilot.Platform.Tests` (they touch the real platform APIs).

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

The repository owner administers the versioning of this repository: the branch
layout, the commit messages, the tags and the releases. The rules are:

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
| `MediaPilot.Platform` | `net10.0-windows` | Win32, not WPF |
| `MediaPilot.UI` | `net10.0-windows` | WPF, not Win32 |
| `MediaPilot.App` | `net10.0-windows` | Everything, in `CompositionRoot` |

`Core` must never reference Windows. If the domain needs to know about the
operating system, the design is wrong.

## Things that are easy to break

- **`RegisterHotKey` needs at least one modifier.** `HotkeyGesture.IsValid`
  enforces it, and the settings window applies it.
- **Hotkeys are global.** If another application owns the combination,
  `RegisterHotKey` returns `false` with `ERROR_HOTKEY_ALREADY_REGISTERED`
  (1409). `CompositionRoot.ApplyHotkeys` reports the conflict to the user; do
  not make it fail silently.
- **`waveOutGetVolume` does not work on Windows 11.** It returns
  `0xFFFFFFFF`. The volume goes through MMDevice.
- **The theme is resolved in code, never with `ThemeMode="System"`.** On a
  machine in a mixed configuration it produces an illegible UI. See
  [ADR 0002](docs/architecture/adr/0002-explicit-theme-resolution.md).
- **The culture is applied before the first window is created.** XAML resolves
  the strings while the visual tree is built.
- **Do not use `string.Format` with a resource string.** Use
  `LocalizedFormat.Format`, which keeps a parsed `CompositeFormat` and
  satisfies CA1863.

## Decision records

Every technical choice with discarded alternatives is recorded in
`docs/architecture/adr/`. When a change introduces a new architectural
decision, add a new ADR instead of editing an existing one.
