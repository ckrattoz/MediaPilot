# MediaPilot architecture

## Core idea

MediaPilot is organized in layers with a one-way dependency rule. Each layer
depends on the previous one, never the other way around, and each one knows a
single platform type: the business logic does not know what Windows is, and the
interoperability layer does not know what WPF is.

```
                 ┌──────────────────────────────┐
                 │        MediaPilot.App        │
                 │  CompositionRoot · App.xaml  │
                 └───────┬──────────────┬───────┘
                         │              │
                 ┌───────▼──────┐  ┌────▼─────────────────┐
                 │ MediaPilot.UI│  │ MediaPilot.Platform  │
                 │  WPF · VM    │  │  Win32 · P/Invoke    │
                 └───────┬──────┘  └────┬─────────────────┘
                         │              │
                 ┌───────▼──────────────▼───────┐
                 │       MediaPilot.Core          │
                 │ domain · contracts · commands │
                 └───────────────┬────────────────┘
                                 │
                 ┌───────────────▼────────────────┐
                 │   MediaPilot.Configuration    │
                 │  JSON · paths · migrations     │
                 └────────────────────────────────┘

  MediaPilot.Localization (net10.0) holds the user interface strings and is
  referenced by Core, Platform, UI and App. It depends on nothing else.
```

| Assembly | Target framework | Responsibility |
| --- | --- | --- |
| `MediaPilot.Core` | `net10.0` | Domain model, service contracts and command orchestration. It has no third-party dependencies. |
| `MediaPilot.Configuration` | `net10.0` | Configuration persistence in JSON and local path resolution. |
| `MediaPilot.Localization` | `net10.0` | User interface strings and resolution of the UI culture. |
| `MediaPilot.Platform` | `net10.0-windows` | Win32 interoperability: tray, hotkeys, volume, theme, start with Windows, key sending. |
| `MediaPilot.UI` | `net10.0-windows` | Windows, style resources, view models and the notification presenter. |
| `MediaPilot.App` | `net10.0-windows` | Composition of all the pieces and the application lifecycle. |

`MediaPilot.Core` is deliberately platform-agnostic: if the domain needed to
know about Windows, the logic would be impossible to test without Windows.

## Contracts

Everything the interface or the composition needs from the system goes through
interfaces defined in `MediaPilot.Core/Contracts`:

| Contract | Responsibility | Implementation |
| --- | --- | --- |
| `IHotkeyService` | Register and unregister global hotkeys | `WindowsHotkeyService` |
| `IMediaController` | Control playback and query the track | `WindowsMediaController` |
| `IVolumeController` | Read and adjust volume and mute | `WindowsVolumeController` |
| `ISettingsStore` | Load and save the settings | `JsonSettingsStore` |
| `ITrayIcon` | Tray icon and its menu | `TrayIconService` |
| `ISystemTheme` | Detect the system theme and notify about changes | `WindowsThemeService` |
| `INotificationPresenter` | Show notifications to the user | `NotificationPresenter` |
| `IStartupService` | Enable or disable start with Windows | `WindowsStartupService` |

The practical benefit is that `MediaPilot.Core.Tests` runs without Windows:
the tests use test doubles and need neither hardware nor a graphical session.

## Command flow

```
Key pressed
     │
     ▼
Win32 sends WM_HOTKEY to the host window (0×0, hidden)
     │
     ▼
HwndSource.AddHook → CompositionRoot.OnWindowMessage
     │
     ▼
NativeMessageRouter.Dispatch  ──►  WindowsHotkeyService.CommandTriggered
     │
     ▼
MediaCommandExecutor.Execute(MediaCommand)
     │
     ├── MediaCommand.Previous / PlayPause / Next
     │        └──► IMediaController.TryExecute
     │
     ├── VolumeUp / VolumeDown
     │        └──► IVolumeController.TryIncreaseVolume / TryDecreaseVolume
     │             └──► 350 ms timer ──► INotificationPresenter.ShowVolume
     │
     ├── VolumeMute
     │        └──► IVolumeController.TrySetMuted(!current)
     │             └──► 350 ms timer ──► INotificationPresenter.ShowVolume
     │
     └── ShowInformation
              └──► INotificationPresenter.ShowMessage
```

The 350 ms delay before the volume notification is the behavior inherited from
the original project: while the key is held down, no notification is emitted
for each repetition.

## Threading model

- **UI thread**: receives the Win32 messages and updates the controls.
- **Delay timer** (`System.Threading.Timer`): fires the volume notification off
  the UI thread. `MediaCommandExecutor` keeps the pending state
  (`_pendingVolume`, `_pendingMuted`, `_pendingIncreased`) and clears it when
  the timer expires.
- **No global keyboard hook**: `RegisterHotKey` delivers the hotkeys to the
  system, not to the process. At rest, the application receives no input
  messages: this is why the idle consumption is low.

## Composition

There is no dependency injection container. `CompositionRoot` builds each
service explicitly and wires the events:

```csharp
_hotkeys = new WindowsHotkeyService(_messageWindow.Handle, _router);
_tray = new TrayIconService(_messageWindow.Handle, _router, iconoActivo, iconoInactivo);

_hotkeys.CommandTriggered += (_, command) => _executor.Execute(command);
_tray.OpenRequested += (_, _) => ShowSettings(settingsStore);
_tray.ExitRequested += (_, _) => _application.Shutdown();
```

The reasoning is in [ADR 0007](adr/0007-manual-composition-root.md).

## Theme

`ThemeManager` swaps two resource dictionaries (`Palette.Light.xaml` and
`Palette.Dark.xaml`) in `Application.Current.Resources`. The palette is the
application's own instead of using the framework's Fluent theme keys, so as
not to depend on resource names that Microsoft may change between versions.
The full reasoning is in [ADR 0002](adr/0002-explicit-theme-resolution.md).

## Message loop

The application has no main window. `CompositionRoot` creates a 0×0
`HwndSource` and hides it with `ShowWindow(SW_HIDE)`; that window is the one
that receives `WM_HOTKEY`, the tray icon messages and the system theme change
notification (`WM_SETTINGCHANGE`).

`ShutdownMode` is `OnExplicitShutdown`: the application only ends when the
user chooses *Exit* in the tray menu.

## Decision records

Every choice with discarded alternatives has its ADR:

| ADR | Topic |
| --- | --- |
| [0001](adr/0001-dotnet-and-wpf-stack.md) | C# + .NET 10 + WPF with the native Fluent theme |
| [0002](adr/0002-explicit-theme-resolution.md) | Explicit theme resolution and own palette |
| [0003](adr/0003-native-tray-icon.md) | Tray with `Shell_NotifyIcon` instead of WinForms |
| [0004](adr/0004-mmdevice-volume-control.md) | Volume with MMDevice instead of the legacy mixer |
| [0005](adr/0005-registerhotkey-global-hotkeys.md) | `RegisterHotKey` instead of a global hook |
| [0006](adr/0006-json-settings-store.md) | JSON configuration instead of the registry |
| [0007](adr/0007-manual-composition-root.md) | Manual composition instead of a dependency injection container |
