using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DeskLite.Services;

public enum WeatherIconStyle
{
    Line,
    Fill
}

public static class WeatherIconLoader
{
    private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static string IconsDir(WeatherIconStyle style) =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "WeatherIcons",
            style == WeatherIconStyle.Fill ? "fill" : "line");

    public static ImageSource? Load(string slug, WeatherIconStyle style = WeatherIconStyle.Line)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "partly-cloudy-day";
        }

        var cacheKey = $"{style}:{slug}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var dir = IconsDir(style);
        var path = Path.Combine(dir, $"{slug}.svg");
        if (!File.Exists(path))
        {
            path = Path.Combine(dir, "partly-cloudy-day.svg");
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
            Cache[cacheKey] = image;
            return image;
        }
        catch
        {
            return null;
        }
    }
}
