using System.Text.Json.Serialization;

namespace MediaPilot.Core;

/// <summary>
/// Subset of Windows virtual keys that can be assigned to a command.
/// </summary>
/// <remarks>
/// The values match the Win32 virtual key codes (<c>VK_*</c>) to allow direct
/// translation in the platform layer. They are serialized by name so that the
/// settings file stays readable.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<VirtualKey>))]
public enum VirtualKey
{
    /// <summary>No key assigned.</summary>
    None = 0x00,

    /// <summary>Space bar.</summary>
    Space = 0x20,

    /// <summary>Key <c>0</c>.</summary>
    D0 = 0x30,

    /// <summary>Key <c>1</c>.</summary>
    D1 = 0x31,

    /// <summary>Key <c>2</c>.</summary>
    D2 = 0x32,

    /// <summary>Key <c>3</c>.</summary>
    D3 = 0x33,

    /// <summary>Key <c>4</c>.</summary>
    D4 = 0x34,

    /// <summary>Key <c>5</c>.</summary>
    D5 = 0x35,

    /// <summary>Key <c>6</c>.</summary>
    D6 = 0x36,

    /// <summary>Key <c>7</c>.</summary>
    D7 = 0x37,

    /// <summary>Key <c>8</c>.</summary>
    D8 = 0x38,

    /// <summary>Key <c>9</c>.</summary>
    D9 = 0x39,

    /// <summary>Key <c>A</c>.</summary>
    A = 0x41,

    /// <summary>Key <c>B</c>.</summary>
    B = 0x42,

    /// <summary>Key <c>C</c>.</summary>
    C = 0x43,

    /// <summary>Key <c>D</c>.</summary>
    D = 0x44,

    /// <summary>Key <c>E</c>.</summary>
    E = 0x45,

    /// <summary>Key <c>F</c>.</summary>
    F = 0x46,

    /// <summary>Key <c>G</c>.</summary>
    G = 0x47,

    /// <summary>Key <c>H</c>.</summary>
    H = 0x48,

    /// <summary>Key <c>I</c>.</summary>
    I = 0x49,

    /// <summary>Key <c>J</c>.</summary>
    J = 0x4A,

    /// <summary>Key <c>K</c>.</summary>
    K = 0x4B,

    /// <summary>Key <c>L</c>.</summary>
    L = 0x4C,

    /// <summary>Key <c>M</c>.</summary>
    M = 0x4D,

    /// <summary>Key <c>N</c>.</summary>
    N = 0x4E,

    /// <summary>Key <c>O</c>.</summary>
    O = 0x4F,

    /// <summary>Key <c>P</c>.</summary>
    P = 0x50,

    /// <summary>Key <c>Q</c>.</summary>
    Q = 0x51,

    /// <summary>Key <c>R</c>.</summary>
    R = 0x52,

    /// <summary>Key <c>S</c>.</summary>
    S = 0x53,

    /// <summary>Key <c>T</c>.</summary>
    T = 0x54,

    /// <summary>Key <c>U</c>.</summary>
    U = 0x55,

    /// <summary>Key <c>V</c>.</summary>
    V = 0x56,

    /// <summary>Key <c>W</c>.</summary>
    W = 0x57,

    /// <summary>Key <c>X</c>.</summary>
    X = 0x58,

    /// <summary>Key <c>Y</c>.</summary>
    Y = 0x59,

    /// <summary>Key <c>Z</c>.</summary>
    Z = 0x5A,

    /// <summary>Key <c>F1</c>.</summary>
    F1 = 0x70,

    /// <summary>Key <c>F2</c>.</summary>
    F2 = 0x71,

    /// <summary>Key <c>F3</c>.</summary>
    F3 = 0x72,

    /// <summary>Key <c>F4</c>.</summary>
    F4 = 0x73,

    /// <summary>Key <c>F5</c>.</summary>
    F5 = 0x74,

    /// <summary>Key <c>F6</c>.</summary>
    F6 = 0x75,

    /// <summary>Key <c>F7</c>.</summary>
    F7 = 0x76,

    /// <summary>Key <c>F8</c>.</summary>
    F8 = 0x77,

    /// <summary>Key <c>F9</c>.</summary>
    F9 = 0x78,

    /// <summary>Key <c>F10</c>.</summary>
    F10 = 0x79,

    /// <summary>Key <c>F11</c>.</summary>
    F11 = 0x7A,

    /// <summary>Key <c>F12</c>.</summary>
    F12 = 0x7B,

    /// <summary>Left arrow.</summary>
    Left = 0x25,

    /// <summary>Up arrow.</summary>
    Up = 0x26,

    /// <summary>Right arrow.</summary>
    Right = 0x27,

    /// <summary>Down arrow.</summary>
    Down = 0x28,

    /// <summary>Numeric keypad decimal point (<c>.</c> on a QWERTY keyboard).</summary>
    OemPeriod = 0xBE,

    /// <summary>Volume down key.</summary>
    VolumeDown = 0xAE,

    /// <summary>Volume mute key.</summary>
    VolumeMute = 0xAD,

    /// <summary>Volume up key.</summary>
    VolumeUp = 0xAF,

    /// <summary>Previous media track key.</summary>
    MediaPreviousTrack = 0xB1,

    /// <summary>Play/pause media key.</summary>
    MediaPlayPause = 0xB3,

    /// <summary>Next media track key.</summary>
    MediaNextTrack = 0xB0,
}
