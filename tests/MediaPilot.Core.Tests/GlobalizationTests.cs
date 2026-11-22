using System.Globalization;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests that guarantee the runtime is not in globalization-invariant mode.
/// </summary>
/// <remarks>
/// The invariant mode leaves the application without real cultures. WPF
/// resolves its default language as "en-US" and the font cache builds real
/// cultures, so every window that shows text dies with a
/// <see cref="CultureNotFoundException"/>. These tests fail if the invariant
/// mode is enabled again, before the defect reaches the user interface.
/// </remarks>
public sealed class GlobalizationTests
{
    [Fact]
    public void TheRuntimeResolvesSpecificCultures()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        Assert.Equal("en-US", culture.Name);
        Assert.False(culture.IsNeutralCulture);
    }

    [Fact]
    public void TheRuntimeResolvesThePrimaryLanguageOfTheInterface()
    {
        var culture = CultureInfo.GetCultureInfo("es");

        Assert.Equal("es", culture.Name);
        Assert.True(culture.IsNeutralCulture);
    }
}
