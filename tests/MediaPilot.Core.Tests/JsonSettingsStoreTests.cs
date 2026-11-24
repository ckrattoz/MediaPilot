using MediaPilot.Configuration;
using MediaPilot.Core;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the JSON settings store.
/// </summary>
public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "MediaPilot.Tests",
        Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void Load_WithoutFileReturnsDefaults()
    {
        var store = new JsonSettingsStore(SettingsPath);

        var settings = store.Load();

        Assert.True(settings.HotkeysEnabled);
        Assert.True(settings.ShowNotifications);
        Assert.False(settings.StartWithWindows);
        Assert.Equal(ThemePreference.Auto, settings.Theme);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void SaveAndLoad_PreserveSettings()
    {
        var store = new JsonSettingsStore(SettingsPath);
        var expected = new AppSettings
        {
            HotkeysEnabled = false,
            ShowNotifications = false,
            StartWithWindows = true,
            Theme = ThemePreference.Dark,
            Hotkeys = [new HotkeyBinding(new HotkeyGesture(ModifierKeys.Control | ModifierKeys.Win, VirtualKey.F5), MediaCommand.Next)],
        };

        store.Save(expected);
        var actual = store.Load();

        Assert.False(actual.HotkeysEnabled);
        Assert.False(actual.ShowNotifications);
        Assert.True(actual.StartWithWindows);
        Assert.Equal(ThemePreference.Dark, actual.Theme);
        Assert.Single(actual.Hotkeys);
        Assert.Equal(MediaCommand.Next, actual.Hotkeys[0].Command);
        Assert.Equal(VirtualKey.F5, actual.Hotkeys[0].Gesture.Key);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Win, actual.Hotkeys[0].Gesture.Modifiers);
    }

    [Fact]
    public void Load_WithCorruptFileReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, "{ this is not valid json");

        var store = new JsonSettingsStore(SettingsPath);

        var settings = store.Load();

        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(7, settings.Hotkeys.Length);
    }

    [Fact]
    public void Save_CreatesTheDirectoryIfMissing()
    {
        var store = new JsonSettingsStore(SettingsPath);

        store.Save(new AppSettings());

        Assert.True(File.Exists(SettingsPath));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
