using System.Runtime.InteropServices;
using MediaPilot.Core;
using MediaPilot.Platform.Interop;
using MediaPilot.Platform.Input;

namespace MediaPilot.Platform.Tests;

/// <summary>
/// Tests of the structure used to inject the system keys.
/// </summary>
/// <remarks>
/// The playback commands are the only ones that go through
/// <c>SendInput</c>: the volume is handled with MMDevice. A malformed
/// structure made the system reject the whole call with
/// <c>ERROR_INVALID_PARAMETER</c>, and the playback commands silently did
/// nothing.
/// </remarks>
public sealed class KeyInputTests
{
    /// <summary>
    /// Size of the native <c>INPUT</c> structure: 40 bytes on x64 and 28
    /// bytes on x86. The union takes the size of its largest member, which
    /// is <c>MOUSEINPUT</c>.
    /// </summary>
    private static int NativeInputSize => IntPtr.Size == 8 ? 40 : 28;

    [Fact]
    public void TheInputStructureHasTheSizeOfTheNativeOne()
    {
        Assert.Equal(NativeInputSize, Marshal.SizeOf<INPUT>());
    }

    [Fact]
    public void TheSystemAcceptsTheInputStructure()
    {
        // A harmless key: the test is about the structure, not about what the
        // key does.
        var input = new INPUT
        {
            type = 1,
            U = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0x87,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = 0,
                },
            },
        };

        var sent = NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());

        if (sent == 0)
        {
            // A session without an interactive desktop can refuse the
            // injection. What must never happen is the rejection caused by a
            // malformed structure, which is reported as
            // ERROR_INVALID_PARAMETER (87).
            Assert.NotEqual(87, Marshal.GetLastWin32Error());
        }
    }

    [Theory]
    [InlineData(VirtualKey.MediaPlayPause)]
    [InlineData(VirtualKey.MediaNextTrack)]
    [InlineData(VirtualKey.MediaPreviousTrack)]
    public void TheMediaKeysAreInjectedWithTheirScanCode(VirtualKey key)
    {
        var inputs = KeyInputService.BuildInputs(key);

        Assert.Equal(2, inputs.Length);

        foreach (var input in inputs)
        {
            // The scan code is what the media key router of Windows watches:
            // an event injected as a virtual key never reaches it.
            Assert.Equal(0, input.U.ki.wVk);
            Assert.NotEqual(0, input.U.ki.wScan);
            Assert.NotEqual(0u, input.U.ki.dwFlags & User32.KeyEventScancode);
            Assert.NotEqual(0u, input.U.ki.dwFlags & User32.KeyEventExtended);
        }

        // The press and the release carry the same scan code.
        Assert.Equal(inputs[0].U.ki.wScan, inputs[1].U.ki.wScan);
        Assert.NotEqual(0u, inputs[1].U.ki.dwFlags & User32.KeyEventKeyUp);
        Assert.Equal(0u, inputs[0].U.ki.dwFlags & User32.KeyEventKeyUp);
    }
}
