using System.IO;
using System.Windows.Media.Imaging;

namespace MediaPilot.Brand;

/// <summary>
/// The entry point of the brand generator.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="args">
    /// The first argument is the command: <c>sheet</c> writes the review
    /// sheets, <c>assets</c> writes the icon files. The second argument is the
    /// output directory.
    /// </param>
    /// <returns>The exit code of the process.</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: MediaPilot.Brand <sheet|assets> <output-directory>");
            return 1;
        }

        var command = args[0];
        var outputDirectory = Path.GetFullPath(args[1]);

        Directory.CreateDirectory(outputDirectory);

        switch (command)
        {
            case "sheet":
                WriteSheets(outputDirectory);
                return 0;

            case "shapes":
                WriteShapeSheet(outputDirectory);
                return 0;

            case "assets":
                WriteAssets(outputDirectory);
                return 0;

            default:
                Console.Error.WriteLine($"Unknown command: {command}");
                return 1;
        }
    }

    /// <summary>
    /// Writes the review sheets of the mark.
    /// </summary>
    /// <param name="outputDirectory">The directory that receives the files.</param>
    private static void WriteSheets(string outputDirectory)
    {
        var current = LoadCurrentIcon();

        var sheet = BrandSheet.Build(BrandPalette.Candidates, current);
        File.WriteAllBytes(Path.Combine(outputDirectory, "sheet.png"), sheet);

        var legibility = BrandSheet.BuildLegibilitySheet(BrandPalette.Candidates, current);
        File.WriteAllBytes(Path.Combine(outputDirectory, "legibilidad.png"), legibility);

        Console.WriteLine($"Sheet: {Path.Combine(outputDirectory, "sheet.png")}");
        Console.WriteLine($"Legibilidad: {Path.Combine(outputDirectory, "legibilidad.png")}");
    }

    /// <summary>
    /// Writes the sheet that compares the candidate geometries of the small sizes.
    /// </summary>
    /// <param name="outputDirectory">The directory that receives the file.</param>
    private static void WriteShapeSheet(string outputDirectory)
    {
        var shapes = new List<(string, BrandShape, bool)>
        {
            ("Elegida · barra 26, hueco 30, punta 18", BrandShape.Bold, true),
            ("Punta viva, sin truncar", BrandShape.Bold with { TipTruncation = 0 }, true),
            ("Punta truncada 10", BrandShape.Bold with { TipTruncation = 10 }, true),
            ("Sin tile · solo la onda", BrandShape.Bold, false),
            ("Barras altas 68", BrandShape.Bold with { SideBarHeight = 68 }, true),
            ("Barras cortas 52", BrandShape.Bold with { SideBarHeight = 52 }, true),
            ("Sin desplazamiento", BrandShape.Bold with { SideBarOffset = 0 }, true),
            ("Hueco amplio 38", BrandShape.Bold with { BarGap = 38 }, true),
        };

        var sheet = ShapeSheet.Build(shapes);
        File.WriteAllBytes(Path.Combine(outputDirectory, "formas-16px.png"), sheet);

        Console.WriteLine($"Formas: {Path.Combine(outputDirectory, "formas-16px.png")}");
    }

    /// <summary>
    /// Writes the icon files of every candidate.
    /// </summary>
    /// <param name="outputDirectory">The directory that receives the files.</param>
    private static void WriteAssets(string outputDirectory)
    {
        foreach (var palette in BrandPalette.Candidates)
        {
            var ico = IcoWriter.Build(palette);
            File.WriteAllBytes(Path.Combine(outputDirectory, $"{palette.Id}.ico"), ico);

            foreach (var size in new[] { 16, 32, 256 })
            {
                var png = IconRenderer.EncodePng(IconRenderer.Render(palette, size));
                File.WriteAllBytes(Path.Combine(outputDirectory, $"{palette.Id}-{size}.png"), png);
            }
        }

        var inactive = IcoWriter.Build(BrandPalette.Inactive);
        File.WriteAllBytes(Path.Combine(outputDirectory, "inactive.ico"), inactive);

        Console.WriteLine($"Assets: {outputDirectory}");
    }

    /// <summary>
    /// Loads the icon that the application uses today, when it is available.
    /// </summary>
    /// <returns>
    /// The frames of the current icon, indexed by their size, or
    /// <see langword="null"/> when the file is not found.
    /// </returns>
    private static Dictionary<int, BitmapSource>? LoadCurrentIcon()
    {
        var path = FindRepositoryIcon();

        if (path is null)
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            var frames = new Dictionary<int, BitmapSource>();

            foreach (var frame in decoder.Frames)
            {
                frame.Freeze();
                frames[frame.PixelWidth] = frame;
            }

            return frames;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"The current icon could not be read: {exception.Message}");

            return null;
        }
    }

    /// <summary>
    /// Searches the icon of the application from the working directory upwards.
    /// </summary>
    /// <returns>The full path of the icon, or <see langword="null"/> when it is not found.</returns>
    private static string? FindRepositoryIcon()
    {
        var relative = Path.Combine(
            "src",
            "MediaPilot.App",
            "Resources",
            "Icons",
            "active.ico");

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
