using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

public enum ThemeMode
{
    Dark,
    Light
}

public sealed class AppThemePalette
{
    public required WpfColor PanelBackground { get; init; }
    public required WpfColor PanelBorder { get; init; }
    public required WpfColor Divider { get; init; }
    public required WpfColor TextPrimary { get; init; }
    public required WpfColor TextSecondary { get; init; }
    public required WpfColor TextTertiary { get; init; }
    public required WpfColor TextSubtle { get; init; }
    public required WpfColor TextMuted { get; init; }
    public required WpfColor TextEmpty { get; init; }
    public required WpfColor TodoText { get; init; }
    public required WpfColor DeleteButton { get; init; }
    public required WpfColor InputBackground { get; init; }
    public required WpfColor InputBorder { get; init; }
    public required WpfColor InputText { get; init; }
    public required WpfColor WeekLabel { get; init; }
    public required WpfColor WeekSolar { get; init; }
    public required WpfColor WeekLunar { get; init; }
    public WpfColor Accent { get; init; } = WpfColor.FromRgb(0x3B, 0x82, 0xF6);
    public WpfColor Mark { get; init; } = WpfColor.FromRgb(0xF5, 0x9E, 0x0B);
    public WpfColor HuangLiBackground { get; init; } = WpfColor.FromArgb(0x18, 0xFF, 0xFF, 0xFF);
    public WpfColor HuangLiMetaCell { get; init; } = WpfColor.FromArgb(0x14, 0xFF, 0xFF, 0xFF);
    public WpfColor HuangLiYi { get; init; } = WpfColor.FromRgb(0x4A, 0xDE, 0x80);
    public WpfColor HuangLiJi { get; init; } = WpfColor.FromRgb(0xF8, 0x71, 0x71);
    public WpfColor HuangLiYiChipBg { get; init; } = WpfColor.FromArgb(0x22, 0x4A, 0xDE, 0x80);
    public WpfColor HuangLiJiChipBg { get; init; } = WpfColor.FromArgb(0x22, 0xF8, 0x71, 0x71);
    public WpfColor HuangLiLuckGood { get; init; } = WpfColor.FromRgb(0x4A, 0xDE, 0x80);
    public WpfColor HuangLiLuckBad { get; init; } = WpfColor.FromRgb(0xF8, 0x71, 0x71);
    public WpfColor HuangLiTimeCell { get; init; } = WpfColor.FromArgb(0x18, 0xFF, 0xFF, 0xFF);
    public WpfColor HuangLiBadgeBg { get; init; } = WpfColor.FromArgb(0x20, 0x3B, 0x82, 0xF6);

    public static AppThemePalette For(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => Light,
        _ => Dark
    };

    public static ThemeMode Parse(string? value) =>
        string.Equals(value, "light", StringComparison.OrdinalIgnoreCase) ? ThemeMode.Light : ThemeMode.Dark;

    private static readonly AppThemePalette Dark = new()
    {
        PanelBackground = WpfColor.FromArgb(0xE8, 0x18, 0x1C, 0x24),
        PanelBorder = WpfColor.FromArgb(0x40, 0xFF, 0xFF, 0xFF),
        Divider = WpfColor.FromArgb(0x28, 0xFF, 0xFF, 0xFF),
        TextPrimary = WpfColor.FromRgb(0xF5, 0xF7, 0xFA),
        TextSecondary = WpfColor.FromRgb(0xB8, 0xC0, 0xCC),
        TextTertiary = WpfColor.FromRgb(0xC5, 0xD0, 0xE0),
        TextSubtle = WpfColor.FromRgb(0x7E, 0x8A, 0x9C),
        TextMuted = WpfColor.FromRgb(0x8B, 0x95, 0xA5),
        TextEmpty = WpfColor.FromRgb(0x6B, 0x72, 0x80),
        TodoText = WpfColor.FromRgb(0xE8, 0xEC, 0xF1),
        DeleteButton = WpfColor.FromRgb(0x7E, 0x87, 0x96),
        InputBackground = WpfColor.FromRgb(0x25, 0x2A, 0x33),
        InputBorder = WpfColor.FromRgb(0x3A, 0x41, 0x50),
        InputText = WpfColor.FromRgb(0xF3, 0xF4, 0xF6),
        WeekLabel = WpfColor.FromRgb(0x8B, 0x95, 0xA5),
        WeekSolar = WpfColor.FromRgb(0xB8, 0xC0, 0xCC),
        WeekLunar = WpfColor.FromRgb(0x6B, 0x72, 0x80),
        HuangLiBackground = WpfColor.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
        HuangLiMetaCell = WpfColor.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        HuangLiYi = WpfColor.FromRgb(0x4A, 0xDE, 0x80),
        HuangLiJi = WpfColor.FromRgb(0xF8, 0x71, 0x71),
        HuangLiYiChipBg = WpfColor.FromArgb(0x22, 0x4A, 0xDE, 0x80),
        HuangLiJiChipBg = WpfColor.FromArgb(0x22, 0xF8, 0x71, 0x71),
        HuangLiLuckGood = WpfColor.FromRgb(0x4A, 0xDE, 0x80),
        HuangLiLuckBad = WpfColor.FromRgb(0xF8, 0x71, 0x71),
        HuangLiTimeCell = WpfColor.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
        HuangLiBadgeBg = WpfColor.FromArgb(0x20, 0x3B, 0x82, 0xF6)
    };

    private static readonly AppThemePalette Light = new()
    {
        PanelBackground = WpfColor.FromArgb(0xE8, 0xFF, 0xFF, 0xFF),
        PanelBorder = WpfColor.FromArgb(0x40, 0x15, 0x23, 0x42),
        Divider = WpfColor.FromArgb(0x28, 0x15, 0x23, 0x42),
        TextPrimary = WpfColor.FromRgb(0x0F, 0x17, 0x2A),
        TextSecondary = WpfColor.FromRgb(0x47, 0x55, 0x69),
        TextTertiary = WpfColor.FromRgb(0x33, 0x41, 0x55),
        TextSubtle = WpfColor.FromRgb(0x64, 0x74, 0x8B),
        TextMuted = WpfColor.FromRgb(0x64, 0x74, 0x8B),
        TextEmpty = WpfColor.FromRgb(0x94, 0xA3, 0xB8),
        TodoText = WpfColor.FromRgb(0x1E, 0x29, 0x3B),
        DeleteButton = WpfColor.FromRgb(0x94, 0xA3, 0xB8),
        InputBackground = WpfColor.FromRgb(0xF1, 0xF5, 0xF9),
        InputBorder = WpfColor.FromRgb(0xCB, 0xD5, 0xE1),
        InputText = WpfColor.FromRgb(0x0F, 0x17, 0x2A),
        WeekLabel = WpfColor.FromRgb(0x64, 0x74, 0x8B),
        WeekSolar = WpfColor.FromRgb(0x47, 0x55, 0x69),
        WeekLunar = WpfColor.FromRgb(0x94, 0xA3, 0xB8),
        HuangLiBackground = WpfColor.FromArgb(0x12, 0x15, 0x23, 0x42),
        HuangLiMetaCell = WpfColor.FromArgb(0x0C, 0x15, 0x23, 0x42),
        HuangLiYi = WpfColor.FromRgb(0x16, 0xA3, 0x4A),
        HuangLiJi = WpfColor.FromRgb(0xDC, 0x26, 0x26),
        HuangLiYiChipBg = WpfColor.FromArgb(0x18, 0x16, 0xA3, 0x4A),
        HuangLiJiChipBg = WpfColor.FromArgb(0x18, 0xDC, 0x26, 0x26),
        HuangLiLuckGood = WpfColor.FromRgb(0x16, 0xA3, 0x4A),
        HuangLiLuckBad = WpfColor.FromRgb(0xDC, 0x26, 0x26),
        HuangLiTimeCell = WpfColor.FromArgb(0x0C, 0x15, 0x23, 0x42),
        HuangLiBadgeBg = WpfColor.FromArgb(0x14, 0x3B, 0x82, 0xF6)
    };
}
