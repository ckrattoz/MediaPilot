# ADR 0016 - MediaPilot is installed for the current user, without elevation

- **Status:** Accepted
- **Date:** 2026-11-25
- **Decides:** Who installs MediaPilot, where it is installed and how it is removed.
- **Extends:** [ADR 0009](0009-msi-installer-wix.md), which decided how the MSI is
  built, and [ADR 0015](0015-installer-texts-and-uninstaller.md), which decided
  what the uninstaller leaves behind.

## Context

The MSI of the 0.3.0 was declared with `Scope="perMachine"` and installed into
`C:\Program Files\MediaPilot`. Writing there is an operation of an
administrator, so Windows Installer asked for the elevation of the account
every single time, even to install a program that the account owns entirely:

- The settings and the diagnostic log are in `%LOCALAPPDATA%\MediaPilot`.
- The entry that starts the program with Windows is in the `Run` key of
  `HKEY_CURRENT_USER`.
- The global hotkeys are registered for the session of the user, and they are
  released when the session ends.
- The icon lives in the notification area of the user and the shortcut in the
  start menu of the user.

There is no part of MediaPilot that is shared between the users of the
computer. The elevation bought a directory that nothing else uses.

The per-machine installation has a second effect that matters more: the
components are registered for the whole computer. Two users of the same
computer installed the same program fought over the same global hotkeys, and
the second registration failed, because the hotkeys of the system are a
resource of the session and not of the profile.

The user asked whether the installer really needs to ask for privileges. It
does not, and the package is changed to not ask for them.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Per-user MSI in `%LOCALAPPDATA%`** | No elevation; each user has their own copy, their own hotkeys and their own settings; the same behavior as the other programs that the user installs | The files live in the profile, so they have to be removed carefully and the package has to answer the checks of the per-user installation |
| Keep the per-machine MSI | The standard for programs that install shared components | Needs the elevation of an administrator for something that belongs to one user, and the components are registered for the whole computer |
| Portable ZIP | Nothing to install | No entry in the installed programs, no shortcut, no uninstaller and no repair |
| MSIX | Automatic updates and a clean sandbox | Requires a signing certificate that has to be obtained and renewed, and the sideloading of the developer mode |

## Decision

**The package is declared `Scope="perUser"` and installs into
`%LOCALAPPDATA%\Programs\MediaPilot`.**

### The scope and the folder

- `Scope="perUser"` means that the package is installed for the current user
  and never for the whole computer.
- The folder is `%LOCALAPPDATA%\Programs\MediaPilot`, the same place that
  Windows uses for the programs of the user.
- The `UpgradeCode` is kept, so that the upgrade from the per-machine 0.3.0
  removes the copy of `Program Files` and puts the one of the user.

### The registration of the installation

- The key path of the component that registers the program is a value of
  `HKCU\Software\ckrattoz\MediaPilot`, so the state of the installation
  belongs to the user and not to the computer.
- The shortcut of the start menu and the entry of the installed programs
  belong to the user as well.

### The removal of the tree of folders

The files of a per-user installation live in the profile of the user, and the
uninstaller has to take the folders with them, including the folders of the
languages of the runtime, whose number changes with the version of .NET.

- `util:RemoveFolderEx` walks the installation folder when the program is
  uninstalled and adds to the `RemoveFile` table the rows that remove every
  file and every folder of it. The rows are written with the folders that
  exist at that moment, so the set of languages of the runtime does not have to
  be known when the package is built.
- The folder is read back from the registry
  (`HKCU\Software\ckrattoz\MediaPilot\InstallFolder`) when the program is
  uninstalled, because at that moment the property of the installation
  directory is not resolved anymore.
- The action only runs when the program is being removed (`REMOVE="ALL"`).

### The checks that are suppressed

- **ICE38** asks for the components that are installed in the profile of the
  user to have a key of `HKCU` as their key path, and not a file, because the
  state of a component whose key path is a file is registered for the whole
  computer. The published output is harvested as it is and every file becomes
  its own component, so there are more than four hundred of them, and giving
  each one a key of the registry would leave four hundred values behind. The
  check is suppressed with that limitation in mind: **two users of the same
  computer that install MediaPilot at the same time would share the
  registration of the files**, even though each one has its own copy, its own
  settings and its own hotkeys. It is the known cost of harvesting the output
  of a self-contained .NET application.
- **ICE64** asks for the directories that the installation creates in the
  profile of the user to be listed in the `RemoveFile` table, because a
  directory that is not in it can survive the uninstall when the profile
  travels to another computer. The action of the Util extension writes those
  rows when the program is uninstalled, with the folders that exist at that
  moment, so the check is satisfied at run time.
- **ICE91** warns that the files are installed in a directory of the profile,
  which does not vary with the `ALLUSERS` property. The documentation of the
  check says that the warning is harmless when the package is only installed in
  the per-user context.
- **ICE61** complains that the product removes a version equal to itself,
  which is exactly what `AllowSameVersionUpgrades` asks for.

## Consequences

- **Positive:** the installer does not ask for the elevation of an
  administrator. It runs with the rights of the user, without the prompt of
  the control of accounts.
- **Positive:** two users of the same computer can install MediaPilot and use
  their own copy, with their own settings and their own hotkeys.
- **Positive:** the program is installed and removed together with the profile
  of the user.
- **Negative:** the files live in the profile. If the user cleans the profile,
  the installation disappears with it. It is the behavior of every per-user
  program.
- **Negative:** ICE38 is suppressed, so the registration of the files is
  shared between the users of the same computer in the rare case that two of
  them install the program at the same time.
- **Accepted:** the upgrade from the per-machine 0.3.0 leaves the empty
  `C:\Program Files\MediaPilot` folder behind. The old package removes its
  files with the elevation of an administrator, but it does not have the rows
  that remove the folder, and the new package, which is per-user, cannot write
  in `Program Files` to remove it.

## Measurements

- The validation of the package reports zero warnings of the ICE validation and
  `msiexec /a` returns 0: the package extracts 425 files and 20 folders.
- The installation runs **with the privileges of the user**. The log of the
  installation says: `Running product '{7B736AF5-...}' with user privileges:
  It's not assigned.` No prompt of the control of accounts appears.
- The installation writes 424 files in `%LOCALAPPDATA%\Programs\MediaPilot`,
  the 17 folders of the languages of the runtime, the shortcut of the start
  menu and the value of the registry of the program.
- The uninstall closes the application, removes the tree of folders (the action
  `Wix4RemoveFoldersEx_X64` walks the installation folder and writes the rows
  of the `RemoveFile` table with the folders that exist at that moment), the
  shortcut, the folder of the start menu, the value of the registry and the
  entry of the installed programs. Nothing of the installation is left behind.

### The upgrade from the per-machine 0.3.0

The upgrade is **not automatic**, and it is Windows Installer that decides it:
`FindRelatedProducts: current install is per-user. Related install for product
'{942FBA1F-...}' is per-machine. Skipping...` The old copy has to be removed
first, and removing it needs the privileges of an administrator (the error
1730 of the installer says so). The two steps are therefore: remove the 0.3.0
with the elevation of an administrator, and install the 0.4.0 without it.

### A defect of the entry of start-up, not introduced here

The `RemoveRegistry` table of the package holds the row that removes the
`MediaPilot` value of the `Run` key, and it was believed that the uninstaller
removed the entry of the start-up with it. **It does not.**

The documentation of the `RemoveRegistryValues` action says that it removes a
value of the `RemoveRegistry` table when the component is set to be
**installed**, not when it is uninstalled. The table exists to clean up what a
previous installation left behind, not to remove what the program writes at
run time. The consequence is the opposite of what the row was meant to do:

- The entry of the start-up **survives the uninstall**, and Windows tries to
  run an executable that no longer exists at every logon.
- The entry of the start-up **is removed at every installation or repair**,
  which silently disables "Start with Windows" after every update.

The behavior is the same in the 0.3.0, so this release does not make it worse;
the claim of [ADR 0015](0015-installer-texts-and-uninstaller.md) about it is
corrected here. The owner of the removal of the entry of the start-up has to
be decided: either the installer writes it, so that Windows Installer can
remove it, and the application reconciles it with the setting when it starts,
or the entry is removed by a custom action of the installer.

