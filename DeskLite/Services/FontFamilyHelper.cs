using System.Windows;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfFonts = System.Windows.Media.Fonts;

namespace DeskLite.Services;

public static class FontFamilyHelper
{
    public const string DefaultFamilyName = "Microsoft YaHei UI";

    private static readonly string[] PreferredFamilies =
    [
        DefaultFamilyName,
        "Segoe UI",
        "Microsoft YaHei",
        "SimHei",
        "PingFang SC",
        "Noto Sans SC",
        "Source Han Sans SC",
        "Arial",
        "Tahoma"
    ];

    public static WpfFontFamily Resolve(string? familyName)
    {
        foreach (var candidate in EnumerateCandidates(familyName))
        {
            if (IsAvailable(candidate))
            {
                return new WpfFontFamily(candidate);
            }
        }

        return new WpfFontFamily(DefaultFamilyName);
    }

    public static string ResolveName(string? familyName) => Resolve(familyName).Source;

    public static IReadOnlyList<string> GetSelectableFamilies()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in PreferredFamilies)
        {
            if (IsAvailable(name))
            {
                set.Add(name);
            }
        }

        foreach (var family in WpfFonts.SystemFontFamilies.Select(f => f.Source).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            set.Add(family);
        }

        return set.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void Apply(Window window, string? familyName)
    {
        window.FontFamily = Resolve(familyName);
    }

    private static IEnumerable<string> EnumerateCandidates(string? familyName)
    {
        if (!string.IsNullOrWhiteSpace(familyName))
        {
            yield return familyName.Trim();
        }

        yield return DefaultFamilyName;
        yield return "Segoe UI";
    }

    private static bool IsAvailable(string familyName)
    {
        try
        {
            _ = new WpfFontFamily(familyName);
            return WpfFonts.SystemFontFamilies.Any(f =>
                string.Equals(f.Source, familyName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
