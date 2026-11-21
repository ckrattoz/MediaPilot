using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MediaPilot.Core;
using MediaPilot.Localization.Resources;

// WPF also declares a ModifierKeys type; the domain one is used here.
using ModifierKeys = MediaPilot.Core.ModifierKeys;

namespace MediaPilot.UI.Controls;

/// <summary>
/// Control that captures a key combination to assign it to a command.
/// </summary>
/// <remarks>
/// It listens to <see cref="UIElement.PreviewKeyDownEvent"/> at the preview
/// level so it can intercept the key before it reaches the active
/// application, and it requires at least one modifier so that a lone key is
/// never hijacked.
/// </remarks>
public sealed class HotkeyCaptureBox : UserControl
{
    /// <summary>Dependency property identifier for <see cref="Gesture"/>.</summary>
    public static readonly DependencyProperty GestureProperty = DependencyProperty.Register(
        nameof(Gesture),
        typeof(HotkeyGesture?),
        typeof(HotkeyCaptureBox),
        new PropertyMetadata(null, OnGestureChanged));

    private readonly Border _frame;
    private readonly TextBlock _label;

    /// <summary>
    /// Initializes the control.
    /// </summary>
    public HotkeyCaptureBox()
    {
        _label = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 13,
        };

        _frame = new Border
        {
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 5, 12, 5),
            MinWidth = 150,
            Background = (Brush)FindResource("MediaPilot.Brush.ControlBackground"),
            BorderBrush = (Brush)FindResource("MediaPilot.Brush.ControlStroke"),
            Child = _label,
        };

        Content = _frame;

        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;
        IsKeyboardFocusWithinChanged += OnFocusChanged;

        UpdateLabel();
    }

    /// <summary>
    /// Gets or sets the currently captured gesture.
    /// </summary>
    public HotkeyGesture? Gesture
    {
        get => (HotkeyGesture?)GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    /// <summary>
    /// Raised when the user captures a new gesture.
    /// </summary>
    public event EventHandler<HotkeyGesture>? GestureCaptured;

    private static void OnGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyCaptureBox box)
        {
            box.UpdateLabel();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = ToVirtualKey(e.Key);
        if (key == VirtualKey.None)
        {
            return;
        }

        var modifiers = ToModifiers(Keyboard.Modifiers);
        if (modifiers == ModifierKeys.None)
        {
            // At least one modifier is required: a lone key would break its
            // normal use everywhere else in the system.
            return;
        }

        var gesture = new HotkeyGesture(modifiers, key);
        Gesture = gesture;
        GestureCaptured?.Invoke(this, gesture);
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void OnFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        _label.Text = Gesture is { } gesture
            ? gesture.ToString()
            : IsKeyboardFocusWithin ? Strings.Hotkey_PressCombination : Strings.Hotkey_Unassigned;

        _frame.BorderBrush = (Brush)FindResource(
            IsKeyboardFocusWithin ? "MediaPilot.Brush.ControlStrokeFocused" : "MediaPilot.Brush.ControlStroke");
    }

    private static ModifierKeys ToModifiers(System.Windows.Input.ModifierKeys modifiers)
    {
        var result = MediaPilot.Core.ModifierKeys.None;

        if ((modifiers & System.Windows.Input.ModifierKeys.Alt) != 0)
        {
            result |= MediaPilot.Core.ModifierKeys.Alt;
        }

        if ((modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
        {
            result |= MediaPilot.Core.ModifierKeys.Control;
        }

        if ((modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
        {
            result |= MediaPilot.Core.ModifierKeys.Shift;
        }

        if ((modifiers & System.Windows.Input.ModifierKeys.Windows) != 0)
        {
            result |= MediaPilot.Core.ModifierKeys.Win;
        }

        return result;
    }

    private static VirtualKey ToVirtualKey(Key key) => key switch
    {
        Key.Left => VirtualKey.Left,
        Key.Right => VirtualKey.Right,
        Key.Up => VirtualKey.Up,
        Key.Down => VirtualKey.Down,
        Key.Space => VirtualKey.Space,
        Key.OemPeriod => VirtualKey.OemPeriod,
        Key.F1 => VirtualKey.F1,
        Key.F2 => VirtualKey.F2,
        Key.F3 => VirtualKey.F3,
        Key.F4 => VirtualKey.F4,
        Key.F5 => VirtualKey.F5,
        Key.F6 => VirtualKey.F6,
        Key.F7 => VirtualKey.F7,
        Key.F8 => VirtualKey.F8,
        Key.F9 => VirtualKey.F9,
        Key.F10 => VirtualKey.F10,
        Key.F11 => VirtualKey.F11,
        Key.F12 => VirtualKey.F12,
        >= Key.D0 and <= Key.D9 => (VirtualKey)((int)VirtualKey.D0 + (key - Key.D0)),
        >= Key.A and <= Key.Z => (VirtualKey)((int)VirtualKey.A + (key - Key.A)),
        _ => VirtualKey.None,
    };
}
