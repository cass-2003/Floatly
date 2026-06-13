using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

public static class FontColorHelper
{
    public static readonly string[] PresetHexColors =
    [
        "#FFFFFF",
        "#4D82FF",
        "#55D38A",
        "#F5A142",
        "#8B5CF6",
        "#D94693"
    ];

    public static WpfColor? TryParseHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return null;
        }

        var value = hex.Trim();
        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        try
        {
            var converted = System.Windows.Media.ColorConverter.ConvertFromString(value);
            return converted is WpfColor color ? color : null;
        }
        catch
        {
            return null;
        }
    }

    public static string? NormalizeHex(string? hex)
    {
        var color = TryParseHex(hex);
        if (color is null)
        {
            return null;
        }

        return $"#{color.Value.R:X2}{color.Value.G:X2}{color.Value.B:X2}";
    }

    public static WpfColor ResolvePrimary(WpfColor themeDefault, string? overrideHex)
    {
        return TryParseHex(overrideHex) ?? themeDefault;
    }

    public static WpfColor ResolvePrimary(WpfColor themeDefault, string? overrideHex, ThemeMode themeMode)
    {
        var custom = TryParseHex(overrideHex);
        if (custom is null)
        {
            return themeDefault;
        }

        var background = themeMode == ThemeMode.Light
            ? WpfColor.FromRgb(0xFF, 0xFF, 0xFF)
            : WpfColor.FromRgb(0x0B, 0x1C, 0x30);

        return ContrastRatio(custom.Value, background) >= 3.0
            ? custom.Value
            : themeDefault;
    }

    private static double ContrastRatio(WpfColor a, WpfColor b)
    {
        var lighter = Math.Max(RelativeLuminance(a), RelativeLuminance(b));
        var darker = Math.Min(RelativeLuminance(a), RelativeLuminance(b));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(WpfColor color)
    {
        static double Channel(byte value)
        {
            var normalized = value / 255.0;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) +
               0.7152 * Channel(color.G) +
               0.0722 * Channel(color.B);
    }
}
