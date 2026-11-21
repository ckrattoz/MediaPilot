using MediaPilot.Core;
using MediaPilot.Platform.Interop;
using MediaPilot.Platform.Theme;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests for the system theme service.
/// </summary>
public class WindowsThemeServiceTests
{
    [Fact]
    public void Resolve_LightAndDarkAreRespectedAsIs()
    {
        var service = new WindowsThemeService();

        Assert.Equal(ThemePreference.Light, service.Resolve(ThemePreference.Light));
        Assert.Equal(ThemePreference.Dark, service.Resolve(ThemePreference.Dark));
    }

    [Fact]
    public void Resolve_AutoFollowsTheSystemTheme()
    {
        var service = new WindowsThemeService();

        var resolved = service.Resolve(ThemePreference.Auto);

        Assert.Equal(service.IsDark ? ThemePreference.Dark : ThemePreference.Light, resolved);
    }

    [Fact]
    public void IsDark_MatchesAppsUseLightTheme()
    {
        var service = new WindowsThemeService();

        // The registry key that decides the application theme is
        // AppsUseLightTheme; reading it is what avoids the mixed rendering.
        Assert.Equal(!WindowsThemeService.ReadAppsUseLightTheme(), service.IsDark);
    }

    [Fact]
    public void NotifyThemeChanged_RaisesTheEvent()
    {
        var service = new WindowsThemeService();
        var raised = 0;
        service.ThemeChanged += (_, _) => raised++;

        service.NotifyThemeChanged();

        Assert.Equal(1, raised);
    }
}

/// <summary>
/// Tests for the native message router.
/// </summary>
public class NativeMessageRouterTests
{
    [Fact]
    public void Dispatch_WithoutHandlersReturnsFalse()
    {
        var router = new NativeMessageRouter();

        Assert.False(router.Dispatch(new NativeMessage(0x0312, 1, 0)));
    }

    [Fact]
    public void Dispatch_DeliversTheMessageToTheHandler()
    {
        var router = new NativeMessageRouter();
        NativeMessage? received = null;
        router.Register(message =>
        {
            received = message;
            return true;
        });

        var result = router.Dispatch(new NativeMessage(0x0312, 42, 7));

        Assert.True(result);
        Assert.NotNull(received);
        Assert.Equal(0x0312, received!.Value.Message);
        Assert.Equal(42, received.Value.WParam);
        Assert.Equal(7, received.Value.LParam);
    }

    [Fact]
    public void Dispatch_StopsAtFirstHandlerThatProcesses()
    {
        var router = new NativeMessageRouter();
        var second = 0;
        router.Register(_ => true);
        router.Register(_ =>
        {
            second++;
            return true;
        });

        router.Dispatch(new NativeMessage(1, 0, 0));

        Assert.Equal(0, second);
    }

    [Fact]
    public void Dispatch_ContinuesIfTheHandlerDoesNotProcess()
    {
        var router = new NativeMessageRouter();
        var processed = false;
        router.Register(_ => false);
        router.Register(_ =>
        {
            processed = true;
            return true;
        });

        Assert.True(router.Dispatch(new NativeMessage(1, 0, 0)));
        Assert.True(processed);
    }
}
