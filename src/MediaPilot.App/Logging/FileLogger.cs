using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MediaPilot.App.Logging;

/// <summary>
/// File based diagnostic log, with daily rotation.
/// </summary>
/// <remarks>
/// Own implementation to avoid adding a logging provider dependency: the
/// application only needs to write one line per event.
/// </remarks>
public sealed class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly Lock _sync = new();

    /// <summary>
    /// Initializes the logger.
    /// </summary>
    /// <param name="filePath">Path of the log file.</param>
    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var line = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] {formatter(state, exception)}");

        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (_sync)
        {
            try
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // The log must never prevent the application from working.
            }
            catch (UnauthorizedAccessException)
            {
                // Same: no permissions over the log folder.
            }
        }
    }
}

/// <summary>
/// Provider that creates file loggers for the application.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="directory">Folder where the logs are written.</param>
    public FileLoggerProvider(string directory)
    {
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, $"mediapilot-{DateTime.Now:yyyyMMdd}.log");
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath);

    /// <inheritdoc />
    public void Dispose()
    {
        // There are no resources to release.
    }
}
