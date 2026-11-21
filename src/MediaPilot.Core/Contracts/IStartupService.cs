namespace MediaPilot.Core.Contracts;

/// <summary>
/// Application registration for automatic startup with Windows.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Indicates whether the application is configured to start with Windows.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables automatic startup.
    /// </summary>
    /// <param name="enabled">Desired state.</param>
    /// <returns><see langword="true"/> if the change was applied.</returns>
    bool TrySetEnabled(bool enabled);
}
