using MediaPilot.Core.Contracts;
using Microsoft.Win32;

namespace MediaPilot.Platform.Startup;

/// <summary>
/// Automatic startup through the <c>Run</c> key of the current user.
/// </summary>
/// <remarks>
/// It is the same mechanism used by the original project and it requires no
/// administrator permissions: it only affects the user running the
/// application.
/// </remarks>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MediaPilot";

    private readonly string _executablePath;

    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <param name="executablePath">Path of the application executable.</param>
    public WindowsStartupService(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _executablePath = executablePath;
    }

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public bool TrySetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null)
            {
                return false;
            }

            if (enabled)
            {
                key.SetValue(ValueName, $"\"{_executablePath}\"", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
