using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// Rasterizes the MediaPilot mark.
/// </summary>
/// <remarks>
/// The drawing is vector and the rasterization is done by the graphics stack of
/// Windows, so a single master produces every size without losing quality and
/// without a new dependency in the product.
/// </remarks>
internal static class IconRenderer
{
    /// <summary>
    /// The size at or below which the bold shape is used.
    /// </summary>
    private const int BoldShapeThreshold = 24;

    /// <summary>
    /// Renders the mark with the given scheme and size.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <param name="size">The side of the square result, in pixels.</param>
    /// <returns>The rendered icon, frozen and ready to be encoded.</returns>
    public static BitmapSource Render(BrandPalette palette, int size)
    {
        var shape = size <= BoldShapeThreshold ? BrandShape.Bold : BrandShape.Regular;

        return Render(palette, size, shape);
    }

    /// <summary>
    /// Renders the mark with the given scheme, size and measurements.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <param name="size">The side of the square result, in pixels.</param>
    /// <param name="shape">The measurements of the mark.</param>
    /// <returns>The rendered icon, frozen and ready to be encoded.</returns>
    public static BitmapSource Render(BrandPalette palette, int size, BrandShape shape)
    {
        return Render(palette, size, shape, drawTile: true);
    }

    /// <summary>
    /// Renders the mark with the given scheme, size, measurements and tile.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <param name="size">The side of the square result, in pixels.</param>
    /// <param name="shape">The measurements of the mark.</param>
    /// <param name="drawTile">
    /// <see langword="true"/> to draw the rounded square of the tile,
    /// <see langword="false"/> to draw only the wave.
    /// </param>
    /// <returns>The rendered icon, frozen and ready to be encoded.</returns>
    public static BitmapSource Render(BrandPalette palette, int size, BrandShape shape, bool drawTile)
    {
        var drawing = new DrawingGroup();

        if (drawTile)
        {
            drawing.Children.Add(new GeometryDrawing(
                CreateTileBrush(palette),
                null,
                BrandGeometry.BuildTile()));
        }

        drawing.Children.Add(new GeometryDrawing(
            CreateBarBrush(palette),
            null,
            BrandGeometry.BuildBars(shape)));

        drawing.Children.Add(new GeometryDrawing(
            new SolidColorBrush(palette.TriangleColor),
            null,
            BrandGeometry.BuildTriangle(shape)));

        var scale = size / BrandGeometry.CanvasSize;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new ScaleTransform(scale, scale));
            context.DrawDrawing(drawing);
            context.Pop();
        }

        var target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();

        return target;
    }

    /// <summary>
    /// Encodes a rendered icon as a PNG.
    /// </summary>
    /// <param name="icon">The rendered icon.</param>
    /// <returns>The bytes of the PNG file.</returns>
    public static byte[] EncodePng(BitmapSource icon)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(icon));

        using var stream = new MemoryStream();
        encoder.Save(stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Builds the brush of the tile: a soft diagonal gradient.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <returns>The brush of the tile.</returns>
    private static LinearGradientBrush CreateTileBrush(BrandPalette palette)
    {
        var brush = new LinearGradientBrush(
            palette.TileStart,
            palette.TileEnd,
            new Point(0, 0),
            new Point(1, 1));

        brush.Freeze();

        return brush;
    }

    /// <summary>
    /// Builds the brush of the side bars of the wave.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <returns>The brush of the side bars.</returns>
    private static Brush CreateBarBrush(BrandPalette palette)
    {
        if (palette.WaveGradientEnd is null)
        {
            var solid = new SolidColorBrush(palette.BarColor);
            solid.Freeze();

            return solid;
        }

        var gradient = new LinearGradientBrush(
            palette.BarColor,
            palette.WaveGradientEnd.Value,
            new Point(0, 0),
            new Point(1, 0));

        gradient.Freeze();

        return gradient;
    }
}
