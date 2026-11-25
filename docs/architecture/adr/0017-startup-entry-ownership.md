# ADR 0017 - The installer registers the program in the Run key of the user

- **Status:** Accepted
- **Date:** 2026-11-25
- **Decides:** Who writes the entry that starts MediaPilot with Windows and who
  removes it.
- **Resolves:** The defect found in
  [ADR 0016](0016-per-user-installation.md) about the entry of the start-up.

## Context

The entry of the start-up is the value `MediaPilot` of the
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` key, which tells Windows
to run the program when the user signs in. The application writes it when the
user enables the setting *Start with Windows*, and it removes it when the user
disables it.

The installer also had a row for it, in the `RemoveRegistry` table, with the
idea that the uninstaller would remove it. It does not work that way. The
documentation of the `RemoveRegistryValues` action says that it removes a
value of the `RemoveRegistry` table when the component is set to be
**installed**, and the table exists to clean up what a previous installation
left behind. The two consequences were the opposite of what the row was meant
to do:

- The entry of the start-up **survived the uninstall**, and Windows tried to
  run an executable that no longer exists at every sign in.
- The entry of the start-up **was removed at every installation or repair**,
  which disabled the setting of the user in silence after every update. It
  happened during the development of this decision: the setting of the user
  was enabled and the entry was gone.

Windows Installer only removes the registry values that the package itself
wrote. The value of the `Run` key is written by the application at run time,
so the package never wrote it and the installer could not take it away.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **The installer writes the entry and the application reconciles it** | The uninstaller takes the entry away with the program, because the package wrote it; the entry always mirrors the setting | The entry is there after the installation, even if the user never enabled the automatic start: the program has to recognize that start and end |
| A custom action of the installer that removes the value | The application does not change at all | An executable custom action is not part of the transaction and Microsoft does not recommend it; the cleanup would happen even if the uninstall failed later |
| The application keeps the entry and nothing removes it | Nothing changes | The entry of a program that no longer exists is left in the registry of the user |
| A shortcut in the folder of start-up instead of the `Run` key | The same problem of the ownership, and another mechanism | The folder of start-up is shared by every program and it is not per user in the same way |

## Decision

**The installer writes the entry of the start-up and the application
reconciles it with the setting when it starts.**

### The installer

- The package writes the value `MediaPilot` of the `Run` key of the current
  user, in the component that registers the program. The value is
  `"[INSTALLFOLDER]MediaPilot.App.exe" --installer-startup`.
- Because the package wrote it, Windows Installer removes it when the program
  is uninstalled, with the rest of the component.
- The argument `--installer-startup` identifies the entry as the one of the
  installer. It is a constant of the application
  (`WindowsStartupService.InstallerStartupArgument`) and the installer writes
  the same text.

### The application

- When it starts, the application reads the setting and writes the entry
  accordingly: it writes it when the user enabled the automatic start and
  removes it when the user did not. The entry always mirrors the setting.
- When the application is started **by the entry of the installer** and the
  user has **not** enabled the automatic start, it removes the entry and ends.
  It does not stay in the notification area on its own: the user installed a
  program that does not start with Windows, and the entry of the installer is
  only a way for the uninstaller to clean up.
- The decision is in `StartupPolicy` and it is covered by tests.

### The setting

The setting keeps a single owner: `settings.json` says whether the program
starts with Windows, and the entry of the `Run` key is the way that Windows is
told about it. The installer writes the entry once, at the installation, and
the application keeps it in sync from that moment.

## Consequences

- **Positive:** the uninstaller removes the entry of the start-up. The
  computer is not left with an entry that points to a program that is gone.
- **Positive:** an update does not disable the setting of the user in
  silence.
- **Positive:** the application does not need a custom action and the
  installer does not need an executable one.
- **Negative:** the entry is written at the installation even when the user
  has not enabled the automatic start. The application removes it at the first
  start, and it ends without running when the installer started it.
- **Negative:** the argument of the installer is a contract between the
  installer and the application. If the application does not recognize it, the
  program starts and stays in the notification area, which is the old
  behavior and not a failure.
- **Accepted:** the entry is written again at every installation. It is the
  price of the ownership: the package has to write what it is going to
  remove.

## Measurements

- The `Registry` table of the MSI holds the row that writes the value
  `MediaPilot` of the `Run` key of the current user, and the `RemoveRegistry`
  table is not in the package any more.
- After the installation, the entry exists and points to the installed
  executable. After the uninstall, it does not exist.
- The application started with `--installer-startup` and the setting of the
  user deactivated removes the entry and ends; the application started by the
  user keeps running.
