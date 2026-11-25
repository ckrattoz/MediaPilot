using System.Windows;
using System.Windows.Media;

namespace MediaPilot.Brand;

/// <summary>
/// The geometric description of the MediaPilot mark.
/// </summary>
/// <remarks>
/// <para>
/// The mark is the concept "the tile and the wave": a rounded square in the
/// visual language of the Windows 11 icons, with an audio wave of three bars
/// inside it. The central bar is a triangle that points to the right, which
/// keeps the association with playback without repeating the loose play
/// triangle of the previous icon.
/// </para>
/// <para>
/// Every measurement is expressed in a 256 by 256 design space, the same
/// space the master is rasterized from. The geometry is resolution
/// independent: the generator scales it, so the proportions of the mark are
/// identical at every size.
/// </para>
/// </remarks>
internal static class BrandGeometry
{
    /// <summary>The side of the square design space.</summary>
    public const double CanvasSize = 256.0;

    /// <summary>The corner radius of the tile, as a fraction of the canvas.</summary>
    private const double TileRadiusRatio = 0.225;

    /// <summary>
    /// Builds the rounded square of the tile.
    /// </summary>
    /// <returns>
    /// The outline of the tile: a rounded square that fills the design space.
    /// </returns>
    public static Geometry BuildTile()
    {
        var radius = CanvasSize * TileRadiusRatio;

        return new RectangleGeometry(
            new Rect(0, 0, CanvasSize, CanvasSize),
            radius,
            radius);
    }

    /// <summary>
    /// Builds the two side bars of the wave.
    /// </summary>
    /// <param name="shape">The measurements of the mark.</param>
    /// <returns>The outline of the side bars.</returns>
    public static Geometry BuildBars(BrandShape shape)
    {
        var group = new GeometryGroup();
        var center = CanvasSize / 2.0;

        group.Children.Add(BuildBar(
            center - (shape.TriangleWidth / 2.0) - shape.BarGap - (shape.BarWidth / 2.0),
            -shape.SideBarOffset,
            shape));

        group.Children.Add(BuildBar(
            center + (shape.TriangleWidth / 2.0) + shape.BarGap + (shape.BarWidth / 2.0),
            shape.SideBarOffset,
            shape));

        return group;
    }

    /// <summary>
    /// Builds the central triangle of the wave, pointing to the right.
    /// </summary>
    /// <param name="shape">The measurements of the mark.</param>
    /// <returns>The outline of the triangle.</returns>
    public static Geometry BuildTriangle(BrandShape shape)
    {
        var center = CanvasSize / 2.0;
        var halfHeight = shape.TriangleHeight / 2.0;
        var left = center - (shape.TriangleWidth / 2.0);
        var right = center + (shape.TriangleWidth / 2.0);

        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(left, center - halfHeight),
            IsClosed = true,
            IsFilled = true,
        };

        // A triangle with a sharp point loses its tip when the mark is
        // rasterized at 16 pixels: the point is thinner than a pixel and the
        // antialiasing fades it away. The small sizes cut the tip, so the
        // gesture keeps a readable edge.
        var tip = Math.Min(shape.TipTruncation, (shape.TriangleWidth / 2.0) - 1.0);

        if (tip > 0)
        {
            var half = tip / 2.0;

            figure.Segments.Add(new LineSegment(new Point(right - tip, center - half), true));
            figure.Segments.Add(new LineSegment(new Point(right, center), true));
            figure.Segments.Add(new LineSegment(new Point(right - tip, center + half), true));
        }
        else
        {
            figure.Segments.Add(new LineSegment(new Point(right, center), true));
        }

        figure.Segments.Add(new LineSegment(new Point(left, center + halfHeight), true));

        geometry.Figures.Add(figure);

        return geometry;
    }

    /// <summary>
    /// Builds a vertical bar with rounded ends.
    /// </summary>
    /// <param name="centerX">The horizontal center of the bar.</param>
    /// <param name="centerYOffset">The vertical offset of the bar.</param>
    /// <param name="shape">The measurements of the mark.</param>
    /// <returns>The outline of the bar.</returns>
    private static RectangleGeometry BuildBar(double centerX, double centerYOffset, BrandShape shape)
    {
        var radius = shape.BarWidth / 2.0;
        var centerY = (CanvasSize / 2.0) + centerYOffset;

        return new RectangleGeometry(
            new Rect(centerX - radius, centerY - (shape.SideBarHeight / 2.0), shape.BarWidth, shape.SideBarHeight),
            radius,
            radius);
    }
}
