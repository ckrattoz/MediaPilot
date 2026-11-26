using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// Builds the sheet that compares the candidate geometries of the small sizes.
/// </summary>
/// <remarks>
/// The legibility of the mark at 16 pixels is the risk number one of the
/// identity: the window of the notification area is 16 pixels wide, and a
/// detail that is thinner than a pixel does not survive there. This sheet
/// exists to measure that risk instead of guessing it.
/// </remarks>
internal static class ShapeSheet
{
    /// <summary>
    /// Builds the sheet of the candidate geometries.
    /// </summary>
    /// <param name="shapes">The candidates to compare.</param>
    /// <returns>The bytes of the PNG file.</returns>
    public static byte[] Build(IReadOnlyList<(string Name, BrandShape Shape, bool DrawTile)> shapes)
    {
        const double margin = 32;
        const double cell = 128;
        const int zoom = 8;
        const double headerHeight = 130;
        const double rowHeight = cell + 40;

        var width = margin + 280 + (cell * 2) + margin;
        var height = headerHeight + (shapes.Count * rowHeight) + margin;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF4)),
                null,
                new Rect(0, 0, width, height));

            DrawText(context, "MediaPilot · geometria de la onda a 16 px", margin, 30, 26, Color.FromRgb(0x1B, 0x1B, 0x1F), bold: true);
            DrawText(
                context,
                "Cada variante se dibuja con su propia forma y se rasteriza a 16 px reales, ampliados x8 sin filtrado.",
                margin,
                68,
                14,
                Color.FromRgb(0x60, 0x60, 0x68));
            DrawText(context, "16 px x8", margin + 280 + 12, 98, 13, Color.FromRgb(0x60, 0x60, 0x68));

            var y = headerHeight;

            foreach (var (name, shape, drawTile) in shapes)
            {
                DrawText(context, name, margin, y + 6, 16, Color.FromRgb(0x1B, 0x1B, 0x1F), bold: true);
                DrawText(
                    context,
                    Describe(shape),
                    margin,
                    y + 30,
                    12,
                    Color.FromRgb(0x60, 0x60, 0x68));

                var icon = IconRenderer.Render(BrandPalette.Active, 16, shape, drawTile);
                DrawMagnified(context, icon, margin + 280, y + 16, zoom);

                y += rowHeight;
            }
        }

        return Encode(visual, (int)width, (int)height);
    }

    /// <summary>
    /// Describes the measurements of a shape.
    /// </summary>
    /// <param name="shape">The shape to describe.</param>
    /// <returns>The description of the shape.</returns>
    private static string Describe(BrandShape shape)
    {
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "barra {0:0} · hueco {1:0} · triangulo {2:0}x{3:0} · alto barra {4:0} · desfase {5:0} · punta {6:0}",
            shape.BarWidth,
            shape.BarGap,
            shape.TriangleWidth,
            shape.TriangleHeight,
            shape.SideBarHeight,
            shape.SideBarOffset,
            shape.TipTruncation);
    }

    private static void DrawMagnified(DrawingContext context, BitmapSource source, double x, double y, int factor)
    {
        var size = source.PixelWidth;
        var stride = size * 4;
        var pixels = new byte[stride * size];
        source.CopyPixels(pixels, stride, 0);

        for (var py = 0; py < size; py++)
        {
            for (var px = 0; px < size; px++)
            {
                var offset = (py * stride) + (px * 4);
                var color = Color.FromArgb(
                    pixels[offset + 3],
                    pixels[offset + 2],
                    pixels[offset + 1],
                    pixels[offset]);

                context.DrawRectangle(
                    new SolidColorBrush(color),
                    null,
                    new Rect(x + (px * factor), y + (py * factor), factor, factor));
            }
        }
    }

    private static void DrawText(
        DrawingContext context,
        string text,
        double x,
        double y,
        double size,
        Color color,
        bool bold = false)
    {
        var typeface = new Typeface(
            new FontFamily("Segoe UI"),
            FontStyles.Normal,
            bold ? FontWeights.SemiBold : FontWeights.Normal,
            FontStretches.Normal);

        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            new SolidColorBrush(color),
            1.0);

        context.DrawText(formatted, new Point(x, y));
    }

    private static byte[] Encode(DrawingVisual visual, int width, int height)
    {
        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));

        using var stream = new System.IO.MemoryStream();
        encoder.Save(stream);

        return stream.ToArray();
    }
}
