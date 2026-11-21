using System.Runtime.InteropServices;

namespace MediaPilot.Platform.Interop;

/// <summary>
/// Application of Desktop Window Manager (DWM) window attributes.
/// </summary>
/// <remarks>
/// It lets WPF windows adopt the native Windows 11 look: dark title bar,
/// rounded corners and Mica material.
/// </remarks>
public static partial class DwmWindowHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaSystemBackdropType = 38;

    private const int DwmsbtAuto = 0;
    private const int DwmsbtNone = 1;
    private const int DwmsbtMainWindow = 2;
    private const int DwmsbtTransientWindow = 3;
    private const int DwmsbtTabbedWindow = 4;

    private const int DwmwcpDefault = 0;
    private const int DwmwcpDoNotRound = 1;
    private const int DwmwcpRound = 2;
    private const int DwmwcpRoundSmall = 3;

    /// <summary>Background material supported by the system.</summary>
    public enum SystemBackdrop
    {
        /// <summary>The system decides.</summary>
        Auto = DwmsbtAuto,

        /// <summary>No material; an opaque background is used.</summary>
        None = DwmsbtNone,

        /// <summary>Mica, for long-lived main windows.</summary>
        Mica = DwmsbtMainWindow,

        /// <summary>Acrylic, for transient windows.</summary>
        Acrylic = DwmsbtTransientWindow,

        /// <summary>Mica Alt, for tabbed windows.</summary>
        MicaAlt = DwmsbtTabbedWindow,
    }

    /// <summary>Corner rounding preference of the window.</summary>
    public enum CornerPreference
    {
        /// <summary>The system decides.</summary>
        Default = DwmwcpDefault,

        /// <summary>No rounding.</summary>
        DoNotRound = DwmwcpDoNotRound,

        /// <summary>Full rounding.</summary>
        Round = DwmwcpRound,

        /// <summary>Small rounding, for auxiliary windows.</summary>
        RoundSmall = DwmwcpRoundSmall,
    }

    /// <summary>
    /// Enables or disables the dark title bar mode.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="enabled">Indicates whether the dark mode is enabled.</param>
    /// <returns><see langword="true"/> if the system accepted the attribute.</returns>
    public static bool TrySetDarkTitleBar(nint windowHandle, bool enabled)
    {
        var value = enabled ? 1 : 0;
        return TrySetAttribute(windowHandle, DwmwaUseImmersiveDarkMode, ref value);
    }

    /// <summary>
    /// Sets the corner rounding preference.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="preference">Rounding preference.</param>
    /// <returns><see langword="true"/> if the system accepted the attribute.</returns>
    public static bool TrySetCornerPreference(nint windowHandle, CornerPreference preference)
    {
        var value = (int)preference;
        return TrySetAttribute(windowHandle, DwmwaWindowCornerPreference, ref value);
    }

    /// <summary>
    /// Applies a system background material (Mica, Acrylic).
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="backdrop">Requested material.</param>
    /// <returns><see langword="true"/> if the system accepted the attribute.</returns>
    /// <remarks>
    /// Requires Windows 11 22H2 or later (build 22621+). On earlier versions
    /// the system ignores the attribute and the window keeps its background.
    /// </remarks>
    public static bool TrySetSystemBackdrop(nint windowHandle, SystemBackdrop backdrop)
    {
        var value = (int)backdrop;
        return TrySetAttribute(windowHandle, DwmwaSystemBackdropType, ref value);
    }

    private static bool TrySetAttribute(nint windowHandle, int attribute, ref int value)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        var hr = DwmSetWindowAttribute(windowHandle, attribute, ref value, sizeof(int));
        return hr == 0;
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
