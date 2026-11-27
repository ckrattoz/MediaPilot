# ADR 0019 - One instance, and a notice that explains where the program is

- **Status:** Accepted
- **Date:** 2026-11-26
- **Decides:** What happens when MediaPilot starts twice, and how the user is
  told that the program is already running.
- **Extends:** [ADR 0003](0003-native-tray-icon.md), which decided how the icon
  of the notification area is built, and
  [ADR 0015](0015-installer-texts-and-uninstaller.md), which decided how the
  installer closes the application.

## Context

MediaPilot has no window of its own. It lives in the notification area and it
does not open anything when it starts, which is right for a program that is
always there and wrong the first time, for two reasons:

- **After the installation, nothing happens.** The installer finishes, the
  program starts and there is no window anywhere. The icon of the notification
  area is small, and Windows hides it behind the chevron when there are many
  icons, so the user is left looking at a program that seems to have failed.
- **A second copy breaks the first one.** The global hotkeys are registered by
  the system, and only one application can own a combination. A second copy of
  MediaPilot registers the same seven, fails, and shows a second icon that
  does not work.

The original AutoHotkey project had neither problem: the script showed a
splash window at the start and the hotkeys of the second copy failed silently.

## Considered alternatives

### How to tell the user that the program is running

| Option | Pros | Cons |
| --- | --- | --- |
| Show a window on the first run | It is the usual way to introduce a program | It interrupts, it is a new surface to design, and the program has no window by design |
| Toast of the system | It is native and it is familiar | The overlay is the visual language of the product; a toast carries the icon of another program and it disappears before it is read |
| Balloon of the notification area | It is the mechanism that the tray already has | It is disabled in many systems, it is the old way, and the program does not use it for anything else |
| Say nothing | There is nothing to build | The program looks broken after the installation |
| Show the notice on every start | It is always visible | It becomes noise, and the user reads it once |
| **Show the notice once, the first time** | It answers exactly the question it raises, and only once | It needs a flag that has to be stored, and it disappears when the settings file is deleted |

### How to keep a single instance

| Option | Pros | Cons |
| --- | --- | --- |
| Let every copy run | There is nothing to build | They compete for the hotkeys and the user finds two icons |
| A named mutex | It is the standard mechanism of Windows, it needs no dependency and it dies with the process | The ownership of a mutex belongs to the thread that took it |
| A named pipe or a socket | It can carry a message with the connection | Two mechanisms for one thing: the mutex decides and the pipe notifies |
| **A named mutex, and the second copy tells the running one** | One mechanism decides and the same message window that already exists carries the notice | The window of the running instance has to be found from another process |

## Decision

### Only one instance per session

The application takes a named mutex, `Local\MediaPilot.SingleInstance`, before
it builds anything else. The prefix `Local\` binds the mutex to the session of
the user and not to the machine, so two users of the same computer do not
interfere with each other.

The mutex is owned by the thread that took it, and the thread that takes it is
the one that releases it: the composition root takes it when it starts and
releases it when it is disposed. If the process dies without releasing it, the
next instance finds it abandoned and takes it over, so a crash never leaves the
program unable to start.

The check is the **first** thing the composition root does. A second copy that
arrives while the first one is still starting would otherwise register the
hotkeys of the user before finding out that it is not the only one.

### The second copy tells the running one and leaves

The second instance cannot take the mutex, so it sends a message to the hidden
window of the running one (`WM_APP + 2`, which does not collide with the
message of the icon, `WM_APP + 1`) and ends with the code 0. The running one
shows a notice that says that MediaPilot is already running, and the copy that
just started is gone.

The window is looked for **by its title**. The hidden window of the host does
not have a class of its own: WPF gives it the name of its wrapper, which
changes with every build, so the class cannot be used to find it.

### The notice of the first run

The notice is the overlay of the product, the same window that shows the
volume, with the mark of the application, the title and the message. It
appears **two seconds after the start**, so that it does not compete with the
notification of a shortcut that the user presses right after the installation,
and it stays **eight seconds** on screen, longer than the notices that only
inform about a change.

The flag that remembers it is `AppSettings.OnboardingCompleted`. It is written
with the rest of the settings when the notice is shown, so the notice appears
once and only once. If the user deletes `settings.json`, the notice appears
again: the file is the state of the program, and a program without state is a
program that has just been installed.

### The installer offers to start the program

The final dialog of the wizard shows a check box, **Lanzar MediaPilot**, and a
note that explains that the program stays in the notification area and does not
open any window. The check box starts marked: after an installation the
natural thing is to try the program that was just installed.

The program starts when the user closes the dialog with **Finalizar**, never
while the installer is still on screen. The extension of the interface shows
the check box only when the product was not installed before, so the option
belongs to a first installation and not to an update.

## Consequences

- **Positive:** the installation is not a silent event. The user sees the
  program and knows where it is.
- **Positive:** a second copy of the program cannot break the hotkeys of the
  first one.
- **Positive:** the notice and the check box of the installer use the visual
  language of the product, so there is nothing new to design.
- **Negative:** the notice needs a flag in the settings, and every place that
  rebuilds `AppSettings` has to carry it over. There are five of them.
- **Negative:** the flag of the first run is lost when the user deletes the
  settings file, and the notice appears again.
- **Accepted:** the notice is not shown on every start. A user who wants to
  see it again can delete the settings file.
- **Accepted:** the program does not start with Windows unless the user asks
  for it. The notice is the way of saying that it is there, not a way of
  keeping it running.

## Measurements

- The whole cycle was verified on the machine of the author, with the
  installer and the application:
  - the final dialog shows the check box **marked** (`BM_GETCHECK` returns 1)
    and the note;
  - pressing **Finalizar** starts MediaPilot (the process appears and the
    settings file is written);
  - the notice of the first run is on screen with the title *MediaPilot está
    listo*, and `settings.json` has `OnboardingCompleted` in `true`;
  - the second start does not show the notice and does not write the settings
    file again;
  - the second instance ends **0.9 seconds** after it starts, with the code 0,
    and the running one shows *MediaPilot ya está en marcha*.
- Three facts about Windows Installer cost a rebuild each one and are worth
  remembering:
  - **the final dialog is a termination action.** Its sequence number is
    negative and nothing can be scheduled before or after it: the compiler
    rejects it with `WIX0177`. The launch is a **control event** on the
    *Finish* button (`DoAction`), with the order 1000 and 1001, so that it
    runs after the event that closes the dialog, which is the 999 of the
    extension.
  - **the property of a type 50 action holds the path of the executable, and
    the target holds the line of commands.** The path must not be quoted: the
    installer uses it as the name of the program to start, and
    `CreateProcess` fails with `ERROR_CREATE_FAILED` (1631) when the quotes
    are part of it.
  - **the text of a property cannot be the value of another property.** The
    texts of the check box and of the note are written with `!(loc.Id)`, which
    is resolved at build time against the localization file of the package.
