using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// Builds the images of the installation wizard.
/// </summary>
/// <remarks>
/// <para>
/// The WixUI extension shows two images: the canvas of the welcome page, which
/// is 370 by 234 pixels, and the band at the top of every other page, which is
/// 370 by 44 pixels. Both are referenced through overridable strings that
/// point to a key of the binary table, so they can be replaced with the
/// images of the brand without forking the extension.
/// </para>
/// <para>
/// The control that shows them does not accept PNG, only bitmap, so the
/// images are written as uncompressed 32 bit bitmaps.
/// </para>
/// </remarks>
internal static class InstallerArtwork
{
    /// <summary>The width of the canvas of the welcome page, in pixels.</summary>
    public const int DialogWidth = 370;

    /// <summary>The height of the canvas of the welcome page, in pixels.</summary>
    public const int DialogHeight = 234;

    /// <summary>The width of the band of the pages, in pixels.</summary>
    public const int BannerWidth = 370;

    /// <summary>The height of the band of the pages, in pixels.</summary>
    public const int BannerHeight = 44;

    /// <summary>
    /// Builds the canvas of the welcome page.
    /// </summary>
    /// <returns>The bytes of the bitmap file.</returns>
    public static byte[] BuildWelcome()
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0x17, 0x1A, 0x20)),
                null,
                new Rect(0, 0, DialogWidth, DialogHeight));

            // The mark, centered at the top of the canvas.
            var mark = IconRenderer.Render(BrandPalette.Active, 84);
            context.DrawImage(mark, new Rect((DialogWidth - 84) / 2.0, 22, 84, 84));

            var typeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.SemiBold,
                FontStretches.Normal);

            var title = new FormattedText(
                "MediaPilot",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                26,
                new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                1.0);

            context.DrawText(title, new Point((DialogWidth - title.Width) / 2.0, 118));

            var captionTypeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            var caption = new FormattedText(
                "Atajos multimedia globales",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                captionTypeface,
                13,
                new SolidColorBrush(Color.FromRgb(0xA6, 0xA6, 0xA6)),
                1.0);

            context.DrawText(caption, new Point((DialogWidth - caption.Width) / 2.0, 152));

            // The wave of the mark is repeated at the bottom of the canvas as
            // a band of bars: it identifies the product without repeating the
            // logo, and it echoes the mark that is above.
            var accent = new LinearGradientBrush(
                Color.FromRgb(0xFF, 0x8A, 0x2B),
                Color.FromRgb(0xB6, 0xE2, 0x4A),
                new Point(0, 0),
                new Point(1, 0));

            var heights = new[] { 16.0, 30.0, 22.0, 38.0, 18.0, 32.0, 24.0, 40.0, 20.0 };

            for (var i = 0; i < heights.Length; i++)
            {
                var x = 22 + (i * 38);
                var height = heights[i];
                var bar = new Rect(x, DialogHeight - 22 - height, 16, height);

                context.DrawRectangle(accent, null, bar);
            }
        }

        return EncodeBitmap(visual, DialogWidth, DialogHeight);
    }

    /// <summary>
    /// Builds the band at the top of the pages.
    /// </summary>
    /// <returns>The bytes of the bitmap file.</returns>
    public static byte[] BuildBanner()
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0x17, 0x1A, 0x20)),
                null,
                new Rect(0, 0, BannerWidth, BannerHeight));

            var accent = new LinearGradientBrush(
                Color.FromRgb(0xFF, 0x8A, 0x2B),
                Color.FromRgb(0xB6, 0xE2, 0x4A),
                new Point(0, 0),
                new Point(1, 0));

            context.DrawRectangle(accent, null, new Rect(0, BannerHeight - 3, BannerWidth, 3));

            var mark = IconRenderer.Render(BrandPalette.Active, 28);
            context.DrawImage(mark, new Rect(14, (BannerHeight - 28) / 2.0, 28, 28));

            var typeface = new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                FontWeights.SemiBold,
                FontStretches.Normal);

            var title = new FormattedText(
                "MediaPilot",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                15,
                new SolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)),
                1.0);

            context.DrawText(title, new Point(52, (BannerHeight - title.Height) / 2.0));
        }

        return EncodeBitmap(visual, BannerWidth, BannerHeight);
    }

    /// <summary>
    /// Encodes a visual as an uncompressed 32 bit bitmap file.
    /// </summary>
    /// <param name="visual">The visual to encode.</param>
    /// <param name="width">The width of the image, in pixels.</param>
    /// <param name="height">The height of the image, in pixels.</param>
    /// <returns>The bytes of the bitmap file.</returns>
    private static byte[] EncodeBitmap(DrawingVisual visual, int width, int height)
    {
        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);

        var stride = width * 4;
        var source = new byte[stride * height];
        target.CopyPixels(source, stride, 0);

        // A bitmap file stores the rows bottom up and the channels in blue,
        // green, red and alpha order.
        var pixels = new byte[stride * height];

        for (var row = 0; row < height; row++)
        {
            var from = (height - 1 - row) * stride;

            for (var x = 0; x < width; x++)
            {
                var s = from + (x * 4);
                var t = (row * stride) + (x * 4);

                pixels[t] = source[s];
                pixels[t + 1] = source[s + 1];
                pixels[t + 2] = source[s + 2];

                // The images of the wizard are painted over an opaque
                // background, so the alpha channel is forced to be complete:
                // the control that shows them does not blend it.
                pixels[t + 3] = 255;
            }
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // The header of the file: the signature, the size of the file, the
        // reserved field of two words and the offset of the pixels, which is
        // the header of the information plus the information itself.
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write((uint)(54 + pixels.Length));
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(54);

        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write((uint)pixels.Length);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(pixels);

        return stream.ToArray();
    }
}
