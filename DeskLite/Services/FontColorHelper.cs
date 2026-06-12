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
}
