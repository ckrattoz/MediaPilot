using MediaPilot.Core;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the policy of the entry of the automatic start.
/// </summary>
/// <remarks>
/// The installer registers the program in the <c>Run</c> key so that the
/// uninstaller can remove it. The program has to end without running when the
/// installer started it and the user has not enabled the automatic start.
/// </remarks>
public class StartupPolicyTests
{
    [Fact]
    public void ShouldDisableAndExit_IsTrueWhenTheInstallerStartedItAndTheSettingIsOff()
    {
        Assert.True(StartupPolicy.ShouldDisableAndExit(startedByInstaller: true, startWithWindows: false));
    }

    [Fact]
    public void ShouldDisableAndExit_IsFalseWhenTheUserEnabledTheAutomaticStart()
    {
        Assert.False(StartupPolicy.ShouldDisableAndExit(startedByInstaller: true, startWithWindows: true));
    }

    [Fact]
    public void ShouldDisableAndExit_IsFalseWhenTheUserStartedIt()
    {
        Assert.False(StartupPolicy.ShouldDisableAndExit(startedByInstaller: false, startWithWindows: false));
    }

    [Fact]
    public void ShouldDisableAndExit_IsFalseWhenTheUserEnabledTheAutomaticStartAndStartedIt()
    {
        Assert.False(StartupPolicy.ShouldDisableAndExit(startedByInstaller: false, startWithWindows: true));
    }
}
