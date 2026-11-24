using System.Text.Json;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace MediaPilot.Configuration;

/// <summary>
/// Settings store backed by a JSON file inside the local application data.
/// </summary>
/// <remarks>
/// It replaces the two <c>REG_DWORD</c> values used by the original project:
/// a JSON file is readable, versionable and allows the schema to evolve
/// without touching the Windows registry.
/// </remarks>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private readonly ILogger<JsonSettingsStore>? _logger;

    /// <summary>
    /// Initializes the store.
    /// </summary>
    /// <param name="filePath">Path of the settings file.</param>
    /// <param name="logger">Optional diagnostic log.</param>
    public JsonSettingsStore(string filePath, ILogger<JsonSettingsStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _logger = logger;
    }

    /// <inheritdoc />
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.AppSettings);

            if (settings is null)
            {
                return new AppSettings();
            }

            return Migrate(settings);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "The settings could not be read from {Path}; the default values are used.", _filePath);
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// Updates settings loaded from an earlier version of the schema.
    /// </summary>
    /// <param name="settings">Loaded settings.</param>
    /// <returns>The settings valid for the current schema.</returns>
    private static AppSettings Migrate(AppSettings settings)
    {
        if (settings.SchemaVersion >= AppSettings.CurrentSchemaVersion)
        {
            return settings;
        }

        // Each schema migration will be added here incrementally.
        return new AppSettings
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion,
            HotkeysEnabled = settings.HotkeysEnabled,
            StartWithWindows = settings.StartWithWindows,
            ShowNotifications = settings.ShowNotifications,
            Theme = settings.Theme,
            Hotkeys = settings.Hotkeys,
        };
    }
}
