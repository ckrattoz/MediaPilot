using MediaPilot.Core;
using MediaPilot.Core.Contracts;
using MediaPilot.Platform.Interop;

namespace MediaPilot.Platform.Hotkeys;

/// <summary>
/// Registration of global hotkeys through <c>RegisterHotKey</c>.
/// </summary>
/// <remarks>
/// Unlike the original script, which installed a permanent global keyboard
/// hook, this service delegates the detection of reserved combinations to the
/// system. That reduces idle consumption and avoids interfering with other
/// applications.
/// </remarks>
public sealed class WindowsHotkeyService : IHotkeyService, IDisposable
{
    private const int FirstHotkeyId = 0xA100;

    private readonly nint _windowHandle;
    private readonly Dictionary<HotkeyGesture, int> _registeredGestures = [];
    private readonly Dictionary<int, MediaCommand> _commandsById = [];
    private int _nextId = FirstHotkeyId;

    /// <summary>
    /// Initializes the service and hooks it to the message router.
    /// </summary>
    /// <param name="windowHandle">Window that receives the <c>WM_HOTKEY</c> messages.</param>
    /// <param name="router">Native message router.</param>
    public WindowsHotkeyService(nint windowHandle, NativeMessageRouter router)
    {
        ArgumentNullException.ThrowIfNull(router);
        _windowHandle = windowHandle;
        router.Register(HandleMessage);
    }

    /// <inheritdoc />
    public event EventHandler<MediaCommand>? CommandTriggered;

    /// <inheritdoc />
    public bool Register(HotkeyGesture gesture, MediaCommand command)
    {
        if (!gesture.IsValid)
        {
            return false;
        }

        if (_registeredGestures.ContainsKey(gesture))
        {
            Unregister(gesture);
        }

        var id = _nextId++;
        var registered = NativeMethods.RegisterHotKey(
            _windowHandle,
            id,
            ToWin32Modifiers(gesture.Modifiers),
            (uint)gesture.Key);

        if (!registered)
        {
            return false;
        }

        _registeredGestures[gesture] = id;
        _commandsById[id] = command;
        return true;
    }

    /// <inheritdoc />
    public bool Unregister(HotkeyGesture gesture)
    {
        if (!_registeredGestures.Remove(gesture, out var id))
        {
            return false;
        }

        _commandsById.Remove(id);
        return NativeMethods.UnregisterHotKey(_windowHandle, id);
    }

    /// <inheritdoc />
    public void UnregisterAll()
    {
        foreach (var gesture in _registeredGestures.Keys.ToArray())
        {
            Unregister(gesture);
        }
    }

    /// <inheritdoc />
    public bool IsRegistered(HotkeyGesture gesture)
    {
        return _registeredGestures.ContainsKey(gesture);
    }

    /// <summary>
    /// Translates the domain modifiers into Win32 values.
    /// </summary>
    /// <param name="modifiers">Domain modifiers.</param>
    /// <returns>Win32 modifier mask.</returns>
    internal static uint ToWin32Modifiers(ModifierKeys modifiers)
    {
        uint result = 0;

        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            result |= User32.ModAlt;
        }

        if ((modifiers & ModifierKeys.Control) != 0)
        {
            result |= User32.ModControl;
        }

        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            result |= User32.ModShift;
        }

        if ((modifiers & ModifierKeys.Win) != 0)
        {
            result |= User32.ModWin;
        }

        return result;
    }

    private bool HandleMessage(NativeMessage message)
    {
        if (message.Message != User32.WmHotkey)
        {
            return false;
        }

        var id = (int)message.WParam;
        if (!_commandsById.TryGetValue(id, out var command))
        {
            return false;
        }

        CommandTriggered?.Invoke(this, command);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        UnregisterAll();
        GC.SuppressFinalize(this);
    }
}
