using System.IO;
using System.Text.Json;
using DeskLite.Models;

namespace DeskLite.Services;

public static class JsonStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskLite");

    private static string DataPath => Path.Combine(DataDir, "data.json");
    private static string SettingsPath => Path.Combine(DataDir, "settings.json");

    public static AppDataFile LoadData()
    {
        EnsureDir();
        if (!File.Exists(DataPath))
        {
            return new AppDataFile();
        }

        try
        {
            var json = File.ReadAllText(DataPath);
            return JsonSerializer.Deserialize<AppDataFile>(json, JsonOptions) ?? new AppDataFile();
        }
        catch
        {
            return new AppDataFile();
        }
    }

    public static void SaveData(AppDataFile data)
    {
        EnsureDir();
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(DataPath, json);
    }

    public static AppSettings LoadSettings()
    {
        EnsureDir();
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            FontScaleHelper.NormalizeFontSettings(settings);
            settings.Opacity = Math.Clamp(settings.Opacity, 0.30, 1.0);
            settings.FontFamily = FontFamilyHelper.ResolveName(settings.FontFamily);
            settings.SkinMode = SkinService.NormalizeMode(settings.SkinMode);
            settings.SkinOverlayOpacity = SkinService.ClampOverlayOpacity(settings.SkinOverlayOpacity);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        EnsureDir();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    private static void EnsureDir()
    {
        Directory.CreateDirectory(DataDir);
    }
}
