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

    /// <summary>
    /// Argument that the installer adds to the entry of the automatic start.
    /// </summary>
    /// <remarks>
    /// The installer registers the program in the <c>Run</c> key, because
    /// Windows Installer only removes the registry values that the package
    /// itself wrote. The argument tells the program that the entry that
    /// started it was written by the installer and not by the user, so it can
    /// check the setting of the user before it stays in the notification area.
    /// </remarks>
    public const string InstallerStartupArgument = "--installer-startup";

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

    /// <summary>
    /// Indicates whether the program was started by the entry that the
    /// installer registered.
    /// </summary>
    /// <param name="arguments">Arguments of the process.</param>
    /// <returns>
    /// <see langword="true"/> when the arguments contain the argument of the
    /// installer.
    /// </returns>
    public static bool IsInstallerStartup(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        for (var index = 0; index < arguments.Count; index++)
        {
            if (string.Equals(arguments[index], InstallerStartupArgument, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
