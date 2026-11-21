namespace MediaPilot.Configuration;

/// <summary>
/// Local storage paths for the application.
/// </summary>
public static class AppPaths
{
    /// <summary>Name of the application data folder.</summary>
    public const string FolderName = "MediaPilot";

    /// <summary>
    /// Root folder of the application data.
    /// </summary>
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        FolderName);

    /// <summary>Path of the settings file.</summary>
    public static string SettingsFile { get; } = Path.Combine(Root, "settings.json");

    /// <summary>Folder for diagnostic logs.</summary>
    public static string LogsDirectory { get; } = Path.Combine(Root, "logs");

    /// <summary>
    /// Makes sure the application data folder exists.
    /// </summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsDirectory);
    }
}
