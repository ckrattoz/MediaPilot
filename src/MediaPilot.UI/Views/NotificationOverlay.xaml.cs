using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MediaPilot.Platform.Interop;

namespace MediaPilot.UI.Views;

/// <summary>
/// Floating notification that replaces the legacy balloon of AutoHotkey.
/// </summary>
/// <remarks>
/// It is presented as a borderless window with system rounded corners, and it
/// destroys itself after a short time. It does not use transparency because
/// transparent windows do not support the DWM materials.
/// </remarks>
public partial class NotificationOverlay : Window
{
    private const double OverlayMargin = 12;

    /// <summary>
    /// Initializes the notification window.
    /// </summary>
    public NotificationOverlay()
    {
        InitializeComponent();

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.Height;
        Width = 320;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        DwmWindowHelper.TrySetCornerPreference(handle, DwmWindowHelper.CornerPreference.Round);
    }

    /// <summary>
    /// Shows a notification for the given amount of time.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body.</param>
    /// <param name="duration">Time visible before closing.</param>
    public void Show(string title, string message, TimeSpan duration)
    {
        TitleText.Text = title;
        MessageText.Text = message;

        // The position is computed on Loaded, not before: until the first
        // layout the real size of the window is unknown and the notification
        // would be placed off screen.
        Loaded += (_, _) => PositionAtBottomRight();

        Show();
        Activate();

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = duration,
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };

        timer.Start();
    }

    private void PositionAtBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - OverlayMargin;
        Top = workArea.Bottom - ActualHeight - OverlayMargin;
    }
}
