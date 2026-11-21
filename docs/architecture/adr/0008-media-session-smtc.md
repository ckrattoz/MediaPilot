# ADR 0008 - Media session API (SMTC) for the track information

- **Status:** Accepted
- **Date:** 2026-11-21
- **Decides:** How MediaPilot learns which track is being played.

## Context

The original AutoHotkey script showed the information of the track with
`WinGetTitle`, which reads the title of the **foreground window**. That method
is fragile for two reasons:

- It applies to any window of the system, not only to the media player. The
  title of a browser tab or a text editor was shown as if it were the track.
- The window title is not the track title. It is often the file name, or the
  name of the application with a suffix.

Windows exposes a supported API for this: the media session API, also known as
SMTC (`Windows.Media.Control`). Every application that plays audio publishes a
media session with its title, artist, album and playback state, and the system
routes the multimedia keys to the session that owns them.

## Considered alternatives

| Option | Pros | Cons |
| --- | --- | --- |
| Title of the foreground window | Simple, no dependency | Wrong for any window that is not a player; it is not the track title |
| **Media session API (SMTC)** | The supported API: real title, artist and album; works with every player that publishes a session | It is a WinRT API: it needs the Windows SDK projections and it is asynchronous |
| Query the player application directly | Exact data | Every player has a different interface; it does not scale |
| Simulate the keys and read the OSD | No API needed | The overlay only shows what the system chooses; it cannot be read |

## Decision

**Use the media session API through the Windows SDK projections, and keep the
result in a snapshot owned by the platform layer.**

- `MediaPilot.Platform/Media/MediaSessionMonitor.cs` owns the
  `GlobalSystemMediaTransportControlsSessionManager`. It subscribes to
  `CurrentSessionChanged`, `SessionsChanged` and `MediaPropertiesChanged`,
  and keeps the last known track in a field.
- The domain is synchronous, so `MediaCommandExecutor` reads that snapshot
  through `IMediaController.GetCurrentTrack()`. The WinRT types never leave
  the platform layer: the domain only sees `MediaTrack`, a record with the
  title and the artist.
- The connection is asynchronous (`RequestAsync` is a WinRT call that cannot
  block the user interface thread). `CompositionRoot` starts it without
  waiting, and the application works without track information until it
  answers.
- Every failure is swallowed and logged: a media session that cannot be
  reached must never stop the application from working.

### Target framework

The projections of the Windows SDK are only available with the Windows specific
target framework, so `Platform`, `UI`, `App` and `Platform.Tests` moved from
`net10.0-windows` to `net10.0-windows10.0.19041.0`
(`SupportedOSPlatformVersion` 10.0.19041.0). The application already
required Windows 11 22H2, which is above that version, so nothing is lost.

## Consequences

- **Positive:** the information command shows the real title and artist of the
  track, for every player that publishes a media session, including the
  browser, Spotify or the movies and TV application.
- **Positive:** the track is known before the user presses the key, because the
  monitor follows the session events.
- **Negative:** the Windows SDK projections become a dependency of the
  platform layer, and the target framework of four projects changes.
- **Negative:** the API is asynchronous and event driven, so the platform layer
  has to keep a snapshot and the domain stays synchronous.
- **Mitigation:** the WinRT types are confined to
  `MediaPilot.Platform/Media/MediaSessionMonitor.cs`. The rest of the
  application only knows `MediaTrack`.

## Verification

The behavior was measured on the development machine with a real media
session, created with `Windows.Media.Playback.MediaPlayer` publishing a known
title and artist:

- The session manager reports the session and its properties.
- `MediaSessionMonitor.CurrentTrack` returns the title and the artist, and
  keeps them up to date when the session changes.
- The application shows `Titulo de prueba - Artista de prueba` in the
  notification overlay when the information hotkey is pressed.

The test is not part of the automated suite on purpose: it needs a real media
session, and a build agent has none.
