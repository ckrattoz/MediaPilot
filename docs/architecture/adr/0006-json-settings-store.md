# ADR 0006 - JSON settings instead of the Windows registry

- **Status:** Accepted
- **Date:** 2026-11-20
- **Decides:** Where and how the application settings are stored.

## Context

The original project stored its settings in two `REG_DWORD` values in the
Windows registry. That approach has several problems: it is not readable, it
cannot be versioned or diffed, it does not support structures (a list of hotkeys
mapped to commands does not fit in two `DWORD` values), and the registry is not
a natural place for a user to review or edit their settings.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| `REG_DWORD` under `HKCU` (as the original) | Consistent with the system | Opaque, structureless, hard to migrate, pollutes the registry |
| **JSON file under `%LOCALAPPDATA%`** | Readable, versionable, migratable, hand-editable, trivial to back up | Has to handle a corrupted file and two instances writing at once |
| XML file under `AppData` | Native .NET format | More verbose and less convenient to read by hand than JSON |
| Local database | Very powerful queries | Disproportionate for a handful of settings |
| Cloud settings | Sync across machines | Complexity, cost, and privacy concerns |

## Decision

**JSON file at `%LOCALAPPDATA%\MediaPilot\settings.json`.**

- `AppPaths` centralizes the paths: the root folder, the settings file, and
  the logs folder.
- `JsonSettingsStore` implements `ISettingsStore` and uses `System.Text.Json`
  with **compile-time source-generated serialization** (`SettingsJsonContext`),
  which avoids runtime reflection and keeps the serialization metadata alive
  under trimming.
- Enums are serialized by name (`"Key": "Left"`, `"Modifiers": "Alt,
  Control"`), not by numeric value: the file stays readable and editable, and a
  change to the internal enum values does not invalidate saved settings.
- `AppSettings.SchemaVersion` enables schema migration. `JsonSettingsStore.Migrate`
  applies the migrations on read; the user's file is not rewritten unless
  something changed.

## Behavior on errors

The store is **fail-open**:

- If the file does not exist -> the default values are returned.
- If the file is corrupted or unreadable -> the warning is logged and the
  default values are returned. The application still starts.
- If it cannot be written -> the exception propagates to the caller, which
  decides: it is better for the user to know that the change was not saved.

## Consequences

- **Positive:** the settings can be reviewed and edited with any text editor;
  they can be included in a backup; the schema evolves without touching the
  Windows registry; the Git diff is readable.
- **Negative:** a corrupted file has to be handled (resolved by the fail-open
  behavior) and concurrent writes from two instances have to be considered
  (Windows serializes writes atomically for small files, and the application is
  single-instance by design).
- **Note:** `%LOCALAPPDATA%` is the right location: it requires no administrator
  rights, it is not synced across machines (which is desirable: hotkeys are a
  local preference), and it does not travel in the profile backup, where
  roaming data ends up.

## Alternative for the future

If the settings ever need to be synced across machines, a JSON file is still
the right foundation: only the sync layer would have to be added, without
changing the format.
