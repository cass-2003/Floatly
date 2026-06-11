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
    public const string ModeVideo = "video";

    public const long MaxVideoBytesSoftWarning = 50L * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".wmv", ".avi", ".mov"
    };

    private static string SkinsDir =>
        Path.Combine(AppConstants.AppDataDir, "skins");

    public static string NormalizeMode(string? mode) => mode switch
    {
        ModeSolid => ModeSolid,
        ModeImage => ModeImage,
        ModeVideo => ModeVideo,
        _ => ModeDefault
    };

    public static string? ImportSkinImage(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var ext = NormalizeExtension(sourcePath, ".png");
        if (!ImageExtensions.Contains(ext))
        {
            return null;
        }

        return CopyToSkinsDir(sourcePath, ext, "skin");
    }

    public static string? ImportSkinVideo(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var ext = NormalizeExtension(sourcePath, ".mp4");
        if (!VideoExtensions.Contains(ext))
        {
            return null;
        }

        return CopyToSkinsDir(sourcePath, ext, "skin_video");
    }

    public static bool ShouldWarnLargeVideo(string sourcePath) =>
        File.Exists(sourcePath) && new FileInfo(sourcePath).Length > MaxVideoBytesSoftWarning;

    public static string? ResolveImagePath(string? storedPath) => ResolveMediaPath(storedPath);

    public static string? ResolveVideoPath(string? storedPath) => ResolveMediaPath(storedPath);

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
        if (mode is ModeSolid or ModeVideo)
        {
            color = WpfColor.FromArgb(0xFF, color.R, color.G, color.B);
        }

        return new SolidColorBrush(color);
    }

    public static bool ShouldShowOverlay(AppSettings settings)
    {
        var mode = NormalizeMode(settings.SkinMode);
        return mode switch
        {
            ModeImage => ResolveImagePath(settings.SkinImagePath) is not null,
            ModeVideo => ResolveVideoPath(settings.SkinVideoPath) is not null,
            _ => false
        };
    }

    public static double ClampOverlayOpacity(double value) => Math.Clamp(value, 0.0, 0.85);

    private static string? ResolveMediaPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return File.Exists(storedPath) ? storedPath : null;
    }

    private static string NormalizeExtension(string sourcePath, string fallback)
    {
        var ext = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(ext))
        {
            return fallback;
        }

        return ext.ToLowerInvariant();
    }

    private static string? CopyToSkinsDir(string sourcePath, string ext, string prefix)
    {
        Directory.CreateDirectory(SkinsDir);
        var fileName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var dest = Path.Combine(SkinsDir, fileName);
        File.Copy(sourcePath, dest, overwrite: true);
        return dest;
    }
}
