namespace MediaPilot.Core.Contracts;

/// <summary>
/// Persistent store for the application settings.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Loads the stored settings.
    /// </summary>
    /// <returns>
    /// The stored settings, or the default values if there is no file or if it
    /// cannot be read.
    /// </returns>
    AppSettings Load();

    /// <summary>
    /// Saves the settings.
    /// </summary>
    /// <param name="settings">Settings to persist.</param>
    void Save(AppSettings settings);
}
