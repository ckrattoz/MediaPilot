using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Core.Services;
using MediaPilot.Localization.Resources;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the media command executor.
/// </summary>
public sealed class MediaCommandExecutorTests
{
    [Fact]
    public void Execute_PlaybackCommandIsDelegatedToTheController()
    {
        var media = new FakeMediaController();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), new FakeNotificationPresenter());

        executor.Execute(MediaCommand.Next);

        Assert.Equal(MediaCommand.Next, media.LastCommand);
    }

    [Fact]
    public void Execute_VolumeUpRaisesAndNotifies()
    {
        var volume = new FakeVolumeController { Volume = 40 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications);

        executor.Execute(MediaCommand.VolumeUp);

        Assert.True(volume.IncreaseCalled);

        // The notification is shown right away: the user sees the volume
        // going up while the key is held down.
        Assert.True(notifications.VolumeShown);
        Assert.Equal(40, notifications.LastVolume);
        Assert.True(notifications.LastIncreased);
    }

    [Fact]
    public void Execute_VolumeDownNotifiesAsDecrease()
    {
        var volume = new FakeVolumeController { Volume = 70 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications);

        executor.Execute(MediaCommand.VolumeDown);

        Assert.True(volume.DecreaseCalled);
        Assert.True(notifications.VolumeShown);
        Assert.False(notifications.LastIncreased);
    }

    [Fact]
    public void Execute_MuteTogglesStateAndNotifies()
    {
        var volume = new FakeVolumeController { Muted = false, Volume = 55 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications);

        executor.Execute(MediaCommand.VolumeMute);

        Assert.True(volume.SetMuteCalled);
        Assert.True(volume.Muted);
        Assert.True(notifications.VolumeShown);
        Assert.True(notifications.LastMuted);
    }

    [Fact]
    public void Execute_EveryVolumeChangeNotifiesAgain()
    {
        var volume = new FakeVolumeController { Volume = 40 };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), volume, notifications);

        executor.Execute(MediaCommand.VolumeUp);
        executor.Execute(MediaCommand.VolumeUp);
        executor.Execute(MediaCommand.VolumeUp);

        // Every change produces its own notification: the one that is on
        // screen is updated in place by the presenter.
        Assert.Equal(3, notifications.VolumeShownCount);
    }

    [Fact]
    public void Execute_WithNotificationsDisabledDoesNotNotify()
    {
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(new FakeMediaController(), new FakeVolumeController(), notifications)
        {
            ShowNotifications = false,
        };

        executor.Execute(MediaCommand.VolumeUp);
        executor.Execute(MediaCommand.VolumeMute);

        Assert.False(notifications.VolumeShown);
    }

    [Fact]
    public void Execute_InformationShowsTheTrackTitle()
    {
        var media = new FakeMediaController { Track = new MediaTrack("Test track", string.Empty) };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), notifications);

        executor.Execute(MediaCommand.ShowInformation);

        Assert.True(notifications.MessageShown);
        Assert.Equal("Test track", notifications.LastMessage);
    }

    [Fact]
    public void Execute_InformationShowsTheTitleAndTheArtist()
    {
        var media = new FakeMediaController { Track = new MediaTrack("Test track", "Test artist") };
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), notifications);

        executor.Execute(MediaCommand.ShowInformation);

        Assert.True(notifications.MessageShown);
        Assert.Equal("Test track - Test artist", notifications.LastMessage);
    }

    [Fact]
    public void Execute_InformationWithoutTrackReportsThatNothingIsPlaying()
    {
        var media = new FakeMediaController();
        var notifications = new FakeNotificationPresenter();
        var executor = new MediaCommandExecutor(media, new FakeVolumeController(), notifications);

        executor.Execute(MediaCommand.ShowInformation);

        Assert.True(notifications.MessageShown);
        Assert.Equal(Strings.Executor_NoTrackPlaying, notifications.LastMessage);
    }

    private sealed class FakeMediaController : IMediaController
    {
        public MediaCommand? LastCommand { get; private set; }

        public MediaTrack? Track { get; set; }

        public bool TryExecute(MediaCommand command)
        {
            LastCommand = command;
            return true;
        }

        public MediaTrack? GetCurrentTrack() => Track;
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

        public int VolumeShownCount { get; private set; }

        public int LastVolume { get; private set; }

        public bool LastMuted { get; private set; }

        public bool LastIncreased { get; private set; }

        public string? LastMessage { get; private set; }

        public void ShowVolume(int volume, bool muted, bool increased, string? trackTitle)
        {
            VolumeShown = true;
            VolumeShownCount++;
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
