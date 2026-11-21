using MediaPilot.Core;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the default hotkey bindings.
/// </summary>
/// <remarks>
/// This set is the functional contract inherited from the original
/// <c>MultimediaHotkeys</c> project: if it changes, the behavior expected by
/// the user changes as well.
/// </remarks>
public class DefaultHotkeysTests
{
    [Fact]
    public void All_ContainsSevenBindings()
    {
        Assert.Equal(7, DefaultHotkeys.All.Length);
    }

    [Theory]
    [InlineData(VirtualKey.Left, ModifierKeys.Control | ModifierKeys.Alt, MediaCommand.Previous)]
    [InlineData(VirtualKey.Down, ModifierKeys.Control | ModifierKeys.Alt, MediaCommand.PlayPause)]
    [InlineData(VirtualKey.Right, ModifierKeys.Control | ModifierKeys.Alt, MediaCommand.Next)]
    [InlineData(VirtualKey.Left, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, MediaCommand.VolumeDown)]
    [InlineData(VirtualKey.Down, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, MediaCommand.VolumeMute)]
    [InlineData(VirtualKey.Right, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, MediaCommand.VolumeUp)]
    [InlineData(VirtualKey.OemPeriod, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, MediaCommand.ShowInformation)]
    public void All_ContainsExpectedBinding(VirtualKey key, ModifierKeys modifiers, MediaCommand command)
    {
        var binding = Assert.Single(
            DefaultHotkeys.All,
            b => b.Command == command && b.Gesture.Key == key);

        Assert.Equal(modifiers, binding.Gesture.Modifiers);
        Assert.True(binding.Gesture.IsValid);
    }

    [Fact]
    public void All_DoesNotRepeatTheSameGesture()
    {
        var gestures = DefaultHotkeys.All.Select(b => b.Gesture).ToArray();

        Assert.Equal(gestures.Length, gestures.Distinct().Count());
    }

    [Fact]
    public void All_ReturnsAnIndependentCopy()
    {
        var first = DefaultHotkeys.All;
        first[0] = new HotkeyBinding(new HotkeyGesture(ModifierKeys.Control, VirtualKey.F1), MediaCommand.Previous);

        // Each call returns a new array: mutating the first one cannot affect
        // the default hotkeys that are handed out next.
        Assert.NotSame(first, DefaultHotkeys.All);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, DefaultHotkeys.All[0].Gesture.Modifiers);
    }
}
