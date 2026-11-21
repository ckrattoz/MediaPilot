using System.Windows;
using MediaPilot.Core;

namespace MediaPilot.UI.Services;

/// <summary>
/// Applies the user interface theme: the WPF light/dark mode and the color palette.
/// </summary>
public sealed class ThemeManager
{
    private const string PaletteResourcePrefix = "pack://application:,,,/MediaPilot.UI;component/Themes/Palette.";

    private ThemePreference _applied = ThemePreference.Auto;

    /// <summary>
    /// Applies the theme preference, resolving <see cref="ThemePreference.Auto"/>
    /// against the actual system theme.
    /// </summary>
    /// <param name="preference">Configured preference.</param>
    /// <param name="systemIsDark">Indicates whether the system uses the dark theme.</param>
    public void Apply(ThemePreference preference, bool systemIsDark)
    {
        var resolved = preference == ThemePreference.Auto
            ? systemIsDark ? ThemePreference.Dark : ThemePreference.Light
            : preference;

        SetThemeMode(resolved);
        SwapPalette(resolved);

        _applied = resolved;
    }

    /// <summary>
    /// Theme currently applied.
    /// </summary>
    public ThemePreference Applied => _applied;

    private static void SetThemeMode(ThemePreference preference)
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        // ThemeMode is an experimental WPF API: it can only be assigned from
        // code with the warning suppressed. Defining it in XAML does not
        // produce the diagnostic, but it does not allow changing it at runtime.
#pragma warning disable WPF0001
        application.ThemeMode = preference == ThemePreference.Dark
            ? System.Windows.ThemeMode.Dark
            : System.Windows.ThemeMode.Light;
#pragma warning restore WPF0001
    }

    private static void SwapPalette(ThemePreference preference)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var dictionaries = resources.MergedDictionaries;

        for (var i = dictionaries.Count - 1; i >= 0; i--)
        {
            if (dictionaries[i].Source?.OriginalString.Contains("Palette.", StringComparison.Ordinal) == true)
            {
                dictionaries.RemoveAt(i);
            }
        }

        var palette = new ResourceDictionary
        {
            Source = new Uri($"{PaletteResourcePrefix}{preference}.xaml", UriKind.Absolute),
        };

        dictionaries.Insert(0, palette);
    }
}
