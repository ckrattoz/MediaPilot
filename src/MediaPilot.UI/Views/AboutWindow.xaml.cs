using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using MediaPilot.Core;
using MediaPilot.Localization;
using MediaPilot.Localization.Resources;
using MediaPilot.Platform.Interop;
using MediaPilot.UI.Resources;

namespace MediaPilot.UI.Views;

/// <summary>
/// "About" window with the shortcut reference and the application details.
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    /// Initializes the window.
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        PopulateShortcuts();
        ShowVersion();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        DwmWindowHelper.TrySetCornerPreference(handle, DwmWindowHelper.CornerPreference.Round);
    }

    private void PopulateShortcuts()
    {
        var panel = new StackPanel();

        foreach (var binding in DefaultHotkeys.All)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var command = new TextBlock
            {
                Text = CommandLabels.Describe(binding.Command),
                Foreground = (System.Windows.Media.Brush)FindResource("MediaPilot.Brush.TextPrimary"),
                FontSize = 13,
            };

            var gesture = new TextBlock
            {
                Text = binding.Gesture.ToString(),
                Foreground = (System.Windows.Media.Brush)FindResource("MediaPilot.Brush.TextSecondary"),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            };

            Grid.SetColumn(gesture, 1);
            row.Children.Add(command);
            row.Children.Add(gesture);
            row.Margin = new Thickness(0, 2, 0, 2);

            panel.Children.Add(row);
        }

        ShortcutsHost.Children.Clear();
        ShortcutsHost.Children.Add(panel);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Shows the version of the running program under the name of the
    /// product.
    /// </summary>
    private void ShowVersion()
    {
        VersionText.Text = LocalizedFormat.Format(
            CultureInfo.CurrentCulture,
            Strings.About_Version,
            ProductVersion.Current);
    }
}
