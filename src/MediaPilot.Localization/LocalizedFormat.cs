using System.Collections.Concurrent;
using System.Text;

namespace MediaPilot.Localization;

/// <summary>
/// Formats a localized string with a composite format that is parsed only once.
/// </summary>
/// <remarks>
/// <para>
/// The format strings of the resource files are not compile-time constants, so
/// every call to <see cref="string.Format(IFormatProvider, string, object[])"/>
/// would parse the format again. The analyzer CA1863 exists precisely for that
/// case, and this type is the answer: it keeps one
/// <see cref="CompositeFormat"/> per format string.
/// </para>
/// <para>
/// The cache is keyed by the format string itself, not by a name, so two
/// languages with a different placeholder order use their own entry. The
/// interface culture is resolved once at startup, so the number of entries is
/// bounded by the number of format strings in the application.
/// </para>
/// </remarks>
public static class LocalizedFormat
{
    private static readonly ConcurrentDictionary<string, CompositeFormat> Formats =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Formats a localized string using the given culture and arguments.
    /// </summary>
    /// <param name="provider">The culture used to format the arguments.</param>
    /// <param name="format">The format string, typically a resource entry.</param>
    /// <param name="arguments">The values of the placeholders.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(IFormatProvider provider, string format, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(format);

        var composite = Formats.GetOrAdd(format, CompositeFormat.Parse);

        return string.Format(provider, composite, arguments);
    }
}
