using MediaPilot.Platform.Volume;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests for the volume controller of the default output device.
/// </summary>
public class WindowsVolumeControllerTests
{
    [Fact]
    public void TryGetVolume_ReadsTheLevelOfTheDefaultDevice()
    {
        var device = new FakeDevice(level: 0.42f);
        using var controller = new WindowsVolumeController(new FakeResolver(device));

        var read = controller.TryGetVolume(out var volume);

        Assert.True(read);
        Assert.Equal(42, volume);
    }

    [Fact]
    public void TryGetVolume_WithoutAnOutputDeviceReturnsFalse()
    {
        using var controller = new WindowsVolumeController(new FakeResolver());

        var read = controller.TryGetVolume(out var volume);

        Assert.False(read);
        Assert.Equal(0, volume);
    }

    [Fact]
    public void TryIncreaseVolume_MovesOnePointAndLiftsTheSilence()
    {
        var device = new FakeDevice(level: 0.50f, muted: true);
        using var controller = new WindowsVolumeController(new FakeResolver(device));

        var applied = controller.TryIncreaseVolume();

        Assert.True(applied);
        Assert.Equal(51, Percent(device.Level));
        Assert.False(device.Muted);
    }

    [Fact]
    public void TryDecreaseVolume_MovesOnePointAndKeepsTheSilence()
    {
        var device = new FakeDevice(level: 0.50f, muted: true);
        using var controller = new WindowsVolumeController(new FakeResolver(device));

        var applied = controller.TryDecreaseVolume();

        Assert.True(applied);
        Assert.Equal(49, Percent(device.Level));
        Assert.True(device.Muted);
    }

    [Fact]
    public void TheVolumeNeverGoesOutOfTheRangeOfTheSystem()
    {
        var highest = new FakeDevice(level: 1f);
        using var atTop = new WindowsVolumeController(new FakeResolver(highest));

        atTop.TryIncreaseVolume();

        Assert.Equal(100, Percent(highest.Level));

        var lowest = new FakeDevice(level: 0f);
        using var atBottom = new WindowsVolumeController(new FakeResolver(lowest));

        atBottom.TryDecreaseVolume();

        Assert.Equal(0, Percent(lowest.Level));
    }

    [Fact]
    public void TheDeviceIsResolvedOnEveryOperation()
    {
        // The device is resolved again on every call, so the commands follow
        // the output device that is the default now and not the one that was
        // the default when the application started.
        var first = new FakeDevice(level: 0.10f);
        var second = new FakeDevice(level: 0.80f);
        var resolver = new FakeResolver(first, second);
        using var controller = new WindowsVolumeController(resolver);

        controller.TryGetVolume(out var before);
        controller.TryGetVolume(out var after);

        Assert.Equal(10, before);
        Assert.Equal(80, after);
    }

    [Fact]
    public void TheVolumeCommandIsAppliedToTheDeviceOfTheMoment()
    {
        var first = new FakeDevice(level: 0.50f);
        var second = new FakeDevice(level: 0.50f);
        using var controller = new WindowsVolumeController(new FakeResolver(first, second));

        controller.TryIncreaseVolume();

        Assert.Equal(51, Percent(first.Level));
        Assert.Equal(50, Percent(second.Level));
    }

    [Fact]
    public void TheControllerReadsTheOutputDeviceOfTheSystem()
    {
        using var controller = new WindowsVolumeController();

        var read = controller.TryGetVolume(out var volume);

        Assert.True(read);
        Assert.InRange(volume, 0, 100);
    }

    private static int Percent(float level) =>
        (int)Math.Round(Math.Clamp(level, 0f, 1f) * 100f, MidpointRounding.AwayFromZero);

    /// <summary>
    /// An output device that only keeps the values in memory.
    /// </summary>
    private sealed class FakeDevice : IAudioEndpointVolumeControl
    {
        internal FakeDevice(float level, bool muted = false)
        {
            Level = level;
            Muted = muted;
        }

        internal float Level { get; private set; }

        internal bool Muted { get; private set; }

        public bool TryGetLevel(out float level)
        {
            level = Level;
            return true;
        }

        public bool TryGetMuted(out bool muted)
        {
            muted = Muted;
            return true;
        }

        public bool SetMuted(bool muted)
        {
            Muted = muted;
            return true;
        }

        public bool SetLevel(float level)
        {
            Level = level;
            return true;
        }
    }

    /// <summary>
    /// A resolver that returns the devices in order, one per call.
    /// </summary>
    private sealed class FakeResolver : IAudioEndpointResolver
    {
        private readonly Queue<IAudioEndpointVolumeControl> _devices;

        internal FakeResolver(params IAudioEndpointVolumeControl[] devices)
        {
            _devices = new Queue<IAudioEndpointVolumeControl>(devices);
        }

        public IAudioEndpointVolumeControl? Resolve() =>
            _devices.Count > 0 ? _devices.Dequeue() : null;
    }
}
