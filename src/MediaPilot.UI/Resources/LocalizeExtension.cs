using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace MediaPilot.UI.Resources;

/// <summary>
/// Resolves a user interface string from the shared resource file.
/// </summary>
/// <remarks>
/// <para>
/// Use it from XAML with the <c>loc</c> prefix:
/// </para>
/// <code>
/// &lt;TextBlock Text="{loc:Localize Settings_Heading}" /&gt;
/// </code>
/// <para>
/// The key is the name of the entry in
/// <c>MediaPilot.Localization/Resources/Strings.resx</c>. The lookup honors the
/// current UI culture, so the same markup renders the string of the active
/// language.
/// </para>
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocalizeExtension : MarkupExtension
{
    private static readonly ResourceManager Manager = new(
        "MediaPilot.Localization.Resources.Strings",
        typeof(MediaPilot.Localization.Resources.Strings).Assembly);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeExtension"/> class.
    /// </summary>
    public LocalizeExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeExtension"/> class
    /// with the resource key to resolve.
    /// </summary>
    /// <param name="key">Name of the resource entry.</param>
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Name of the resource entry to resolve.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Resolves the resource entry for the current UI culture.
    /// </summary>
    /// <param name="serviceProvider">The markup service provider.</param>
    /// <returns>
    /// The localized text, or the key itself when the entry does not exist.
    /// Returning the key makes a missing translation visible instead of
    /// silently rendering an empty string.
    /// </returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
        {
            return string.Empty;
        }

        return Manager.GetString(Key, CultureInfo.CurrentUICulture) ?? Key;
    }
}
