using System.Runtime.InteropServices;
using MediaPilot.Core;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Input;

/// <summary>
/// Sends keystrokes to the system through <c>SendInput</c>.
/// </summary>
/// <remarks>
/// <para>
/// It reproduces the mechanism of the original script (<c>Send "{Media_Next}"</c>
/// and the like), but through the official API instead of the AutoHotkey
/// abstraction.
/// </para>
/// <para>
/// The media keys are injected with their <b>scan code</b>, not with their
/// virtual key. The media key router of Windows watches the scan code path of
/// the keyboard, and an event injected as a virtual key never reaches it: the
/// playback commands arrived in a random way and the change between playing
/// and paused did not happen. The scan code of each key is the one that
/// Windows itself uses, asked through <c>MapVirtualKey</c>.
/// </para>
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

        var inputs = BuildInputs(key);

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return sent == inputs.Length;
    }

    /// <summary>
    /// Builds the events that send a key.
    /// </summary>
    /// <param name="key">Virtual key to send.</param>
    /// <returns>The key press and the key release.</returns>
    internal static INPUT[] BuildInputs(VirtualKey key)
    {
        var scanCode = NativeMethods.MapVirtualKey((uint)key, User32.MapVkToVsc);

        if (scanCode == 0)
        {
            // The key has no scan code in the current layout: it is sent as a
            // virtual key, which is the only thing left to try.
            return
            [
                CreateVirtualKeyInput((ushort)key, User32.KeyEventKeyDown | User32.KeyEventExtended),
                CreateVirtualKeyInput((ushort)key, User32.KeyEventKeyUp | User32.KeyEventExtended),
            ];
        }

        var flags = User32.KeyEventScancode | User32.KeyEventExtended;

        return
        [
            CreateScanCodeInput((ushort)scanCode, flags),
            CreateScanCodeInput((ushort)scanCode, flags | User32.KeyEventKeyUp),
        ];
    }

    private static INPUT CreateScanCodeInput(ushort scanCode, uint flags) => new()
    {
        type = InputKeyboard,
        U = new INPUTUNION
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = scanCode,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = 0,
            },
        },
    };

    private static INPUT CreateVirtualKeyInput(ushort virtualKey, uint flags) => new()
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
