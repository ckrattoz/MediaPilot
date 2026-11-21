using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Core.Services;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the media command executor.
/// </summary>
public sealed class MediaCommandExecutorTests
{
    private const int TestDebounceMs = 20;

    [Fact]
    public void Execute_PlaybackCommandIsDelegatedToTheController()
    {
        var media = new FakeMediaController();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), new FakeNotificationPresenter(), TestDebounceMs);

        executor.Execute(MediaCommand.Next);

        Assert.Equal(MediaCommand.Next, media.LastCommand);
    }

    [Fact]
    public void Execute_VolumeUpRaisesAndNotifies()
    {
        var volume = new FakeVolumeController { Volume = 40 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications, TestDebounceMs);

        executor.Execute(MediaCommand.VolumeUp);

        Assert.True(volume.IncreaseCalled);
        Assert.False(notifications.VolumeShown);

        WaitForNotification();

        Assert.True(notifications.VolumeShown);
        Assert.Equal(40, notifications.LastVolume);
        Assert.True(notifications.LastIncreased);
    }

    [Fact]
    public void Execute_VolumeDownNotifiesAsDecrease()
    {
        var volume = new FakeVolumeController { Volume = 70 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications, TestDebounceMs);

        executor.Execute(MediaCommand.VolumeDown);

        WaitForNotification();

        Assert.True(volume.DecreaseCalled);
        Assert.True(notifications.VolumeShown);
        Assert.False(notifications.LastIncreased);
    }

    [Fact]
    public void Execute_MuteTogglesStateAndNotifies()
    {
        var volume = new FakeVolumeController { Muted = false, Volume = 55 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications, TestDebounceMs);

        executor.Execute(MediaCommand.VolumeMute);

        WaitForNotification();

        Assert.True(volume.SetMuteCalled);
        Assert.True(volume.Muted);
        Assert.True(notifications.VolumeShown);
        Assert.True(notifications.LastMuted);
    }

    [Fact]
    public void Execute_InformationShowsTheTrackTitle()
    {
        var media = new FakeMediaController { TrackTitle = "Test track" };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), notifications, TestDebounceMs);

        executor.Execute(MediaCommand.ShowInformation);

        Assert.True(notifications.MessageShown);
        Assert.Equal("Test track", notifications.LastMessage);
    }

    private static void WaitForNotification()
    {
        Thread.Sleep(TestDebounceMs * 4);
    }

    private sealed class FakeMediaController : IMediaController
    {
        public MediaCommand? LastCommand { get; private set; }

        public string? TrackTitle { get; set; }

        public bool TryExecute(MediaCommand command)
        {
            LastCommand = command;
            return true;
        }

        public string? GetCurrentTrackTitle() => TrackTitle;
    }

    private sealed class FakeVolumeController : IVolumeController
    {
        public int Volume { get; set; }

        public bool Muted { get; set; }

        public bool IncreaseCalled { get; private set; }

        public bool DecreaseCalled { get; private set; }

        public bool SetMuteCalled { get; private set; }

        public bool TryGetVolume(out int volume)
        {
            volume = Volume;
            return true;
        }

        public bool TryGetMuted(out bool muted)
        {
            muted = Muted;
            return true;
        }

        public bool TrySetMuted(bool muted)
        {
            SetMuteCalled = true;
            Muted = muted;
            return true;
        }

        public bool TryIncreaseVolume()
        {
            IncreaseCalled = true;
            return true;
        }

        public bool TryDecreaseVolume()
        {
            DecreaseCalled = true;
            return true;
        }
    }

    private sealed class FakeNotificationPresenter : INotificationPresenter
    {
        public bool VolumeShown { get; private set; }

        public bool MessageShown { get; private set; }

        public int LastVolume { get; private set; }

        public bool LastMuted { get; private set; }

        public bool LastIncreased { get; private set; }

        public string? LastMessage { get; private set; }

        public void ShowVolume(int volume, bool muted, bool increased, string? trackTitle)
        {
            VolumeShown = true;
            LastVolume = volume;
            LastMuted = muted;
            LastIncreased = increased;
        }

        public void ShowMessage(string title, string message)
        {
            MessageShown = true;
            LastMessage = message;
        }
    }
}
