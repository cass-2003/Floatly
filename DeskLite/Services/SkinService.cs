using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskLite.Models;
using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

public static class SkinService
{
    public const string ModeDefault = "default";
    public const string ModeSolid = "solid";
    public const string ModeImage = "image";

    private static string SkinsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskLite", "skins");

    public static string NormalizeMode(string? mode) => mode switch
    {
        ModeSolid => ModeSolid,
        ModeImage => ModeImage,
        _ => ModeDefault
    };

    public static string? ImportSkinImage(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        Directory.CreateDirectory(SkinsDir);
        var ext = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".png";
        }

        ext = ext.ToLowerInvariant();
        if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp")
        {
            return null;
        }

        var fileName = $"skin_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var dest = Path.Combine(SkinsDir, fileName);
        File.Copy(sourcePath, dest, overwrite: true);
        return dest;
    }

    public static string? ResolveImagePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return File.Exists(storedPath) ? storedPath : null;
    }

    public static ImageBrush? TryCreateImageBrush(string? path)
    {
        path = ResolveImagePath(path);
        if (path is null)
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            return new ImageBrush(bitmap)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }
        catch
        {
            return null;
        }
    }

    public static System.Windows.Media.Brush CreatePanelBackground(AppSettings settings, AppThemePalette palette)
    {
        var mode = NormalizeMode(settings.SkinMode);
        if (mode == ModeImage)
        {
            var brush = TryCreateImageBrush(settings.SkinImagePath);
            if (brush is not null)
            {
                return brush;
            }
        }

        var color = palette.PanelBackground;
        if (mode == ModeSolid)
        {
            color = WpfColor.FromArgb(0xFF, color.R, color.G, color.B);
        }

        return new SolidColorBrush(color);
    }

    public static bool ShouldShowOverlay(AppSettings settings) =>
        NormalizeMode(settings.SkinMode) == ModeImage &&
        ResolveImagePath(settings.SkinImagePath) is not null;

    public static double ClampOverlayOpacity(double value) => Math.Clamp(value, 0.0, 0.85);
}
