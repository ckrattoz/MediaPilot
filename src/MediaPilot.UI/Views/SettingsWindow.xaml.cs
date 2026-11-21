using System.Windows;
using System.Windows.Interop;
using MediaPilot.Core;
using MediaPilot.Platform.Interop;
using MediaPilot.UI.ViewModels;

namespace MediaPilot.UI.Views;

/// <summary>
/// MediaPilot settings panel.
/// </summary>
public partial class SettingsWindow : Window
{
    private bool _syncingTheme;

    /// <summary>
    /// Initializes the window.
    /// </summary>
    /// <param name="viewModel">Settings state.</param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;

        SyncThemeSelector(viewModel.Theme);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        DwmWindowHelper.TrySetCornerPreference(handle, DwmWindowHelper.CornerPreference.Round);
    }

    private void SyncThemeSelector(ThemePreference preference)
    {
        _syncingTheme = true;
        try
        {
            var index = preference switch
            {
                ThemePreference.Light => 1,
                ThemePreference.Dark => 2,
                _ => 0,
            };

            ThemeSelector.SelectedIndex = index;
        }
        finally
        {
            _syncingTheme = false;
        }
    }

    private void OnThemeSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_syncingTheme || DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var tag = (ThemeSelector.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString();

        viewModel.Theme = tag switch
        {
            "Light" => ThemePreference.Light,
            "Dark" => ThemePreference.Dark,
            _ => ThemePreference.Auto,
        };
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
        }

        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.CancelCommand.Execute(null);
        }

        Close();
    }
}
