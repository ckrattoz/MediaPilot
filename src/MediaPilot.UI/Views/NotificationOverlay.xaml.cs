using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
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

    private readonly DispatcherTimer _dismissTimer;

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

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _dismissTimer.Tick += OnDismissTimerTick;

        // The notification stays anchored to the corner of the screen while
        // its content changes.
        SizeChanged += (_, _) => PositionAtBottomRight();
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
    /// <param name="level">Level to show in the bar, from 0 to 100, or null to hide it.</param>
    public void Show(string title, string message, TimeSpan duration, int? level)
    {
        Update(title, message, duration, level);

        // The position is computed on Loaded, not before: until the first
        // layout the real size of the window is unknown and the notification
        // would be placed off screen.
        Loaded += (_, _) => PositionAtBottomRight();

        Show();
        Activate();
    }

    /// <summary>
    /// Updates the content of a notification that is already on screen.
    /// </summary>
    /// <remarks>
    /// The time on screen starts again, so the notification stays visible
    /// while the value it shows keeps changing.
    /// </remarks>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body.</param>
    /// <param name="duration">Time visible before closing.</param>
    /// <param name="level">Level to show in the bar, from 0 to 100, or null to hide it.</param>
    public void Update(string title, string message, TimeSpan duration, int? level)
    {
        TitleText.Text = title;
        MessageText.Text = message;
        ApplyLevel(level);

        _dismissTimer.Stop();
        _dismissTimer.Interval = duration;
        _dismissTimer.Start();
    }

    /// <summary>
    /// Shows the level of the volume, or hides the bar when there is none.
    /// </summary>
    /// <remarks>
    /// The fill is scaled instead of resized: the bar is the same width as
    /// the track, and the scale is the proportion of the level, so the
    /// notification does not run a new layout pass on every change of the
    /// volume.
    /// </remarks>
    /// <param name="level">Level to show in the bar, from 0 to 100, or null to hide it.</param>
    private void ApplyLevel(int? level)
    {
        if (level is null)
        {
            LevelTrack.Visibility = Visibility.Collapsed;
            return;
        }

        LevelScale.ScaleX = Math.Clamp(level.Value, 0, 100) / 100.0;
        LevelTrack.Visibility = Visibility.Visible;
    }

    private void OnDismissTimerTick(object? sender, EventArgs e)
    {
        _dismissTimer.Stop();
        Close();
    }

    private void PositionAtBottomRight()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - OverlayMargin;
        Top = workArea.Bottom - ActualHeight - OverlayMargin;
    }
}
