using MediaPilot.Platform.Startup;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests for the detection of the start that comes from the entry of the
/// installer.
/// </summary>
/// <remarks>
/// The installer adds an argument to the entry of the automatic start, so the
/// program can tell that the entry was written by the installer and not by
/// the user. The detection is a pure function of the arguments of the
/// process, and it never touches the registry.
/// </remarks>
public class WindowsStartupServiceTests
{
    [Fact]
    public void IsInstallerStartup_IsTrueWithTheArgumentOfTheInstaller()
    {
        Assert.True(WindowsStartupService.IsInstallerStartup([WindowsStartupService.InstallerStartupArgument]));
    }

    [Fact]
    public void IsInstallerStartup_IsTrueWithTheArgumentAmongOthers()
    {
        string[] arguments = ["--other", WindowsStartupService.InstallerStartupArgument];

        Assert.True(WindowsStartupService.IsInstallerStartup(arguments));
    }

    [Fact]
    public void IsInstallerStartup_IsFalseWithoutArguments()
    {
        Assert.False(WindowsStartupService.IsInstallerStartup([]));
    }

    [Fact]
    public void IsInstallerStartup_IsFalseWithOtherArguments()
    {
        Assert.False(WindowsStartupService.IsInstallerStartup(["--other"]));
    }

    [Fact]
    public void IsInstallerStartup_IsFalseWithAnArgumentThatOnlyLooksLikeTheOne()
    {
        Assert.False(WindowsStartupService.IsInstallerStartup(["--installer-startup-extra"]));
    }
}
