using MediaPilot.Platform.Instance;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests for the guard that keeps a single instance of the application.
/// </summary>
public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void TryAcquire_SucceedsWhenNoOtherInstanceHoldsTheMutex()
    {
        var name = UniqueMutexName();

        using var guard = SingleInstanceGuard.TryAcquire(name);

        Assert.NotNull(guard);
    }

    [Fact]
    public void TryAcquire_FailsWhenAnotherInstanceHoldsTheMutex()
    {
        var name = UniqueMutexName();

        using var first = SingleInstanceGuard.TryAcquire(name);
        var second = AcquireFromAnotherThread(name);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void TryAcquire_SucceedsAgainAfterTheFirstGuardIsReleased()
    {
        var name = UniqueMutexName();

        using (var first = SingleInstanceGuard.TryAcquire(name))
        {
            Assert.NotNull(first);
        }

        using var second = SingleInstanceGuard.TryAcquire(name);

        Assert.NotNull(second);
    }

    [Fact]
    public void TryAcquire_UsesADifferentMutexForEveryName()
    {
        var first = UniqueMutexName();
        var second = UniqueMutexName();

        using var guard = SingleInstanceGuard.TryAcquire(first);
        using var other = SingleInstanceGuard.TryAcquire(second);

        Assert.NotNull(guard);
        Assert.NotNull(other);
    }

    [Fact]
    public void NotifyRunningInstance_DoesNotThrowWhenThereIsNoHostWindow()
    {
        // The window of a running instance does not exist in the test
        // environment: the notice is a courtesy and it must never fail.
        SingleInstanceGuard.NotifyRunningInstance();
    }

    /// <summary>
    /// Builds the name of a mutex that belongs to this test only.
    /// </summary>
    /// <returns>The name of the mutex.</returns>
    private static string UniqueMutexName() => $@"Local\MediaPilot.Tests.{Guid.NewGuid():N}";

    /// <summary>
    /// Tries to take the mutex from another thread.
    /// </summary>
    /// <remarks>
    /// The ownership of a mutex belongs to the thread that took it, and a
    /// thread that already owns it can take it again. The second instance of
    /// the application is another process, and this method reproduces that
    /// situation from the same process.
    /// </remarks>
    /// <param name="mutexName">Name of the mutex.</param>
    /// <returns>The guard, or null when the mutex was already taken.</returns>
    private static SingleInstanceGuard? AcquireFromAnotherThread(string mutexName)
    {
        SingleInstanceGuard? guard = null;

        var thread = new Thread(() => guard = SingleInstanceGuard.TryAcquire(mutexName));
        thread.Start();
        thread.Join();

        return guard;
    }
}
