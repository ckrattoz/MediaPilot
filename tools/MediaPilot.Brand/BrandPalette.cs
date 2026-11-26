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
    /// The color of the brand: the orange of the side bars.
    /// </summary>
    public static Color BrandOrange { get; } = Color.FromRgb(0xFF, 0x8A, 0x2B);

    /// <summary>
    /// The color of the brand: the lime of the central triangle.
    /// </summary>
    public static Color BrandLime { get; } = Color.FromRgb(0xB6, 0xE2, 0x4A);

    /// <summary>
    /// The scheme of the mark of the application.
    /// </summary>
    /// <remarks>
    /// The graphite tile carries the weight of the mark and the orange and the
    /// lime carry the color. The choice was made with the contact sheet of the
    /// concept, at the real sizes and on the real backgrounds.
    /// </remarks>
    public static BrandPalette Active { get; } = new(
        Id: "active",
        Name: "Activo",
        Description: "La tile grafito con las barras naranjas y el triangulo lima.",
        TileStart: Color.FromRgb(0x2E, 0x32, 0x3B),
        TileEnd: Color.FromRgb(0x17, 0x1A, 0x20),
        BarColor: BrandOrange,
        TriangleColor: BrandLime);

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
    /// The schemes that were explored to choose the one of the mark.
    /// </summary>
    /// <remarks>
    /// They are kept so that the decision can be reviewed and reproduced: the
    /// sheet of the concept is generated from this list.
    /// </remarks>
    public static IReadOnlyList<BrandPalette> Explored { get; } =
    [
        Active,
        new(
            Id: "b-lima-carbon",
            Name: "B · Lima sobre carbon",
            Description: "La inversion de A: barras lima y triangulo naranja.",
            TileStart: Color.FromRgb(0x1B, 0x1E, 0x23),
            TileEnd: Color.FromRgb(0x0C, 0x0D, 0x10),
            BarColor: BrandLime,
            TriangleColor: BrandOrange),
        new(
            Id: "c-degradado",
            Name: "C · Degradado naranja a lima",
            Description: "La onda se tine de naranja a lima.",
            TileStart: Color.FromRgb(0x23, 0x26, 0x2D),
            TileEnd: Color.FromRgb(0x10, 0x12, 0x16),
            BarColor: BrandOrange,
            TriangleColor: BrandLime,
            WaveGradientEnd: BrandLime),
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
