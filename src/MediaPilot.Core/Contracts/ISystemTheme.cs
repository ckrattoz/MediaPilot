namespace MediaPilot.Core.Contracts;

/// <summary>
/// Query for the effective operating system theme.
/// </summary>
public interface ISystemTheme
{
    /// <summary>
    /// Raised when the operating system changes its theme.
    /// </summary>
    event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets a value that indicates whether the effective system theme is dark.
    /// </summary>
    bool IsDark { get; }

    /// <summary>
    /// Resolves the configured preference against the system theme.
    /// </summary>
    /// <param name="preference">User preference.</param>
    /// <returns>The theme that should actually be applied.</returns>
    ThemePreference Resolve(ThemePreference preference);
}
