using System.Windows.Media;

namespace MediaPilot.Brand;

/// <summary>
/// A color scheme of the MediaPilot mark.
/// </summary>
/// <param name="Id">The short identifier of the scheme, used in the file names.</param>
/// <param name="Name">The name of the scheme, used in the generated sheets.</param>
/// <param name="Description">A short description of the intent of the scheme.</param>
/// <param name="TileStart">The color of the tile at the top left corner.</param>
/// <param name="TileEnd">The color of the tile at the bottom right corner.</param>
/// <param name="BarColor">The color of the side bars of the wave.</param>
/// <param name="TriangleColor">The color of the central triangle of the wave.</param>
/// <param name="WaveGradientEnd">
/// The end of the gradient of the side bars, or <see langword="null"/> when the
/// side bars are painted with a single color.
/// </param>
internal sealed record BrandPalette(
    string Id,
    string Name,
    string Description,
    Color TileStart,
    Color TileEnd,
    Color BarColor,
    Color TriangleColor,
    Color? WaveGradientEnd = null)
{
    /// <summary>
    /// The scheme of the mark when the application is not playing: everything
    /// is dimmed, so the notification area shows that the pilot is idle.
    /// </summary>
    public static BrandPalette Inactive { get; } = new(
        Id: "inactive",
        Name: "Inactivo",
        Description: "Todo atenuado: la aplicacion esta en reposo.",
        TileStart: Color.FromRgb(0x23, 0x26, 0x2C),
        TileEnd: Color.FromRgb(0x15, 0x17, 0x1B),
        BarColor: Color.FromRgb(0x6E, 0x73, 0x7C),
        TriangleColor: Color.FromRgb(0x6E, 0x73, 0x7C));

    /// <summary>
    /// The catalog of the schemes proposed for the mark.
    /// </summary>
    public static IReadOnlyList<BrandPalette> Candidates { get; } =
    [
        new(
            Id: "a-naranja-lima",
            Name: "A · Naranja y lima sobre grafito",
            Description: "Barras naranjas, triangulo lima.",
            TileStart: Color.FromRgb(0x2E, 0x32, 0x3B),
            TileEnd: Color.FromRgb(0x17, 0x1A, 0x20),
            BarColor: Color.FromRgb(0xFF, 0x8A, 0x2B),
            TriangleColor: Color.FromRgb(0xB6, 0xE2, 0x4A)),
        new(
            Id: "b-lima-carbon",
            Name: "B · Lima sobre carbon",
            Description: "Barras lima, triangulo naranja.",
            TileStart: Color.FromRgb(0x1B, 0x1E, 0x23),
            TileEnd: Color.FromRgb(0x0C, 0x0D, 0x10),
            BarColor: Color.FromRgb(0xC3, 0xEA, 0x4E),
            TriangleColor: Color.FromRgb(0xFF, 0x8A, 0x2B)),
        new(
            Id: "c-degradado",
            Name: "C · Degradado naranja a lima",
            Description: "La onda se tine de naranja a lima.",
            TileStart: Color.FromRgb(0x23, 0x26, 0x2D),
            TileEnd: Color.FromRgb(0x10, 0x12, 0x16),
            BarColor: Color.FromRgb(0xFF, 0x8A, 0x2B),
            TriangleColor: Color.FromRgb(0xC3, 0xEA, 0x4E),
            WaveGradientEnd: Color.FromRgb(0xC3, 0xEA, 0x4E)),
        new(
            Id: "d-tile-naranja",
            Name: "D · Tile naranja, onda en negativo",
            Description: "La tile naranja, la onda oscura encima.",
            TileStart: Color.FromRgb(0xFF, 0x9E, 0x3D),
            TileEnd: Color.FromRgb(0xEE, 0x7A, 0x16),
            BarColor: Color.FromRgb(0x15, 0x17, 0x1C),
            TriangleColor: Color.FromRgb(0x15, 0x17, 0x1C)),
    ];
}
