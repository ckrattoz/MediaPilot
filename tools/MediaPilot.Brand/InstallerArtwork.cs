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
/// <para>
/// The dialogs draw their title and their description over these images, in
/// the color of the system. The canvas is therefore white, and the mark goes
/// where the dialog does not write: the column on the left of the welcome
/// page, and the end on the right of the band.
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
    /// The left column of the canvas of the welcome page, in pixels.
    /// </summary>
    /// <remarks>
    /// The dialog draws its title and its description over the canvas, from
    /// the column 135 to the column 355. The column on the left is the only
    /// one that stays free, and it is where the mark goes.
    /// </remarks>
    private const int MarkColumn = 135;

    /// <summary>
    /// Builds the canvas of the welcome page.
    /// </summary>
    /// <returns>The bytes of the bitmap file.</returns>
    public static byte[] BuildWelcome()
    {
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            // The canvas is white. The dialog draws its title and its
            // description over it, in the color of the system, and a white
            // canvas is the only one that keeps that text readable with any
            // theme of the operating system.
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, DialogWidth, DialogHeight));

            // The mark, in the column on the left, where the dialog does not
            // draw anything.
            var size = 120;
            var mark = IconRenderer.Render(BrandPalette.Active, size);
            context.DrawImage(mark, new Rect((MarkColumn - size) / 2.0, (DialogHeight - size) / 2.0, size, size));

            // The line of the brand at the bottom of the page.
            context.DrawRectangle(BuildAccent(), null, new Rect(0, DialogHeight - 3, DialogWidth, 3));
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
            // The band is white, like the background of the dialog, so that it
            // is not a stripe of another color on top of the page.
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, BannerWidth, BannerHeight));

            // The line of the brand at the bottom of the band, right above
            // the line that the extension draws.
            context.DrawRectangle(BuildAccent(), null, new Rect(0, BannerHeight - 3, BannerWidth, 3));

            // The mark at the right end, where the extension puts its logo.
            // The title of the dialog ends at the column 215 and the
            // description does not reach the last column.
            var size = 32;
            var mark = IconRenderer.Render(BrandPalette.Active, size);
            context.DrawImage(mark, new Rect(BannerWidth - size - 6, (BannerHeight - size) / 2.0, size, size));
        }

        return EncodeBitmap(visual, BannerWidth, BannerHeight);
    }

    /// <summary>
    /// Builds the gradient of the brand, from the orange to the lime.
    /// </summary>
    /// <returns>The brush of the gradient.</returns>
    private static LinearGradientBrush BuildAccent() => new(
        BrandPalette.BrandOrange,
        BrandPalette.BrandLime,
        new Point(0, 0),
        new Point(1, 0));

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
