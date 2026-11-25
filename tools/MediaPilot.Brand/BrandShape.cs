namespace MediaPilot.Brand;

/// <summary>
/// The measurements of the MediaPilot mark.
/// </summary>
/// <param name="BarWidth">The thickness of the side bars of the wave.</param>
/// <param name="BarGap">The horizontal gap between the side bars and the triangle.</param>
/// <param name="TriangleWidth">The width of the central triangle of the wave.</param>
/// <param name="TriangleHeight">The height of the central triangle of the wave.</param>
/// <param name="SideBarHeight">The height of the side bars of the wave.</param>
/// <param name="SideBarOffset">The vertical offset of the side bars from the center.</param>
/// <param name="TipTruncation">
/// The length of the flat cut at the tip of the triangle, in design units. A
/// point that is thinner than a pixel fades away when the mark is rasterized
/// at 16 pixels, so the small sizes cut it.
/// </param>
internal sealed record BrandShape(
    double BarWidth,
    double BarGap,
    double TriangleWidth,
    double TriangleHeight,
    double SideBarHeight,
    double SideBarOffset,
    double TipTruncation = 0)
{
    /// <summary>
    /// The proportions of the mark for the sizes where the details can be
    /// resolved: 32 pixels and above.
    /// </summary>
    public static BrandShape Regular { get; } = new(
        BarWidth: 28.0,
        BarGap: 20.0,
        TriangleWidth: 72.0,
        TriangleHeight: 100.0,
        SideBarHeight: 62.0,
        SideBarOffset: 12.0);

    /// <summary>
    /// The proportions of the mark for the small sizes, where the details of
    /// the regular shape collapse into a blob.
    /// </summary>
    /// <remarks>
    /// A single drawing cannot serve every size: at 16 pixels the gaps of the
    /// regular shape are thinner than a pixel and the wave reads as a smudge.
    /// The bold shape trades the hairline gaps and the rounded ends of the bars
    /// for a silhouette that survives the smallest size of the notification
    /// area, and it cuts the tip of the triangle for the same reason.
    /// </remarks>
    public static BrandShape Bold { get; } = new(
        BarWidth: 26.0,
        BarGap: 30.0,
        TriangleWidth: 76.0,
        TriangleHeight: 112.0,
        SideBarHeight: 62.0,
        SideBarOffset: 16.0,
        TipTruncation: 18.0);
}
