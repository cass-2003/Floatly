using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

public static class FontColorHelper
{
    public static readonly string[] PresetHexColors =
    [
        "#F5F7FA",
        "#E8ECF1",
        "#B8C0CC",
        "#3B82F6",
        "#60A5FA",
        "#22C55E",
        "#F59E0B",
        "#F97316",
        "#EF4444",
        "#EC4899",
        "#A78BFA",
        "#0F172A"
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
}
