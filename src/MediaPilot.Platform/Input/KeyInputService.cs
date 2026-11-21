using System.Runtime.InteropServices;
using MediaPilot.Core;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Input;

/// <summary>
/// Sends keystrokes to the system through <c>SendInput</c>.
/// </summary>
/// <remarks>
/// It reproduces the mechanism of the original script (<c>Send "{Media_Next}"</c>
/// and the like), but through the official API instead of the AutoHotkey
/// abstraction.
/// </remarks>
public static class KeyInputService
{
    private const uint InputKeyboard = 1;

    /// <summary>
    /// Sends a media or volume key.
    /// </summary>
    /// <param name="key">Virtual key to send.</param>
    /// <returns><see langword="true"/> if the event was accepted by the system.</returns>
    public static bool Send(VirtualKey key)
    {
        if (key == VirtualKey.None)
        {
            return false;
        }

        var inputs = new[]
        {
            CreateKeyInput((ushort)key, User32.KeyEventKeyDown | User32.KeyEventExtended),
            CreateKeyInput((ushort)key, User32.KeyEventKeyUp | User32.KeyEventExtended),
        };

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return sent == inputs.Length;
    }

    private static INPUT CreateKeyInput(ushort virtualKey, uint flags) => new()
    {
        type = InputKeyboard,
        U = new INPUTUNION
        {
            ki = new KEYBDINPUT
            {
                wVk = virtualKey,
                wScan = 0,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = 0,
            },
        },
    };
}
