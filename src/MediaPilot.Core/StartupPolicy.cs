namespace MediaPilot.Core;

/// <summary>
/// Policy of the entry of the automatic start.
/// </summary>
/// <remarks>
/// The installer registers the program in the <c>Run</c> key of the user,
/// because Windows Installer only removes the registry values that the
/// package itself wrote, and the entry has to disappear with the program.
/// The entry is therefore there even when the user has not asked for the
/// automatic start, and the program has to decide what to do with it when it
/// is started by it.
/// </remarks>
public static class StartupPolicy
{
    /// <summary>
    /// Indicates whether the program has to remove the entry of the automatic
    /// start and end, instead of running normally.
    /// </summary>
    /// <param name="startedByInstaller">
    /// Whether the program was started by the entry that the installer
    /// registered.
    /// </param>
    /// <param name="startWithWindows">
    /// Whether the user enabled the automatic start.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the entry has to be removed and the
    /// program has to end.
    /// </returns>
    public static bool ShouldDisableAndExit(bool startedByInstaller, bool startWithWindows)
    {
        return startedByInstaller && !startWithWindows;
    }
}
