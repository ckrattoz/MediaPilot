using System;
using System.Threading;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Instance;

/// <summary>
/// Keeps a single instance of the application running per session.
/// </summary>
/// <remarks>
/// MediaPilot lives in the notification area and registers global hotkeys: a
/// second copy of it would compete for the same shortcuts, would show a second
/// icon and would leave the user with two copies of the same program.
///
/// The guard takes a named mutex that only exists while the process is alive.
/// The second instance cannot take it, so it asks the running one to show that
/// it is alive and leaves.
///
/// The mutex is named with the <c>Local\</c> prefix, so it belongs to the
/// session of the user and not to the machine: two different users of the same
/// computer do not interfere with each other.
/// </remarks>
public sealed class SingleInstanceGuard : IDisposable
{
    /// <summary>Name of the mutex that represents the running instance.</summary>
    public const string DefaultMutexName = @"Local\MediaPilot.SingleInstance";

    /// <summary>
    /// Message that tells the running instance that another one was started.
    /// </summary>
    /// <remarks>
    /// It belongs to the range of <c>WM_APP</c> and it does not collide with
    /// the message of the icon in the notification area, which is
    /// <c>WM_APP + 1</c>.
    /// </remarks>
    public const int WmAppSecondInstance = 0x8002;

    /// <summary>Title of the hidden window of the application.</summary>
    /// <remarks>
    /// The window that receives the messages of the host does not have a
    /// class of its own: WPF gives it the name of its wrapper, which
    /// changes with every build. Its title is the one that is written in
    /// the source, so it is the one that can be looked for.
    /// </remarks>
    public const string HostWindowTitle = "MediaPilot.Host";

    private const int HostWindowLookupAttempts = 5;
    private const int HostWindowLookupDelayMilliseconds = 100;

    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>
    /// Tries to become the only running instance of the application.
    /// </summary>
    /// <param name="mutexName">Name of the mutex that represents the instance.</param>
    /// <returns>
    /// The guard when this process is the only instance, or
    /// <see langword="null"/> when another instance already holds it.
    /// </returns>
    public static SingleInstanceGuard? TryAcquire(string mutexName = DefaultMutexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);

        var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);

        if (createdNew)
        {
            return new SingleInstanceGuard(mutex);
        }

        bool owned;

        try
        {
            // The mutex already exists: another instance created it. Taking
            // it without waiting tells whether that instance is still alive.
            owned = mutex.WaitOne(TimeSpan.Zero, exitContext: false);
        }
        catch (AbandonedMutexException)
        {
            // The previous instance died without releasing the mutex. This
            // process owns it now, and there is nothing left running.
            owned = true;
        }

        if (!owned)
        {
            mutex.Dispose();
            return null;
        }

        return new SingleInstanceGuard(mutex);
    }

    /// <summary>
    /// Tells the running instance that another one was started.
    /// </summary>
    /// <remarks>
    /// The notice is a courtesy: when the host window cannot be found, for
    /// example because the running instance is still starting, the second
    /// instance leaves without saying anything.
    /// </remarks>
    public static void NotifyRunningInstance()
    {
        var host = FindHostWindow();

        if (host == 0)
        {
            return;
        }

        NativeMethods.PostMessage(host, WmAppSecondInstance, 0, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Disposing the mutex releases it: the next instance is free to take
        // it, and the name disappears when the last handle is closed.
        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finds the hidden window of the running instance.
    /// </summary>
    /// <returns>The handle of the window, or zero when there is none.</returns>
    private static nint FindHostWindow()
    {
        for (var attempt = 0; attempt < HostWindowLookupAttempts; attempt++)
        {
            // The class of the window is the one of the wrapper of WPF, which
            // is not known in advance: the window is looked for by its title.
            var host = NativeMethods.FindWindowW(null, HostWindowTitle);

            if (host != 0)
            {
                return host;
            }

            // The running instance may not have created its window yet.
            Thread.Sleep(HostWindowLookupDelayMilliseconds);
        }

        return 0;
    }
}
