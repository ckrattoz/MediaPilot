using MediaPilot.Core;

namespace MediaPilot.Core.Tests;

/// <summary>
/// Tests for the <see cref="HotkeyGesture"/> type.
/// </summary>
public class HotkeyGestureTests
{
    [Fact]
    public void IsValid_RequiresKeyAndModifier()
    {
        Assert.False(new HotkeyGesture(ModifierKeys.None, VirtualKey.A).IsValid);
        Assert.False(new HotkeyGesture(ModifierKeys.Control, VirtualKey.None).IsValid);
        Assert.True(new HotkeyGesture(ModifierKeys.Control, VirtualKey.A).IsValid);
    }

    [Fact]
    public void ToString_OmitsMissingModifiers()
    {
        var gesture = new HotkeyGesture(ModifierKeys.Control | ModifierKeys.Alt, VirtualKey.Left);

        Assert.Equal("Ctrl+Alt+←", gesture.ToString());
    }

    [Fact]
    public void ToString_WithoutKeyReturnsUnassigned()
    {
        Assert.Equal("Sin asignar", new HotkeyGesture(ModifierKeys.Control, VirtualKey.None).ToString());
    }

    [Fact]
    public void DescribeKey_UsesReadableName()
    {
        Assert.Equal("←", HotkeyGesture.DescribeKey(VirtualKey.Left));
        Assert.Equal("Espacio", HotkeyGesture.DescribeKey(VirtualKey.Space));
        Assert.Equal("Silenciar", HotkeyGesture.DescribeKey(VirtualKey.VolumeMute));
    }
}
