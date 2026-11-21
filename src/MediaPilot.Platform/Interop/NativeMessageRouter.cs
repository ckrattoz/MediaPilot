namespace MediaPilot.Platform.Interop;

/// <summary>
/// Native Windows message captured by the host window.
/// </summary>
/// <param name="Message">Message code.</param>
/// <param name="WParam">First message parameter.</param>
/// <param name="LParam">Second message parameter.</param>
public readonly record struct NativeMessage(int Message, nint WParam, nint LParam);

/// <summary>
/// Dispatches native Windows messages across the platform services.
/// </summary>
/// <remarks>
/// It exists so that the interop layer does not depend on WPF: the host
/// (the application) creates a hidden window, hooks its window procedure and
/// delivers every message here.
/// </remarks>
public sealed class NativeMessageRouter
{
    private readonly List<Func<NativeMessage, bool>> _handlers = [];

    /// <summary>
    /// Registers a message handler.
    /// </summary>
    /// <param name="handler">
    /// Function that returns <see langword="true"/> if it has processed the message.
    /// </param>
    public void Register(Func<NativeMessage, bool> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers.Add(handler);
    }

    /// <summary>
    /// Delivers a message to the registered handlers.
    /// </summary>
    /// <param name="message">Message to dispatch.</param>
    /// <returns><see langword="true"/> if some handler processed it.</returns>
    public bool Dispatch(NativeMessage message)
    {
        foreach (var handler in _handlers)
        {
            if (handler(message))
            {
                return true;
            }
        }

        return false;
    }
}
