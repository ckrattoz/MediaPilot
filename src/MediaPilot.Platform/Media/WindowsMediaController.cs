using System.Runtime.InteropServices;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Platform.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaPilot.Platform.Media;

/// <summary>
/// Control over media playback.
/// </summary>
/// <remarks>
/// The playback commands are sent through the system keys, which is exactly
/// the mechanism of the original project: the system routes them to the
/// application that owns the media session.
/// </remarks>
public sealed class WindowsMediaController : IMediaController
{
    private readonly MediaSessionMonitor _sessionMonitor;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    /// <param name="sessionMonitor">Monitor of the media sessions of the system.</param>
    /// <param name="logger">Diagnostic log, optional.</param>
    public WindowsMediaController(MediaSessionMonitor sessionMonitor, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(sessionMonitor);
        _sessionMonitor = sessionMonitor;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public bool TryExecute(MediaCommand command)
    {
        var key = command switch
        {
            MediaCommand.Previous => VirtualKey.MediaPreviousTrack,
            MediaCommand.PlayPause => VirtualKey.MediaPlayPause,
            MediaCommand.Next => VirtualKey.MediaNextTrack,
            _ => VirtualKey.None,
        };

        if (key == VirtualKey.None)
        {
            return false;
        }

        var sent = KeyInputService.Send(key);

        if (!sent)
        {
            // The system rejected the event: the command cannot be executed
            // and the user would see nothing happening.
            _logger.LogWarning(
                "The key {Key} was not accepted by the system (Win32 error {Error}).",
                key,
                Marshal.GetLastWin32Error());
        }

        return sent;
    }

    /// <inheritdoc />
    public MediaTrack? GetCurrentTrack() => _sessionMonitor.CurrentTrack;
}
