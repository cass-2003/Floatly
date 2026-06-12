using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

/// <summary>Floatly 2.0 visual tokens — glassmorphism dashboard.</summary>
public static class FloatlyDesignTokens
{
    public const double PanelCornerRadius = 16;
    public const double CardCornerRadius = 18;
    public const double CardPadding = 14;
    public const double ModuleGap = 12;
    public const double ClockFontSize = 64;
    public const double ClockLineHeight = 68;
    public const double ClockSecondsFontSize = 20;
    public const double DateFontSize = 12;
    public const double CardTitleFontSize = 13;
    public const double BodyFontSize = 12;
    public const double ProgressBarHeight = 8;
    public const double ProgressBarRadius = 4;

    public static WpfColor Background => WpfColor.FromArgb(0xFF, 0x07, 0x14, 0x25);
    public static WpfColor PanelBackground => WpfColor.FromArgb(0xD8, 0x0A, 0x1B, 0x31);
    public static WpfColor PanelGlow => WpfColor.FromArgb(0xA8, 0x4D, 0x77, 0xA5);
    public static WpfColor CardBackground => WpfColor.FromArgb(0x34, 0xD8, 0xEA, 0xFF);
    public static WpfColor CardBackgroundMid => WpfColor.FromArgb(0x2A, 0x78, 0x9E, 0xC8);
    public static WpfColor CardBackgroundDeep => WpfColor.FromArgb(0x42, 0x09, 0x1A, 0x30);
    public static WpfColor CardHighlight => WpfColor.FromArgb(0x4C, 0xFF, 0xFF, 0xFF);
    public static WpfColor ContentBackdrop => WpfColor.FromArgb(0x16, 0xD8, 0xEA, 0xFF);
    public static WpfColor ToolbarBackground => WpfColor.FromArgb(0x42, 0xE8, 0xF4, 0xFF);
    public static WpfColor CardBorder => WpfColor.FromArgb(0x5C, 0xE4, 0xF0, 0xFF);
    public static WpfColor CardInnerBorder => WpfColor.FromArgb(0x22, 0xFF, 0xFF, 0xFF);
    public static WpfColor AccentBlue => WpfColor.FromRgb(0x5C, 0x8D, 0xFF);
    public static WpfColor AccentOrange => WpfColor.FromRgb(0xFF, 0x8A, 0x72);
    public static WpfColor AccentGreen => WpfColor.FromRgb(0x55, 0xD3, 0x8A);
    public static WpfColor TextPrimary => WpfColor.FromRgb(0xFF, 0xFF, 0xFF);
    public static WpfColor TextSecondary => WpfColor.FromArgb(0xA6, 0xFF, 0xFF, 0xFF);
    public static WpfColor ProgressTrack => WpfColor.FromArgb(0x38, 0xFF, 0xFF, 0xFF);
    public static WpfColor ScratchYellow => WpfColor.FromArgb(0x55, 0x3D, 0x35, 0x20);
    public static WpfColor ScratchBlue => WpfColor.FromArgb(0x44, 0x1A, 0x2A, 0x42);
}
