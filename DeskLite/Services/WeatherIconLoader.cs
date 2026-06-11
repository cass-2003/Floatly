using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DeskLite.Services;

public static class WeatherIconLoader
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static string IconsDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "WeatherIcons", "fill");

    public static ImageSource? Load(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "partly-cloudy-day";
        }

        if (Cache.TryGetValue(slug, out var cached))
        {
            return cached;
        }

        var path = Path.Combine(IconsDir, $"{slug}.svg");
        if (!File.Exists(path))
        {
            path = Path.Combine(IconsDir, "partly-cloudy-day.svg");
            if (!File.Exists(path))
            {
                return null;
            }
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
            Cache[slug] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }
}
