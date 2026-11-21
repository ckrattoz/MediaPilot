namespace MediaPilot.Core;

/// <summary>
/// Binding between a keyboard gesture and the media command it executes.
/// </summary>
public sealed class HotkeyBinding
{
    /// <summary>
    /// Initializes a new binding.
    /// </summary>
    /// <param name="gesture">Triggering keyboard gesture.</param>
    /// <param name="command">Command to execute.</param>
    public HotkeyBinding(HotkeyGesture gesture, MediaCommand command)
    {
        Gesture = gesture;
        Command = command;
    }

    /// <summary>Triggering keyboard gesture.</summary>
    public HotkeyGesture Gesture { get; init; }

    /// <summary>Command executed when the gesture is pressed.</summary>
    public MediaCommand Command { get; init; }
}
