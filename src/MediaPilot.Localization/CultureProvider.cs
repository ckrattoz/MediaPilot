using System.Globalization;

namespace MediaPilot.Localization;

/// <summary>
/// Resolves and applies the user interface culture for the application.
/// </summary>
/// <remarks>
/// <para>
/// The neutral resource file (<c>Strings.resx</c>) holds the primary language,
/// Spanish. Every other language lives in its own satellite file
/// (<c>Strings.en.resx</c>, and any future <c>Strings.&lt;culture&gt;.resx</c>).
/// When the requested culture has no satellite file, the
/// <see cref="ResourceManager"/> falls back to the neutral resources, so the
/// interface is always available in the primary language.
/// </para>
/// <para>
/// The culture is resolved once, when the assembly is loaded, before any
/// window is created. That ordering matters: XAML markup that resolves a
/// string does it while the visual tree is built, and the lookup uses the
/// current UI culture at that moment.
/// </para>
/// </remarks>
public static class CultureProvider
{
    /// <summary>
    /// Name of the environment variable that overrides the user interface language.
    /// </summary>
    /// <remarks>
    /// Useful to try another language without changing the Windows settings, for
    /// example <c>MEDIAPILOT_LANGUAGE=en</c>.
    /// </remarks>
    public const string LanguageVariable = "MEDIAPILOT_LANGUAGE";

    /// <summary>
    /// The primary language of the application, used when no other language is
    /// available for the requested culture.
    /// </summary>
    public const string PrimaryLanguage = "es";

    /// <summary>
    /// Applies the user interface culture to the current thread and to every
    /// thread created afterwards.
    /// </summary>
    public static void Apply()
    {
        var culture = Resolve();

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    /// <summary>
    /// Determines the culture to use for the user interface.
    /// </summary>
    /// <returns>The culture to apply.</returns>
    private static CultureInfo Resolve()
    {
        var requested = Environment.GetEnvironmentVariable(LanguageVariable);

        if (!string.IsNullOrWhiteSpace(requested))
        {
            try
            {
                return CultureInfo.GetCultureInfo(requested);
            }
            catch (CultureNotFoundException)
            {
                // An unknown language falls back to the operating system culture
                // rather than failing: the interface must always start.
            }
        }

        return CultureInfo.CurrentUICulture;
    }
}
