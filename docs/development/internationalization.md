# Internationalization

Every string the user can read lives in a resource file. No user-facing text is
hardcoded in the C# sources or in the XAML files. This document explains where
the strings live, how to change them, and how to add a new language.

## Layout

```
src/MediaPilot.Localization/
├── MediaPilot.Localization.csproj
├── CultureProvider.cs            Resolves and applies the UI culture
└── Resources/
    ├── Strings.resx               Neutral file: the primary language (Spanish)
    └── Strings.en.resx            Satellite file: English
```

The neutral file holds the primary language and is embedded in the main
assembly. Every other language lives in its own satellite file
(`Strings.<culture>.resx`) and is compiled into a satellite assembly named
`<culture>/MediaPilot.Localization.resources.dll`. The .NET SDK picks up any
`Strings.<culture>.resx` file placed in that folder automatically: there is
nothing to register in the project file.

The project has no dependency on `Microsoft.Extensions.Localization`. The
resource manager of the framework is enough for an application of this size,
and adding the package would only bring in abstractions that MediaPilot does
not need.

## How a string is resolved

1. `CultureProvider.Apply()` runs at startup, before any window is created. It
   sets `CurrentUICulture` and `DefaultThreadCurrentUICulture`.
2. The culture comes from the `MEDIAPILOT_LANGUAGE` environment variable when
   it is set and valid. Otherwise the UI culture of the operating system is
   used.
3. When the requested culture has no satellite file, the resource manager
   falls back to the neutral file, so the interface is always available in the
   primary language.

The order matters: XAML resolves the strings while the visual tree is built,
so the culture has to be set before the first window exists. That is why
`App.OnStartup` calls `CultureProvider.Apply()` as its first statement.

### Trying another language

```powershell
$env:MEDIAPILOT_LANGUAGE = 'en'
dotnet run --project src/MediaPilot.App/MediaPilot.App.csproj
```

```powershell
$env:MEDIAPILOT_LANGUAGE = 'es-ES'
```

## Using a string

### From XAML

The `loc` markup extension resolves a key against the current UI culture:

```xml
<Window xmlns:loc="clr-namespace:MediaPilot.UI.Resources;assembly=MediaPilot.UI">
    <TextBlock Text="{loc:Localize Settings_Heading}" />
</Window>
```

When the key does not exist, the extension returns the key itself. A missing
translation is therefore visible in the interface instead of rendering an empty
label.

### From C#

The strongly typed designer class is generated at build time by the
`GenerateResxSource` target of the .NET SDK. It exposes one property per
entry, with the underscores of the key turned into underscores of the
property name:

```csharp
string title = Strings.Settings_WindowTitle;
```

The generated class is public, so it is used from `MediaPilot.UI` and
`MediaPilot.App` without any additional plumbing.

### Format strings

Formatted entries use the standard .NET placeholders and are applied with
`string.Format` and the current UI culture:

```csharp
string text = string.Format(
    CultureInfo.CurrentUICulture,
    Strings.Notification_VolumeFormat,
    volume);
```

## Naming convention of the keys

The key describes the place and the purpose, never the wording:

```
<Area>_<Element>_<Purpose>
```

| Prefix | Area |
| --- | --- |
| `App_` | Application name and tagline |
| `Tray_Menu_` | Tray icon context menu |
| `Settings_` | Settings window |
| `About_` | About window |
| `Command_` | Multimedia command names |
| `Key_` | Names of the individual keys |
| `Hotkey_` | Hotkey capture hints |
| `Notification_` | Notification overlay |
| `Executor_` | Messages produced while executing a command |
| `HotkeyConflict_` | Warning about a combination owned by another application |
| `Startup_` | Startup failures |

Rules:

- The key is in English, because the code is in English. The wording of the
  translation is not part of the key.
- The key is never reused for two different meanings.
- A key that is renamed is a breaking change for the satellite files: the
  script below detects the mismatch.

## Changing a string

1. Edit the neutral file `Resources/Strings.resx` and change the Spanish text.
2. Run the synchronization script so the satellite files stay consistent:

   ```powershell
   ./build/i18n.ps1 -Sync -Culture en
   ```

3. Translate the entries that the script marked as pending.
4. Validate:

   ```powershell
   ./build/i18n.ps1 -Check
   ```

## Adding a language

1. Generate the satellite file with the script. It creates the file with the
   schema of the neutral file, copies every key and marks the ones that still
   need a translation:

   ```powershell
   ./build/i18n.ps1 -Sync -Culture pt-BR
   ```

2. Translate the entries marked with the `Pending translation` comment. Remove
   that comment once the entry is translated, or run the script again: an entry
   that still carries the marker is reset to the neutral value.

3. Validate the result:

   ```powershell
   ./build/i18n.ps1 -Check
   ```

4. Build. The satellite assembly is produced automatically:

   ```powershell
   dotnet build MediaPilot.slnx -c Release
   ```

Nothing else has to be registered: the SDK turns every
`Strings.<culture>.resx` of that folder into a satellite assembly, and the
resource manager picks it when the culture matches.

The publish step does not filter the satellites either. The
`SatelliteResourceLanguages` property is deliberately left unset in
`MediaPilot.App.csproj`, so every language present in the repository reaches
the published package without touching the project file.

## The validation script

`build/i18n.ps1` is the single source of truth for the consistency of the
resource files.

| Command | What it does |
| --- | --- |
| `./build/i18n.ps1 -Check` | Compares every satellite file against the neutral one. Reports missing keys, unexpected keys and empty values as errors, and entries that are still pending translation as warnings. Exits with code 1 when there is an error. |
| `./build/i18n.ps1 -Sync -Culture <culture>` | Creates or refreshes a satellite file. Existing translations are preserved, new or pending entries receive the neutral value and the `Pending translation` comment. |

The check runs in two places, so a mismatch cannot reach `main`:

- `./build/build.ps1`, before the build.
- The `build` job of the continuous integration
  (`.github/workflows/ci.yml`).

## Rules for translators

- **The simple hyphen, not the long dash.** The interface uses the plain `-`
  character. The em dash and the en dash are not used in any user-facing text.
- Keep the tone consistent with the existing texts: short, direct, without
  exclamation marks.
- Text that is a format string keeps its placeholders untouched:
  `{0}`, `{1}`. The order of the placeholders must not change between
  languages unless the translation also updates every call site.
- Do not translate the application name `MediaPilot`, the name of the product
  `Windows`, or the name of a key.
- The text of a tooltip, a label and a notification is the same string when it
  means the same thing. If two entries mean the same, they should still be
  separate keys, because they may be reworded independently.
