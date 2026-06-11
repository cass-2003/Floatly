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

    private static bool _migrationChecked;

    private static string DataDir => AppConstants.AppDataDir;

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
            settings.HotkeyShowHide = HotkeyComboHelper.Sanitize(settings.HotkeyShowHide, HotkeyComboHelper.DefaultShowHide);
            settings.HotkeyQuickTodo = HotkeyComboHelper.Sanitize(settings.HotkeyQuickTodo, HotkeyComboHelper.DefaultQuickTodo);
            if (HotkeyComboHelper.Conflicts(settings.HotkeyShowHide, settings.HotkeyQuickTodo))
            {
                settings.HotkeyQuickTodo = HotkeyComboHelper.DefaultQuickTodo;
            }

            settings.PrimaryTextColor = FontColorHelper.NormalizeHex(settings.PrimaryTextColor);
            settings.WorkStartTime = OffWorkService.TryParseTime(settings.WorkStartTime, out _) ? settings.WorkStartTime : "09:00";
            settings.WorkEndTime = OffWorkService.TryParseTime(settings.WorkEndTime, out _) ? settings.WorkEndTime : "18:00";
            settings.WorkDaysPerMonth = Math.Clamp(settings.WorkDaysPerMonth, 1, 31);
            settings.WorkHoursPerDay = Math.Clamp(settings.WorkHoursPerDay, 1, 24);
            settings.MonthlySalary = Math.Max(0, settings.MonthlySalary);
            settings.ModuleOrder = DeskModuleIds.Normalize(settings.ModuleOrder);
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
        MigrateLegacyDataIfNeeded();
        Directory.CreateDirectory(DataDir);
    }

    private static void MigrateLegacyDataIfNeeded()
    {
        if (_migrationChecked)
        {
            return;
        }

        _migrationChecked = true;

        var legacyDir = AppConstants.LegacyAppDataDir;
        var newDir = AppConstants.AppDataDir;

        if (!Directory.Exists(legacyDir))
        {
            return;
        }

        Directory.CreateDirectory(newDir);

        var hasNewSettings = File.Exists(Path.Combine(newDir, "settings.json"));
        var hasNewData = File.Exists(Path.Combine(newDir, "data.json"));
        if (hasNewSettings || hasNewData)
        {
            return;
        }

        foreach (var fileName in new[] { "settings.json", "data.json", "weather-cache.json" })
        {
            var src = Path.Combine(legacyDir, fileName);
            var dest = Path.Combine(newDir, fileName);
            if (File.Exists(src) && !File.Exists(dest))
            {
                File.Copy(src, dest);
            }
        }

        var legacySkins = Path.Combine(legacyDir, "skins");
        var newSkins = Path.Combine(newDir, "skins");
        if (Directory.Exists(legacySkins) && !Directory.Exists(newSkins))
        {
            CopyDirectory(legacySkins, newSkins);
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(file));
            if (!File.Exists(dest))
            {
                File.Copy(file, dest);
            }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
