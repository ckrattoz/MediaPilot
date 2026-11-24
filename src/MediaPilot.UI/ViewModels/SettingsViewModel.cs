using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;

namespace MediaPilot.UI.ViewModels;

/// <summary>
/// State and commands of the settings panel.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly IHotkeyService _hotkeyService;
    private readonly IStartupService _startupService;

    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <param name="settingsStore">Settings store.</param>
    /// <param name="hotkeyService">Global shortcut service.</param>
    /// <param name="startupService">Automatic startup service.</param>
    public SettingsViewModel(
        ISettingsStore settingsStore,
        IHotkeyService hotkeyService,
        IStartupService startupService)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(hotkeyService);
        ArgumentNullException.ThrowIfNull(startupService);

        _settingsStore = settingsStore;
        _hotkeyService = hotkeyService;
        _startupService = startupService;

        Load(_settingsStore.Load());
    }

    /// <summary>Shortcut bindings shown in the panel.</summary>
    public ObservableCollection<HotkeyBindingViewModel> Hotkeys { get; } = [];

    /// <summary>Indicates whether the global shortcuts are active.</summary>
    [ObservableProperty]
    private bool _hotkeysEnabled;

    /// <summary>Indicates whether the volume notifications are shown.</summary>
    [ObservableProperty]
    private bool _showNotifications;

    /// <summary>Indicates whether the application starts with Windows.</summary>
    [ObservableProperty]
    private bool _startWithWindows;

    /// <summary>User interface theme preference.</summary>
    [ObservableProperty]
    private ThemePreference _theme = ThemePreference.Auto;

    /// <summary>
    /// Raised when the view model has applied and saved the changes.
    /// </summary>
    public event EventHandler? Saved;

    /// <summary>
    /// Loads the settings into the view model.
    /// </summary>
    /// <param name="settings">Settings to display.</param>
    public void Load(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        HotkeysEnabled = settings.HotkeysEnabled;
        ShowNotifications = settings.ShowNotifications;
        StartWithWindows = settings.StartWithWindows;
        Theme = settings.Theme;

        Hotkeys.Clear();
        foreach (var binding in settings.Hotkeys)
        {
            Hotkeys.Add(new HotkeyBindingViewModel(binding));
        }
    }

    /// <summary>
    /// Applies the settings: persists them, reconfigures the shortcuts and updates
    /// the automatic startup.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        var settings = new AppSettings
        {
            HotkeysEnabled = HotkeysEnabled,
            ShowNotifications = ShowNotifications,
            StartWithWindows = StartWithWindows,
            Theme = Theme,
            Hotkeys = [.. Hotkeys
                .Where(h => h.Gesture is not null)
                .Select(h => new HotkeyBinding(h.Gesture!.Value, h.Command))],
        };

        _settingsStore.Save(settings);

        _startupService.TrySetEnabled(settings.StartWithWindows);
        ReapplyHotkeys(settings);

        Saved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Discards the changes and shows the stored settings again.
    /// </summary>
    [RelayCommand]
    private void Cancel() => Load(_settingsStore.Load());

    private void ReapplyHotkeys(AppSettings settings)
    {
        _hotkeyService.UnregisterAll();

        if (!settings.HotkeysEnabled)
        {
            return;
        }

        foreach (var binding in settings.Hotkeys)
        {
            _hotkeyService.Register(binding.Gesture, binding.Command);
        }
    }
}
