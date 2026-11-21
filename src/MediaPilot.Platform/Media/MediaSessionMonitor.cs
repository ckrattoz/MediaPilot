using System;
using System.Threading;
using System.Threading.Tasks;
using MediaPilot.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Windows.Media.Control;

namespace MediaPilot.Platform.Media;

/// <summary>
/// Watches the media sessions of the system and keeps the track being played.
/// </summary>
/// <remarks>
/// <para>
/// The media session API (SMTC, <c>Windows.Media.Control</c>) is the supported
/// way of knowing what is playing: the original AutoHotkey script read the
/// title of the foreground window, which applied to any window of the system
/// and was therefore unreliable.
/// </para>
/// <para>
/// The API is asynchronous and event driven, but the domain is synchronous, so
/// the monitor keeps the last known track in a field and the rest of the
/// application reads that snapshot. Every failure is swallowed: a media
/// session that cannot be reached must never stop the application from
/// working.
/// </para>
/// </remarks>
public sealed class MediaSessionMonitor : IDisposable
{
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _startGate = new(1, 1);

    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private volatile MediaTrack? _currentTrack;
    private volatile bool _hasActiveSession;
    private int _disposed;

    /// <summary>
    /// Initializes the monitor.
    /// </summary>
    /// <param name="logger">Diagnostic log, optional.</param>
    public MediaSessionMonitor(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets the track that is being played, or <see langword="null"/> when
    /// there is none.
    /// </summary>
    public MediaTrack? CurrentTrack => _currentTrack;

    /// <summary>
    /// Gets a value indicating whether the system reports an active media
    /// session, even when it publishes no track information.
    /// </summary>
    public bool HasActiveSession => _hasActiveSession;

    /// <summary>
    /// Connects to the media session manager and starts watching the sessions.
    /// </summary>
    /// <remarks>
    /// It never throws: when the media session API is not available the
    /// application keeps working without track information.
    /// </remarks>
    public async Task StartAsync()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        await _startGate.WaitAsync().ConfigureAwait(true);

        try
        {
            if (_manager is not null)
            {
                return;
            }

            _manager = await GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync()
                .AsTask()
                .ConfigureAwait(true);

            _manager.CurrentSessionChanged += OnCurrentSessionChanged;
            _manager.SessionsChanged += OnSessionsChanged;

            await RefreshAsync().ConfigureAwait(true);

            _logger.LogInformation("Media session monitor connected.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The media session API is not available; the track information is disabled.");
        }
        finally
        {
            _startGate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (_manager is not null)
        {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
            _manager.SessionsChanged -= OnSessionsChanged;
        }

        DetachSession();

        _manager = null;
        _startGate.Dispose();

        GC.SuppressFinalize(this);
    }

    private void OnCurrentSessionChanged(object? sender, CurrentSessionChangedEventArgs args) => QueueRefresh();

    private void OnSessionsChanged(object? sender, SessionsChangedEventArgs args) => QueueRefresh();

    private void OnMediaPropertiesChanged(object? sender, MediaPropertiesChangedEventArgs args) => QueueRefresh();

    private void QueueRefresh()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        // The event handler cannot be awaited, so the refresh runs on the
        // thread pool and never propagates an exception to the caller.
        _ = Task.Run(RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        var manager = _manager;

        if (manager is null || Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            var session = ResolveSession(manager);

            _hasActiveSession = session is not null;

            if (session is null)
            {
                DetachSession();
                _currentTrack = null;
                return;
            }

            AttachSession(session);

            var properties = await session.TryGetMediaPropertiesAsync().AsTask().ConfigureAwait(false);

            var title = properties?.Title ?? string.Empty;
            var artist = properties?.Artist ?? string.Empty;

            _currentTrack = new MediaTrack(title, artist);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The media session could not be read.");
        }
    }

    private static GlobalSystemMediaTransportControlsSession? ResolveSession(
        GlobalSystemMediaTransportControlsSessionManager manager)
    {
        var current = manager.GetCurrentSession();
        if (current is not null)
        {
            return current;
        }

        var sessions = manager.GetSessions();

        return sessions is { Count: > 0 } ? sessions[0] : null;
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession session)
    {
        if (ReferenceEquals(_session, session))
        {
            return;
        }

        DetachSession();

        _session = session;
        _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
    }

    private void DetachSession()
    {
        if (_session is null)
        {
            return;
        }

        _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _session = null;
    }
}
