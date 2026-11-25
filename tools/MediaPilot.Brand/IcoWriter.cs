using System.IO;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// Packages the rendered icons into an ICO file.
/// </summary>
/// <remarks>
/// <para>
/// The file holds one entry per size. The entry of 256 pixels is stored as a
/// PNG, which keeps the file small and is understood by every version of
/// Windows that the application supports. The smaller entries are stored as
/// uncompressed bitmaps, the format that the shell and the Windows Installer
/// expect for the sizes they display.
/// </para>
/// </remarks>
internal static class IcoWriter
{
    /// <summary>
    /// The sizes packed into the file, in pixels.
    /// </summary>
    private static readonly int[] Sizes = [16, 20, 24, 32, 48, 64, 256];

    /// <summary>
    /// Builds the ICO file of a color scheme.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <returns>The bytes of the ICO file.</returns>
    public static byte[] Build(BrandPalette palette)
    {
        var images = Sizes
            .Select(size => (Size: size, Data: EncodeEntry(palette, size)))
            .ToArray();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WriteHeader(writer, images.Length);
        WriteDirectory(writer, images);

        foreach (var image in images)
        {
            writer.Write(image.Data);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Writes the header of the ICO file.
    /// </summary>
    /// <param name="writer">The writer of the file.</param>
    /// <param name="count">The number of entries.</param>
    private static void WriteHeader(BinaryWriter writer, int count)
    {
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)count);
    }

    /// <summary>
    /// Writes the directory of the ICO file.
    /// </summary>
    /// <param name="writer">The writer of the file.</param>
    /// <param name="images">The entries of the file, with their sizes.</param>
    private static void WriteDirectory(BinaryWriter writer, (int Size, byte[] Data)[] images)
    {
        var offset = 6 + (16 * images.Length);

        foreach (var image in images)
        {
            // A size of 256 is encoded as zero, the format is one byte wide.
            writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
            writer.Write((byte)(image.Size >= 256 ? 0 : image.Size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)image.Data.Length);
            writer.Write((uint)offset);

            offset += image.Data.Length;
        }
    }

    /// <summary>
    /// Encodes the image of one size.
    /// </summary>
    /// <param name="palette">The color scheme of the mark.</param>
    /// <param name="size">The side of the square image, in pixels.</param>
    /// <returns>The bytes of the entry.</returns>
    private static byte[] EncodeEntry(BrandPalette palette, int size)
    {
        var icon = IconRenderer.Render(palette, size);

        return size >= 256 ? EncodePng(icon) : EncodeBitmap(icon);
    }

    /// <summary>
    /// Encodes an image as a PNG.
    /// </summary>
    /// <param name="icon">The rendered icon.</param>
    /// <returns>The bytes of the PNG file.</returns>
    private static byte[] EncodePng(BitmapSource icon)
    {
        return IconRenderer.EncodePng(icon);
    }

    /// <summary>
    /// Encodes an image as the uncompressed bitmap of an ICO entry.
    /// </summary>
    /// <param name="icon">The rendered icon.</param>
    /// <returns>The bytes of the bitmap.</returns>
    private static byte[] EncodeBitmap(BitmapSource icon)
    {
        var size = icon.PixelWidth;
        var stride = size * 4;

        // The source is premultiplied blue, green, red and alpha.
        var source = new byte[stride * size];
        icon.CopyPixels(source, stride, 0);

        // The destination is a separate buffer on purpose: the entry is
        // written bottom up while the source is top down, and sharing the
        // buffer would mix one row with another.
        var destination = new byte[stride * size];

        for (var row = 0; row < size; row++)
        {
            var sourceRow = size - 1 - row;

            for (var x = 0; x < size; x++)
            {
                var from = (sourceRow * stride) + (x * 4);
                var to = (row * stride) + (x * 4);

                var alpha = source[from + 3];

                // The alpha of the entry is straight, so the premultiplied
                // color of the source has to be divided by it.
                destination[to] = Unmultiply(source[from], alpha);
                destination[to + 1] = Unmultiply(source[from + 1], alpha);
                destination[to + 2] = Unmultiply(source[from + 2], alpha);
                destination[to + 3] = alpha;
            }
        }

        var maskStride = ((size + 31) / 32) * 4;
        var mask = new byte[maskStride * size];

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // BITMAPINFOHEADER of an icon: the height counts the mask as well.
        writer.Write(40);
        writer.Write(size);
        writer.Write(size * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write((uint)(destination.Length + mask.Length));
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(destination);
        writer.Write(mask);

        return stream.ToArray();
    }

    /// <summary>
    /// Divides a premultiplied channel by the alpha of the pixel.
    /// </summary>
    /// <param name="channel">The premultiplied value of the channel.</param>
    /// <param name="alpha">The alpha of the pixel.</param>
    /// <returns>The straight value of the channel.</returns>
    private static byte Unmultiply(byte channel, byte alpha)
    {
        if (alpha == 0)
        {
            return 0;
        }

        if (alpha == 255)
        {
            return channel;
        }

        var value = (channel * 255 + (alpha / 2)) / alpha;

        return (byte)Math.Clamp(value, 0, 255);
    }
}
