using MediaPilot.Core;
using MediaPilot.Platform.Hotkeys;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests for the global hotkey service.
/// </summary>
public class WindowsHotkeyServiceTests
{
    [Theory]
    [InlineData(ModifierKeys.None, 0u)]
    [InlineData(ModifierKeys.Alt, 0x0001u)]
    [InlineData(ModifierKeys.Control, 0x0002u)]
    [InlineData(ModifierKeys.Shift, 0x0004u)]
    [InlineData(ModifierKeys.Win, 0x0008u)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt, 0x0003u)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 0x0007u)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Win, 0x000Fu)]
    public void ToWin32Modifiers_TranslatesCorrectly(ModifierKeys modifiers, uint expected)
    {
        Assert.Equal(expected, WindowsHotkeyService.ToWin32Modifiers(modifiers));
    }
}
