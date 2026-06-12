using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DeskLite.Services;

public static class DashboardIconLoader
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static string IconsDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons", "icon_vjcsvs6hzbr");

    public static ImageSource? Load(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (Cache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(IconsDir, $"{name}.svg");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var settings = new WpfDrawingSettings { IncludeRuntime = true };
            var reader = new FileSvgReader(settings);
            var drawing = reader.Read(path);
            if (drawing is null)
            {
                return null;
            }

            drawing.Freeze();
            var image = new DrawingImage(drawing);
            image.Freeze();
            Cache[name] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }
}
