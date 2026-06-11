using System.IO;

namespace DeskLite.Services;

public static class AppConstants
{
    public const string DisplayName = "Floatly";
    public const string DisplayNameChinese = "浮岛";
    public const string Version = "2.0.18";
    public const string AppDataFolderName = "Floatly";
    public const string LegacyAppDataFolderName = "DeskLite";
    public const string BackupFilePrefix = "floatly-backup";
    public const string AutoStartRegistryValueName = "Floatly";
    public const string LegacyAutoStartRegistryValueName = "DeskLite";

    public static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);

    public static string LegacyAppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), LegacyAppDataFolderName);
}
