using CommunityToolkit.Mvvm.ComponentModel;
using MediaPilot.Core;
using MediaPilot.UI.Resources;

namespace MediaPilot.UI.ViewModels;

/// <summary>
/// Represents a shortcut-to-command binding in the settings panel.
/// </summary>
public sealed partial class HotkeyBindingViewModel : ObservableObject
{
    /// <summary>
    /// Initializes the view model.
    /// </summary>
    /// <param name="binding">Source binding.</param>
    public HotkeyBindingViewModel(HotkeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        Command = binding.Command;
        _gesture = binding.Gesture;
    }

    /// <summary>Gets the command bound to the shortcut.</summary>
    public MediaCommand Command { get; }

    /// <summary>Gets the readable description of the command.</summary>
    public string Description => CommandLabels.Describe(Command);

    /// <summary>Gets or sets the assigned key gesture, or <see langword="null"/> when there is none.</summary>
    [ObservableProperty]
    private HotkeyGesture? _gesture;

    /// <summary>
    /// Raised when the user captures a new gesture for this row.
    /// </summary>
    public event EventHandler<HotkeyBindingViewModel>? GestureEdited;

    partial void OnGestureChanged(HotkeyGesture? value) => GestureEdited?.Invoke(this, this);
}
