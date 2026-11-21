using System.Text.Json.Serialization;
using MediaPilot.Core;

namespace MediaPilot.Configuration;

/// <summary>
/// Serialization context generated at compile time.
/// </summary>
/// <remarks>
/// Automatic generation avoids reflection at runtime and allows trimming to
/// keep the serialization metadata of the settings types.
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(HotkeyBinding[]))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
