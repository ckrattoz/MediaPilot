# ADR 0007 - Manual composition instead of a dependency injection container

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** How the application services are built and wired together.

## Context

`MediaPilot.App` needs to wire up eight services with very different
dependencies: the tray, the hotkeys, the volume, the theme, the settings, the
notifications, and the auto-start. The question is whether that composition is
done with a dependency injection container or by hand.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| **Manual composition in `CompositionRoot`** | Zero dependencies, zero configuration, one file that reads top to bottom, and an obvious startup flow | The wiring has to be maintained by hand as the number of services grows |
| `Microsoft.Extensions.DependencyInjection` | Standard container, automatic resolution, easy testing | One more dependency (`Microsoft.Extensions.DependencyInjection.Abstractions` and its implementation), lifetimes that have to be configured correctly, and more ceremony for eight services |
| Third-party container (Autofac, SimpleInjector, ...) | Advanced features | Disproportionate external dependency |

## Decision

**Manual composition in `MediaPilot.App/CompositionRoot.cs`.**

The pattern is deliberately the inverse of the usual WPF one:

```csharp
_tray.OpenRequested  += (_, _) => ShowSettings(settingsStore);
_tray.AboutRequested += (_, _) => ShowAbout();
_tray.ExitRequested  += (_, _) => _application.Shutdown();
```

The application has **no** `MainWindow` and no `ViewModelLocator`: `App`
creates `CompositionRoot`, which builds everything and hooks the events. A
container adds nothing when there is neither type-based nor scoped resolution:
here each service is constructed with explicit arguments, exactly once.

Furthermore, the contracts live in `MediaPilot.Core/Contracts`, so the domain is
already decoupled: `MediaCommandExecutor` depends on `IMediaController`,
`IVolumeController`, and `INotificationPresenter`, and the tests replace them
with doubles. The proof that the architecture works is already written and
passes; a container would only automate something that is already explicit.

## Consequences

- **Positive:** no injection dependency; the application startup reads in a
  single file and in a single direction; there are no lifetimes to configure
  and no singletons to register.
- **Negative:** if the number of services grew substantially, `CompositionRoot`
  would get long. A refactor to a container, if that day comes, is local: it
  only affects that file.
- **Note on the diagnostic log:** `Microsoft.Extensions.Logging` is used, but
  only through its **`ILogger` interface** and `LoggerFactory`. The log
  provider is the application's own (`FileLoggerProvider`), with no extra
  dependencies, because the application only needs to write one line per
  event.

## Criteria to revisit it

This ADR would be revisited if any of these conditions were met:

- More than fifteen services with dependencies between them.
- A need for a per-session or per-request scope (there is no scope today other
  than the application itself).
- A need to resolve implementations by configuration at runtime.

None of them is true today.
