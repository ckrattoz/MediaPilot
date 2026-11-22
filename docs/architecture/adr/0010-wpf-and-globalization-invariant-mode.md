# ADR 0010 - WPF needs real cultures and a safe tray menu

- **Status:** Accepted
- **Date:** 2026-11-21
- **Decides:** How the runtime is configured and how the tray menu is displayed.

## Context

The first versions of MediaPilot could run in the notification area, answer the
global shortcuts and show the overlay with the volume, but **every window of
the application died as soon as it was opened**. Choosing "Open settings" or
"About" from the tray menu terminated the process:

```
System.Windows.Markup.XamlParseException: Cannot find non-neutral culture related to 'en-us'.
 ---> System.InvalidOperationException: Cannot find non-neutral culture related to 'en-us'.
   at System.Windows.Markup.XmlLanguage.GetSpecificCulture()
   ...
   at MediaPilot.App.CompositionRoot.ShowSettings(...)

System.TypeInitializationException: The type initializer for 'MS.Internal.FontCache.MajorLanguages' threw an exception.
 ---> System.Globalization.CultureNotFoundException: Only the invariant culture is supported in globalization-invariant mode.
   at System.Globalization.CultureInfo..ctor(String name, Boolean useUserOverride)
   at MS.Internal.FontCache.MajorLanguages..cctor()
```

The cause was `Directory.Build.props`:

```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

The invariant mode leaves the runtime without cultures: `new CultureInfo("en")`
throws, and WPF needs real cultures in two different places.

- WPF uses `en-us` as the default language of every element. As soon as a
  template is applied, `XmlLanguage.GetSpecificCulture()` looks for that culture
  and fails.
- The font cache builds the set of "major languages" to decide the typography
  features of a font. It creates the `en` culture in its static constructor and
  fails.

The defect was invisible for a long time because the tray menu had never been
opened: the rest of the application lives in the notification area and the
overlay is a simple window whose text is measured through a different path.

The symptom reported by the user was that the computer froze when the context
menu was used and that the application ended up closing. The freeze comes from
the native modal loop of the popup menu: the menu is dismissed by a null
message posted to its owner window, and without that message the menu stays on
screen when the user clicks outside of it. The closing comes from the crash
above.

A third defect made the failure worse: the menu commands were executed inside
the window procedure, that is, inside a native callback. An exception that
escapes a callback does not unwind to the managed code: the process is
terminated with `STATUS_FATAL_USER_CALLBACK_EXCEPTION` (0xc000041d) and there is
no chance to recover.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Real cultures + safe menu** | WPF works as documented; the errors are logged instead of killing the process | The runtime needs the globalization data of the system |
| Keep the invariant mode and avoid the cultures in the interface | Smallest package | WPF cannot lay out text: the application dies on any window |
| Replace WPF with WinUI or a native toolkit | Full control over the rendering | A rewrite of the whole user interface for a defect that has a one-line fix |
| Disable the offending WPF features | The application would start | The interface would be empty or in English only, and the defect would stay hidden |

## Decision

**Run the runtime with real cultures and make the tray menu unable to take the
application down.**

### Globalization

- `Directory.Build.props` sets `InvariantGlobalization` to `false` explicitly,
  with a comment that explains why. The property is left in the file on
  purpose: it is the place where somebody would try to shrink the package
  again, and the comment tells them why that is not possible.
- On Windows the globalization data comes from the operating system, so the
  published package does not grow.
- `GlobalizationTests` fails if the invariant mode is enabled again, before the
  defect reaches the user interface.
- The format provider of the localized messages is now
  `CultureInfo.CurrentCulture` instead of `CultureInfo.CurrentUICulture`. The
  text of the message is already chosen by the resource manager with the
  interface culture; the culture that formats the values is the regional one.
  The analyzer CA1305 requires this distinction.

### The tray menu

- The popup menu is dismissed with `PostMessage(WM_NULL)` to the owner window
  right after `TrackPopupMenuEx` returns. This is the documented way to tell
  Windows that the menu is over, and it is what makes the menu close when the
  user clicks outside of it.
- The message of the icon is always consumed. The default window procedure of
  the hidden host window has nothing to do with the notification icon.
- The commands are not executed inside the window procedure. The tray service
  raises the events and the application schedules the work on the dispatcher,
  where an error is logged instead of terminating the process.
- Every call of the tray service that runs inside the window procedure is
  guarded: an unexpected error is written to the log and the application keeps
  running.
- `DispatcherUnhandledException` is handled in the same way: an error in a
  window must not make the icon disappear from the notification area and leave
  the user without the shortcuts.

### The windows

- The settings window and the about window are reused. Opening them twice from
  the tray menu leaves a single window on screen.

## Consequences

- **Positive:** the windows of the application open normally.
- **Positive:** a failure in the user interface is logged and does not end the
  session of the application.
- **Positive:** the menu closes when the user clicks outside of it.
- **Negative:** the analyzer CA1305 forced the format provider of the messages
  to change, which touches every place that formats a localized string.
- **Accepted:** the popup menu blocks the user interface thread while it is
  open, because it is a native modal loop. The application has no visible
  window while that happens, so there is nothing that the user perceives as
  frozen.
