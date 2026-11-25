# ADR 0015 - The installer has its own texts and a complete uninstaller

- **Status:** Accepted
- **Date:** 2026-11-24
- **Decides:** What the installer says and what the uninstaller leaves behind.
- **Extends:** [ADR 0009](0009-msi-installer-wix.md), which decided how the MSI
  is built.

## Context

The installer was built with the `WixUI_InstallDir` dialog set and nothing
else. The result was a package that looked like a proof of concept:

- The **license dialog showed the literal Lorem ipsum** of the extension. The
  `WixUI` library carries a placeholder license, a rich text file with the
  classic Latin filler, for the projects that do not bring their own license.
  The user had to accept a text that means nothing.
- **Every text was the generic one of WiX, in English**: "Welcome to the
  Setup Wizard", "This feature has no independently selectable features",
  "Highlighted volumes do not have enough disk space available for selected
  features". For a product whose interface is in Spanish, and that installs a
  single program without options, they are noise.
- The **uninstaller was incomplete**. It removed the files and the shortcut,
  and it left behind the entry that starts MediaPilot with Windows: after the
  uninstall, Windows tried to run an executable that no longer existed, at
  every logon. It also did not close the application, and MediaPilot lives in
  the notification area: it keeps running with its files deleted until the
  user finds the icon.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Keep the dialogs of the extension and give them the text of MediaPilot** | The user sees the wizard that every Windows program has; the text is ours, in our language | The texts that the extension does not name cannot be changed |
| Write every dialog from scratch | Total control over the layout and the text | A lot of XML to maintain, and a wizard that does not look like the one the user knows |
| Keep the extension as it comes | Nothing to write | The Lorem ipsum, the generic English texts and the uninstall that leaves the startup entry behind |
| Build the installer in the language of the system | Every user reads their own language | An MSI has one language: it needs a package per language, and the choice of language is not known until the user opens it |
| Let the installer write the startup entry | The installer owns the setting | Two sources of truth for the same setting, and the application would have to stop writing it |

## Decision

**Keep the dialogs of the WixUI extension, give them the text of MediaPilot in
Spanish, and complete the uninstaller.**

### The texts

- The installer is built with `<Cultures>es-ES</Cultures>`. Spanish is the
  neutral language of the product: it is the language of the resource files
  and the fallback of the application.
- The extension brings its own translation for that culture, and
  `installer/Localization/MediaPilot.es-ES.wxl` overrides the texts that are
  generic or that mention "features", "setup wizard" or "highlighted volumes".
  Every string of the extension is marked as overridable, and the file only
  contains the ones that MediaPilot writes differently.
- The tone is the one of the application: treatment of "tu", the same as the
  interface.

### The license

- `installer/License.rtf` holds the real MIT license of the project, and it is
  bound to the variable `WixUILicenseRtf`, which is the one that the extension
  reads to fill the license control. The placeholder of the extension is not
  used.

### The uninstaller

- `util:CloseApplication` closes MediaPilot before the installer replaces or
  removes its files. The application answers `WM_CLOSE` and exits, so the
  tray icon does not survive its own installation.
- `RemoveRegistryValue` removes the entry of the `Run` key when the program is
  uninstalled. The setting keeps a single owner: the application writes it
  when the user asks for it, and the installer only cleans it up when the
  program is gone.
- **The user data is not removed.** The settings and the diagnostic log are in
  `%LOCALAPPDATA%\MediaPilot`, they belong to the user and they survive a
  reinstall. The text of the uninstaller says so.

### The entry of the installed programs

- It has the icon of the application, a comment that describes it and the link
  to the repository.

## Consequences

- **Positive:** the license dialog shows the MIT license of the project
  instead of a paragraph of Latin filler.
- **Positive:** the whole installer speaks the same language and the same
  tone as the application.
- **Positive:** the uninstaller leaves nothing that points to a program that no
  longer exists, and the application is closed instead of being left running
  with its files deleted.
- **Positive:** the texts are in one file, so they are reviewed and translated
  together.
- **Negative:** the installer is built for one language. An English version is
  one property (`Cultures`) and one file away, but it is a second package.
- **Negative:** the list of texts is a copy of the one of the extension: when
  the extension is updated, the file has to be reviewed, because a string that
  does not exist any more is an error in the build.
- **Accepted:** the user data survives the uninstall. A program that deletes
  the configuration of the user without asking is worse than one that leaves
  a folder behind.

## Measurements

- The license control of the 0.2.5 MSI contained the Lorem ipsum of the
  extension. The one of the 0.3.0 MSI contains the MIT license.
- The validation of the package reports zero warnings of the ICE validation,
  and `msiexec /a` returns 0.
- The `RemoveRegistry` table of the MSI holds the row that removes the
  `MediaPilot` value of `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- The custom actions `Wix4CloseApplications_X64` and
  `Wix4CloseApplicationsDeferred_X64` are in the package.
