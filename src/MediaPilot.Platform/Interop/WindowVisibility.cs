namespace MediaPilot.Platform.Interop;

/// <summary>
/// <c>user32</c> window operations needed by the application layer.
/// </summary>
/// <remarks>
/// It exposes the intent (hiding a window) instead of the direct P/Invoke
/// call. That way <see cref="NativeMethods"/> can remain <c>internal</c> and
/// the public surface of the <c>Platform</c> layer stays bounded.
/// </remarks>
public static class WindowVisibility
{
    /// <summary>
    /// Hides the window without minimizing or activating it.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns><see langword="true"/> if the system confirmed the operation.</returns>
    public static bool Hide(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        return NativeMethods.ShowWindow(windowHandle, User32.SwHide);
    }
}
