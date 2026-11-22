using System;
using System.Windows;
using System.Windows.Threading;
using MediaPilot.Localization;
using Microsoft.Extensions.Logging;

namespace MediaPilot.App;

/// <summary>
/// Entry point of the application.
/// </summary>
/// <remarks>
/// There is no main window: MediaPilot lives in the notification area, so
/// <see cref="Application.ShutdownMode"/> is explicit and the application only
/// ends when the user decides it from the tray menu.
/// </remarks>
public partial class App : Application, IDisposable
{
    private CompositionRoot? _composition;
    private ILogger? _logger;
    private bool _disposed;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The user interface culture is resolved before any window is built,
        // because XAML resolves its strings while the visual tree is created.
        CultureProvider.Apply();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _composition = new CompositionRoot(this);
        _composition.Start();

        // A dedicated reference to the logger is kept: after shutdown the
        // logger factory is disposed and must not be used to write anymore.
        _logger = _composition.Log;
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Releases the composition root and the native services it registers.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources that are not managed by the collector.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when the call comes from <see cref="Dispose()"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _composition?.Dispose();
        }

        _disposed = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger?.LogCritical(exception, "Unhandled error.");
        }
    }

    /// <summary>
    /// Handles the errors raised by the user interface.
    /// </summary>
    /// <remarks>
    /// A failure in a window must not terminate the application: the icon
    /// would disappear from the notification area and the shortcuts would stop
    /// working. The error is written to the log and the application keeps
    /// running.
    /// </remarks>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.Exception, "Unhandled error in the user interface.");
        e.Handled = true;
    }
}
