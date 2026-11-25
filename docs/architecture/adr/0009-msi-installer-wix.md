# ADR 0009 - MSI installer built with the WiX Toolset

- **Status:** Accepted
- **Date:** 2026-11-21
- **Decides:** How MediaPilot is packaged and installed on the user machine.
- **Extended by:** [ADR 0015](0015-installer-texts-and-uninstaller.md), which
  decides the texts of the installer and what the uninstaller leaves behind.

## Context

Until now the only distributable artifact was the published folder. That is
enough to run the application, but it is not an installation: there is no
entry in the list of installed programs, no shortcut in the Start menu, and
removing the application means deleting a folder by hand.

MediaPilot installs files into `Program Files`, registers a component, and
offers an uninstaller. That is exactly what an MSI does, and the MSI format is
the one every version of Windows understands.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **MSI with the WiX Toolset** | The standard format of Windows; installed programs entry, Start menu shortcut, repair and uninstall; no runtime dependency on the target machine | It needs a build step that is separate from the solution |
| MSIX package | The format of the Microsoft Store; sandboxed and self updating | It requires a signing certificate that has to be obtained and renewed; sideloading needs the developer mode |
| ZIP with the published folder | Nothing else to build | It is not an installation: no shortcut, no entry in the installed programs, no uninstaller |
| NSIS or Inno Setup | Small and quick | Another toolchain, and the result is not the standard format of Windows |

## Decision

**Build an MSI with the WiX Toolset, as a separate project built by
`build/package.ps1`.**

- `installer/MediaPilot.Installer.wixproj` is a WiX project (`WixToolset.Sdk`
  5.0.2) with `installer/Package.wxs` as its source.
- The published output is harvested with the `<Files>` element, so every file
  becomes its own component and the installer can replace them one by one.
- The dialogs come from the `WixToolset.UI.wixext` extension
  (`WixUI_InstallDir`): the user chooses the installation folder.
- The package installs into `Program Files\MediaPilot`, creates a Start menu
  shortcut and registers itself so that Windows Installer can repair or
  uninstall it. `MajorUpgrade` with a fixed `UpgradeCode` allows upgrading
  from one version to the next.
- The version is read from `Directory.Build.props`, the same place the
  assembly reads it from, so the installer and the application never disagree.

### Why the WiX Toolset v5

WiX v6 and later require accepting the Open Source Maintenance Fee EULA.
That is a legal decision for the project owner, not for the build, so the
project stays on v5, the last version that does not require it. The v5
project is restored from NuGet as a package: **there is no software to install
on the build machine**.

### Why it is not part of the solution

The installer needs the published output of the application to exist before it
can be compiled. A project that depends on the output of another project does
not fit in the incremental build of the solution, so `build/package.ps1`
publishes the application first and then builds the installer.

## Consequences

- **Positive:** the user installs MediaPilot like any other Windows program,
  with a shortcut, an entry in the installed programs and a clean uninstall.
- **Positive:** the build machine needs no extra software: the toolset comes
  from NuGet.
- **Positive:** the MSI is x64 and self contained, so the target machine does
  not need .NET installed.
- **Negative:** the installer is not built by `dotnet build MediaPilot.slnx`
  and the continuous integration has to build it in its own job.
- **Negative:** the package is about 56 MB, because it carries the .NET
  runtime. That is the price of not requiring any prerequisite on the target
  machine.
- **Accepted:** the application does not register itself to start with Windows.
  The setting belongs to the application, which writes the `Run` key of the
  current user. Having the installer and the application write the same key
  would mean two sources of truth for the same setting. The installer does
  remove that entry when the program is uninstalled, so that it does not try
  to start an executable that is gone. See
  [ADR 0015](0015-installer-texts-and-uninstaller.md).
