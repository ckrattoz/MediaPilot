using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using Microsoft.Win32;

namespace MediaPilot.Platform.Theme;

/// <summary>
/// Query for the effective operating system theme.
/// </summary>
/// <remarks>
/// It reads <c>AppsUseLightTheme</c> instead of the global shell state: it is
/// the key Windows uses to decide whether <em>applications</em> go light or
/// dark. Reading the right key is what avoids the mixed rendering documented
/// in Phase 3 (dark window with light controls).
/// </remarks>
public sealed class WindowsThemeService : ISystemTheme
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    /// <inheritdoc />
    public event EventHandler? ThemeChanged;

    /// <inheritdoc />
    public bool IsDark => !ReadAppsUseLightTheme();

    /// <inheritdoc />
    public ThemePreference Resolve(ThemePreference preference)
    {
        return preference switch
        {
            ThemePreference.Light => ThemePreference.Light,
            ThemePreference.Dark => ThemePreference.Dark,
            _ => IsDark ? ThemePreference.Dark : ThemePreference.Light,
        };
    }

    /// <summary>
    /// Notifies a system theme change.
    /// </summary>
    /// <remarks>
    /// The system window does not raise a .NET event: the host invokes it
    /// upon receiving <c>WM_SETTINGCHANGE</c>.
    /// </remarks>
    public void NotifyThemeChanged() => ThemeChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Reads the <c>AppsUseLightTheme</c> value.
    /// </summary>
    /// <returns><see langword="true"/> if applications use the light theme.</returns>
    internal static bool ReadAppsUseLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            return key?.GetValue(AppsUseLightThemeValue, 1) is int value && value != 0;
        }
        catch (Exception)
        {
            // If the registry is unavailable, the light theme is assumed,
            // which is the Windows default.
            return true;
        }
    }
}
