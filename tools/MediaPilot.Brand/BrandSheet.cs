using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// Builds the contact sheet that presents the candidates of the mark.
/// </summary>
/// <remarks>
/// The sheet is a design artifact, not a product asset: it exists so the mark
/// can be judged with the eyes, at the real sizes and on the real backgrounds,
/// before a single line of the application is touched.
/// </remarks>
internal static class BrandSheet
{
    private const double Margin = 32;
    private const double HeaderHeight = 148;
    private const double RowHeight = 302;

    private const double LabelX = Margin;
    private const double LargeDarkX = 356;
    private const double LargeLightX = 640;
    private const double Size64X = 924;
    private const double Size32DarkX = 1016;
    private const double Size32LightX = 1072;
    private const double Size16DarkX = 1128;
    private const double Size16LightX = 1168;
    private const double SheetWidth = 1236;

    private static readonly Color SheetBackground = Color.FromRgb(0xF2, 0xF2, 0xF4);
    private static readonly Color SheetText = Color.FromRgb(0x1B, 0x1B, 0x1F);
    private static readonly Color SheetMuted = Color.FromRgb(0x60, 0x60, 0x68);
    private static readonly Color PanelDark = Color.FromRgb(0x1F, 0x1F, 0x22);
    private static readonly Color PanelLight = Color.FromRgb(0xFF, 0xFF, 0xFF);

    /// <summary>
    /// Builds the contact sheet of the candidates.
    /// </summary>
    /// <param name="palettes">The candidates to present.</param>
    /// <param name="currentIcon">The frames of the icon that the application uses today, indexed by size.</param>
    /// <returns>The bytes of the PNG file.</returns>
    public static byte[] Build(
        IReadOnlyList<BrandPalette> palettes,
        IReadOnlyDictionary<int, BitmapSource>? currentIcon)
    {
        var rows = palettes.Count + (currentIcon is null ? 0 : 1);
        var height = HeaderHeight + (rows * RowHeight) + Margin;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(SheetBackground), null, new Rect(0, 0, SheetWidth, height));

            DrawHeader(context);
            DrawColumnHeaders(context);

            var y = HeaderHeight;

            if (currentIcon is not null)
            {
                DrawCurrentRow(context, currentIcon, y);
                y += RowHeight;
            }

            foreach (var palette in palettes)
            {
                DrawCandidateRow(context, palette, y);
                y += RowHeight;
            }
        }

        return Encode(visual, (int)SheetWidth, (int)height);
    }

    /// <summary>
    /// Builds the sheet that shows how the mark survives at 16 and 32 pixels.
    /// </summary>
    /// <param name="palettes">The candidates to present.</param>
    /// <param name="currentIcon">The frames of the icon that the application uses today, indexed by size.</param>
    /// <returns>The bytes of the PNG file.</returns>
    public static byte[] BuildLegibilitySheet(
        IReadOnlyList<BrandPalette> palettes,
        IReadOnlyDictionary<int, BitmapSource>? currentIcon)
    {
        const double cell = 168;
        const int zoom = 8;
        const int zoom32 = 4;
        const double labelWidth = 400;

        var rows = palettes.Count + (currentIcon is null ? 0 : 1);
        var width = Margin + labelWidth + (cell * 2) + (cell * 2) + Margin;
        var height = HeaderHeight + (rows * (cell + 42)) + Margin;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(SheetBackground), null, new Rect(0, 0, width, height));

            DrawText(context, "MediaPilot · legibilidad en tamano real", Margin, 34, 26, SheetText, bold: true);
            DrawText(
                context,
                "Cada pixel de la izquierda es un pixel del icono final, ampliado sin filtrado.",
                Margin,
                72,
                15,
                SheetMuted);
            DrawText(
                context,
                "16 px x8",
                Margin + labelWidth + 12,
                108,
                14,
                SheetMuted);
            DrawText(
                context,
                "32 px x4",
                Margin + labelWidth + (cell * 2) + 12,
                108,
                14,
                SheetMuted);

            var y = HeaderHeight;

            if (currentIcon is not null)
            {
                DrawCurrentRowLabel(context, "Estado actual", "El icono generico de hoy.", y);
                DrawMagnified(context, CurrentFrame(currentIcon, 16), Margin + labelWidth, y + 24, zoom);
                DrawMagnified(context, CurrentFrame(currentIcon, 32), Margin + labelWidth + (cell * 2), y + 24, zoom32);
                y += cell + 42;
            }

            foreach (var palette in palettes)
            {
                DrawText(context, palette.Name, Margin, y + 6, 17, SheetText, bold: true);
                DrawText(context, palette.Description, Margin, y + 32, 13, SheetMuted);

                var rendered16 = IconRenderer.Render(palette, 16);
                var rendered32 = IconRenderer.Render(palette, 32);

                DrawMagnified(context, rendered16, Margin + labelWidth, y + 24, zoom);
                DrawMagnified(context, rendered32, Margin + labelWidth + (cell * 2), y + 24, zoom32);

                y += cell + 42;
            }
        }

        return Encode(visual, (int)width, (int)height);
    }

    private static void DrawHeader(DrawingContext context)
    {
        DrawText(context, "MediaPilot · concepto B, la tile y la onda", Margin, 30, 28, SheetText, bold: true);
        DrawText(
            context,
            "Bocetos a 16, 32 y 256 px, sobre fondo oscuro y fondo claro.",
            Margin,
            70,
                15,
            SheetMuted);
        DrawText(
            context,
            "Paleta: naranja, verde lima y gris oscuro. La onda central es un triangulo que apunta a la derecha.",
            Margin,
            94,
            15,
            SheetMuted);
    }

    private static void DrawColumnHeaders(DrawingContext context)
    {
        DrawText(context, "256 px", LargeDarkX, 112, 14, SheetMuted);
        DrawText(context, "256 px", LargeLightX, 112, 14, SheetMuted);
        DrawText(context, "64 px", Size64X, 112, 14, SheetMuted);
        DrawText(context, "32 px", Size32DarkX, 112, 14, SheetMuted);
        DrawText(context, "32 px", Size32LightX, 112, 14, SheetMuted);
        DrawText(context, "16 px", Size16DarkX, 112, 14, SheetMuted);
        DrawText(context, "16 px", Size16LightX, 112, 14, SheetMuted);

        DrawText(context, "oscuro", LargeDarkX, 130, 12, SheetMuted);
        DrawText(context, "claro", LargeLightX, 130, 12, SheetMuted);
        DrawText(context, "oscuro", Size32DarkX, 130, 12, SheetMuted);
        DrawText(context, "claro", Size32LightX, 130, 12, SheetMuted);
        DrawText(context, "oscuro", Size16DarkX, 130, 12, SheetMuted);
        DrawText(context, "claro", Size16LightX, 130, 12, SheetMuted);
    }

    private static void DrawCurrentRow(
        DrawingContext context,
        IReadOnlyDictionary<int, BitmapSource> currentIcon,
        double y)
    {
        DrawCurrentRowLabel(context, "Estado actual", "El circulo azul con el triangulo de play.", y);

        var panel = new Rect(LargeDarkX, y + 18, 256, 256);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(SheetMuted), 1) { DashStyle = new DashStyle(new double[] { 4, 4 }, 0) }, panel);
        DrawText(context, "no hay version de 256 px", LargeDarkX + 40, y + 138, 14, SheetMuted);

        DrawIcon(context, CurrentFrame(currentIcon, 16), 16, Size16DarkX, y + 34, PanelDark);
        DrawIcon(context, CurrentFrame(currentIcon, 32), 32, Size32DarkX, y + 26, PanelDark);
        DrawIcon(context, CurrentFrame(currentIcon, 32), 32, Size32LightX, y + 26, PanelLight);
        DrawIcon(context, CurrentFrame(currentIcon, 16), 16, Size16LightX, y + 34, PanelLight);
    }

    /// <summary>
    /// Returns the frame of the current icon of a size, or an empty one when
    /// the file does not have it.
    /// </summary>
    /// <param name="currentIcon">The frames of the current icon, indexed by size.</param>
    /// <param name="size">The size of the frame to return.</param>
    /// <returns>The frame of the requested size.</returns>
    private static BitmapSource CurrentFrame(IReadOnlyDictionary<int, BitmapSource> currentIcon, int size)
    {
        if (currentIcon.TryGetValue(size, out var frame))
        {
            return frame;
        }

        var placeholder = new DrawingVisual();
        using (var context = placeholder.RenderOpen())
        {
            context.DrawRectangle(
                new SolidColorBrush(SheetMuted),
                null,
                new Rect(0, 0, size, size));
        }

        var target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        target.Render(placeholder);
        target.Freeze();

        return target;
    }

    private static void DrawCurrentRowLabel(DrawingContext context, string title, string description, double y)
    {
        DrawText(context, title, LabelX, y + 6, 17, SheetText, bold: true);
        DrawText(context, description, LabelX, y + 32, 13, SheetMuted);
    }

    private static void DrawCandidateRow(DrawingContext context, BrandPalette palette, double y)
    {
        DrawText(context, palette.Name, LabelX, y + 6, 17, SheetText, bold: true);
        DrawText(context, palette.Description, LabelX, y + 32, 13, SheetMuted);

        DrawSwatches(context, palette, LabelX, y + 66);

        DrawIcon(context, IconRenderer.Render(palette, 256), 256, LargeDarkX, y + 18, PanelDark);
        DrawIcon(context, IconRenderer.Render(palette, 256), 256, LargeLightX, y + 18, PanelLight);

        DrawIcon(context, IconRenderer.Render(palette, 64), 64, Size64X, y + 22, PanelDark);
        DrawIcon(context, IconRenderer.Render(palette, 32), 32, Size32DarkX, y + 38, PanelDark);
        DrawIcon(context, IconRenderer.Render(palette, 32), 32, Size32LightX, y + 38, PanelLight);
        DrawIcon(context, IconRenderer.Render(palette, 16), 16, Size16DarkX, y + 46, PanelDark);
        DrawIcon(context, IconRenderer.Render(palette, 16), 16, Size16LightX, y + 46, PanelLight);
    }

    private static void DrawSwatches(DrawingContext context, BrandPalette palette, double x, double y)
    {
        var colors = new[]
        {
            palette.TileStart,
            palette.TileEnd,
            palette.BarColor,
            palette.TriangleColor,
        };

        for (var i = 0; i < colors.Length; i++)
        {
            var swatch = new Rect(x + (i * 30), y, 24, 24);
            context.DrawRectangle(
                new SolidColorBrush(colors[i]),
                new Pen(new SolidColorBrush(SheetMuted), 1),
                swatch);
        }
    }

    private static void DrawIcon(DrawingContext context, BitmapSource icon, int size, double x, double y, Color panel)
    {
        var padding = 8.0;
        var panelRect = new Rect(x, y, size + (padding * 2), size + (padding * 2));
        context.DrawRectangle(new SolidColorBrush(panel), null, panelRect);
        context.DrawImage(icon, new Rect(x + padding, y + padding, size, size));
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
            CultureInfo.CurrentUICulture,
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

        using var stream = new MemoryStream();
        encoder.Save(stream);

        return stream.ToArray();
    }
}
