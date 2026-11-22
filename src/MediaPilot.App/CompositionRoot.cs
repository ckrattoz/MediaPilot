using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using MediaPilot.App.Logging;
using MediaPilot.Configuration;
using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Core.Services;
using MediaPilot.Localization;
using MediaPilot.Localization.Resources;
using MediaPilot.Platform.Hotkeys;
using MediaPilot.Platform.Interop;
using MediaPilot.Platform.Media;
using MediaPilot.Platform.Startup;
using MediaPilot.Platform.Theme;
using MediaPilot.Platform.Tray;
using MediaPilot.Platform.Volume;
using MediaPilot.UI.Services;
using MediaPilot.UI.ViewModels;
using MediaPilot.UI.Views;
using Microsoft.Extensions.Logging;

namespace MediaPilot.App;

/// <summary>
/// Composition root of the application.
/// </summary>
/// <remarks>
/// It is the only place where the concrete implementations are known: the
/// rest of the solution depends exclusively on the domain contracts.
/// </remarks>
public sealed class CompositionRoot : IDisposable
{
    private const int WmSettingChange = 0x001A;
    private const string IconFolder = "Resources/Icons";

    private readonly App _application;
    private readonly NativeMessageRouter _router = new();
    private readonly ThemeManager _themeManager = new();
    private readonly Dispatcher _dispatcher;

    private HwndSource? _messageWindow;
    private ILoggerFactory? _loggerFactory;
    private WindowsHotkeyService? _hotkeys;
    private TrayIconService? _tray;
    private WindowsVolumeController? _volume;
    private MediaSessionMonitor? _mediaSessions;
    private MediaCommandExecutor? _executor;
    private WindowsThemeService? _systemTheme;
    private NotificationPresenter? _notifications;
    private AppSettings _settings = new();
    private SettingsWindow? _settingsWindow;
    private AboutWindow? _aboutWindow;

    /// <summary>
    /// Initializes the composition root.
    /// </summary>
    /// <param name="application">Host application.</param>
    public CompositionRoot(App application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    /// <summary>Diagnostic log of the application.</summary>
    public ILogger Log { get; private set; } = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    /// <summary>
    /// Builds and starts every service.
    /// </summary>
    public void Start()
    {
        AppPaths.EnsureCreated();

        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(new FileLoggerProvider(AppPaths.LogsDirectory)));

        Log = _loggerFactory.CreateLogger("MediaPilot");

        var settingsStore = new JsonSettingsStore(AppPaths.SettingsFile, _loggerFactory.CreateLogger<JsonSettingsStore>());
        _settings = settingsStore.Load();

        _systemTheme = new WindowsThemeService();
        _themeManager.Apply(_settings.Theme, _systemTheme.IsDark);
        _systemTheme.ThemeChanged += OnSystemThemeChanged;

        _messageWindow = CreateMessageWindow();

        _hotkeys = new WindowsHotkeyService(_messageWindow.Handle, _router);
        _tray = new TrayIconService(
            _messageWindow.Handle,
            _router,
            Path.Combine(AppContext.BaseDirectory, IconFolder, "active.ico"),
            Path.Combine(AppContext.BaseDirectory, IconFolder, "inactive.ico"),
            _loggerFactory.CreateLogger<TrayIconService>());

        _volume = new WindowsVolumeController();

        _mediaSessions = new MediaSessionMonitor(_loggerFactory.CreateLogger<MediaSessionMonitor>());
        var media = new WindowsMediaController(_mediaSessions);
        _notifications = new NotificationPresenter();

        _executor = new MediaCommandExecutor(media, _volume, _notifications, _settings.NotificationDebounceMs);
        _hotkeys.CommandTriggered += (_, command) => _executor.Execute(command);

        _tray.OpenRequested += (_, _) => RunOnDispatcher(() => ShowSettings(settingsStore));
        _tray.AboutRequested += (_, _) => RunOnDispatcher(ShowAbout);
        _tray.HotkeysToggled += (_, _) => RunOnDispatcher(() => ToggleHotkeys(settingsStore));
        _tray.StartupToggled += (_, _) => RunOnDispatcher(() => ToggleStartup(settingsStore));
        _tray.ExitRequested += (_, _) => RunOnDispatcher(() => _application.Shutdown());

        _tray.SetTooltip(Strings.App_Name);
        _tray.SetState(_settings.HotkeysEnabled ? TrayIconState.Active : TrayIconState.Inactive);
        _tray.SetStartupEnabled(_settings.StartWithWindows);

        ApplyHotkeys();

        // The media session monitor connects asynchronously: the media session
        // API is a WinRT API and its request cannot block the user interface
        // thread. Until it answers, the application works without track
        // information.
        _ = _mediaSessions.StartAsync();

        if (Log.IsEnabled(LogLevel.Information))
        {
            Log.LogInformation("MediaPilot started. Hotkeys enabled: {Enabled}.", _settings.HotkeysEnabled);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _executor?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _volume?.Dispose();
        _mediaSessions?.Dispose();
        _messageWindow?.Dispose();
        _loggerFactory?.Dispose();

        GC.SuppressFinalize(this);
    }

    private HwndSource CreateMessageWindow()
    {
        var parameters = new HwndSourceParameters("MediaPilot.Host")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
        };

        var source = new HwndSource(parameters);

        // The host window must not be visible nor appear in the taskbar: it
        // only exists to receive WM_HOTKEY and the tray icon messages.
        WindowVisibility.Hide(source.Handle);

        source.AddHook(OnWindowMessage);
        return source;
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmSettingChange)
        {
            _systemTheme?.NotifyThemeChanged();
            handled = false;
            return IntPtr.Zero;
        }

        handled = _router.Dispatch(new NativeMessage(msg, wParam, lParam));
        return IntPtr.Zero;
    }

    private void OnSystemThemeChanged(object? sender, EventArgs e)
    {
        _themeManager.Apply(_settings.Theme, _systemTheme?.IsDark ?? false);
    }

    private void ApplyHotkeys()
    {
        if (_hotkeys is null)
        {
            return;
        }

        _hotkeys.UnregisterAll();

        if (!_settings.HotkeysEnabled)
        {
            return;
        }

        var conflicts = new List<HotkeyGesture>();

        foreach (var binding in _settings.Hotkeys)
        {
            if (!_hotkeys.Register(binding.Gesture, binding.Command))
            {
                conflicts.Add(binding.Gesture);
                Log.LogWarning("Could not register the shortcut {Gesture}.", binding.Gesture);
            }
        }

        if (conflicts.Count > 0)
        {
            NotifyHotkeyConflicts(conflicts);
        }
    }

    /// <summary>
    /// Tells the user about the shortcuts that could not be registered.
    /// </summary>
    /// <remarks>
    /// A combination that Windows has already assigned does not fail in a
    /// visible way: the shortcut simply does nothing. Without this notice the
    /// user would press the combination without knowing that another program
    /// is behind it.
    /// </remarks>
    /// <param name="conflicts">Gestures that could not be registered.</param>
    private void NotifyHotkeyConflicts(IReadOnlyCollection<HotkeyGesture> conflicts)
    {
        var combinations = string.Join(", ", conflicts.Select(gesture => gesture.ToString()));

        _notifications?.ShowMessage(
            Strings.HotkeyConflict_Title,
            LocalizedFormat.Format(CultureInfo.CurrentCulture, Strings.HotkeyConflict_Message, combinations));
    }

    private void ToggleHotkeys(JsonSettingsStore settingsStore)
    {
        _settings = new AppSettings
        {
            HotkeysEnabled = !_settings.HotkeysEnabled,
            ShowNotifications = _settings.ShowNotifications,
            StartWithWindows = _settings.StartWithWindows,
            Theme = _settings.Theme,
            NotificationDebounceMs = _settings.NotificationDebounceMs,
            Hotkeys = _settings.Hotkeys,
        };

        settingsStore.Save(_settings);

        ApplyHotkeys();
        _tray?.SetState(_settings.HotkeysEnabled ? TrayIconState.Active : TrayIconState.Inactive);

        if (Log.IsEnabled(LogLevel.Information))
        {
            Log.LogInformation("Shortcuts {State}.", _settings.HotkeysEnabled ? "enabled" : "disabled");
        }
    }

    private void ToggleStartup(JsonSettingsStore settingsStore)
    {
        var desired = !_settings.StartWithWindows;

        if (desired)
        {
            var startup = new WindowsStartupService(Environment.ProcessPath ?? string.Empty);
            if (!startup.TrySetEnabled(true))
            {
                _tray?.ShowNotification(Strings.App_Name, Strings.Startup_FailedMessage);
                return;
            }
        }
        else
        {
            var startup = new WindowsStartupService(Environment.ProcessPath ?? string.Empty);
            startup.TrySetEnabled(false);
        }

        _settings = new AppSettings
        {
            HotkeysEnabled = _settings.HotkeysEnabled,
            ShowNotifications = _settings.ShowNotifications,
            StartWithWindows = desired,
            Theme = _settings.Theme,
            NotificationDebounceMs = _settings.NotificationDebounceMs,
            Hotkeys = _settings.Hotkeys,
        };

        settingsStore.Save(_settings);
        _tray?.SetStartupEnabled(desired);
    }

    private void ShowSettings(ISettingsStore settingsStore)
    {
        if (_hotkeys is null)
        {
            return;
        }

        // The window is reused: opening it twice from the tray menu would
        // leave two copies of it on screen.
        if (_settingsWindow is not null)
        {
            if (_settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = null;
        }

        var viewModel = new SettingsViewModel(settingsStore, _hotkeys, new WindowsStartupService(Environment.ProcessPath ?? string.Empty));
        var window = new SettingsWindow(viewModel);
        window.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow = window;
        window.Show();
        window.Activate();
    }

    private void ShowAbout()
    {
        if (_aboutWindow is not null)
        {
            if (_aboutWindow.IsVisible)
            {
                _aboutWindow.Activate();
                return;
            }

            _aboutWindow = null;
        }

        var window = new AboutWindow();
        window.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow = window;
        window.Show();
        window.Activate();
    }

    /// <summary>
    /// Runs an action on the user interface dispatcher.
    /// </summary>
    /// <remarks>
    /// The tray commands are raised from a window procedure, that is, from
    /// inside a native callback: an exception that escapes it terminates the
    /// process with a fatal error. The work is deferred to the dispatcher,
    /// where a failure is logged instead of killing the application.
    /// </remarks>
    /// <param name="action">Action to run.</param>
    private void RunOnDispatcher(Action action)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Log.LogCritical(exception, "The command could not be executed.");
            }
        }));
    }
}
