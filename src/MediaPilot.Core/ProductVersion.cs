using System.Reflection;

namespace MediaPilot.Core;

/// <summary>
/// Version of the running product.
/// </summary>
/// <remarks>
/// The version is read from the entry assembly, so the value that the user
/// sees is the one of the program that is running and not the one of a
/// library. The build metadata that the version may carry (the commit it was
/// built from) is removed: it belongs to the diagnostic log, not to the
/// interface.
/// </remarks>
public static class ProductVersion
{
    /// <summary>
    /// Version of the running product, for example "0.5.0".
    /// </summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(ProductVersion).Assembly;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var separator = informational.IndexOf('+', StringComparison.Ordinal);
            return separator >= 0 ? informational[..separator] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
