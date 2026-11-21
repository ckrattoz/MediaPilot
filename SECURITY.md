# Security policy

## Supported versions

| Version | Supported |
| --- | --- |
| 0.1.x | Yes |

The project does not have a published version yet, so **all** the versions are
in development and receive fixes.

## How to report a vulnerability

**Do not open a public issue.** Publishing the details of a vulnerability gives
the opportunity to fix it before it can be exploited.

Instead, report it privately:

1. Open a private *Security Advisory* in this repository
   (**Security → Report a vulnerability** tab).
2. If you do not have access to that channel, write to the repository owner
   with the subject `security`.

Include, as far as possible:

- Which component and version are affected.
- Steps to reproduce it.
- The impact you have observed.
- Any screenshot or log that helps.

Receipt is confirmed within a reasonable time and the reporter is kept informed
during both the investigation and the publication of the fix.

## Scope

MediaPilot is a local desktop application that:

- Runs with the permissions of the user who launches it.
- Does not open network ports or make outgoing connections.
- Writes only inside `%LOCALAPPDATA%\MediaPilot` and in the registry key
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (only if the user
  enables start with Windows).
- Does not collect telemetry data.

Therefore, the relevant attack surfaces are: the deserialization of the
configuration file, the global hotkey registration, the tray icon handler and
the start with Windows configuration.

## What counts as a vulnerability

- Local privilege escalation through the configuration files or the registry.
- Code execution by controlling the content of `settings.json`.
- Race condition that allows registering malicious hotkeys.
- Disclosure of sensitive information to other processes of the machine.

The following cases **do not** count as vulnerabilities:

- Another application of the same user being able to read or write in
  `%LOCALAPPDATA%`.
- The binary registered for start with Windows being replaced by another
  program. In that case the problem belongs to the file system, not to
  MediaPilot.
- Denial of service by force closing the application.

## Responsible disclosure

The project follows the responsible disclosure model: a reasonable period is
granted to publish the fix before the technical details are published.
