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
    public static WpfColor PanelBackground => WpfColor.FromArgb(0xFF, 0x10, 0x22, 0x39);
    public static WpfColor PanelGlow => WpfColor.FromArgb(0xFF, 0x24, 0x4E, 0x78);
    public static WpfColor CardBackground => WpfColor.FromArgb(0xA6, 0x1D, 0x34, 0x50);
    public static WpfColor CardBackgroundDeep => WpfColor.FromArgb(0xC4, 0x0D, 0x20, 0x37);
    public static WpfColor ContentBackdrop => WpfColor.FromArgb(0x40, 0x0C, 0x1D, 0x31);
    public static WpfColor ToolbarBackground => WpfColor.FromArgb(0x96, 0x16, 0x2A, 0x43);
    public static WpfColor CardBorder => WpfColor.FromArgb(0x2E, 0xE4, 0xF0, 0xFF);
    public static WpfColor AccentBlue => WpfColor.FromRgb(0x5C, 0x8D, 0xFF);
    public static WpfColor AccentOrange => WpfColor.FromRgb(0xFF, 0x8A, 0x72);
    public static WpfColor AccentGreen => WpfColor.FromRgb(0x55, 0xD3, 0x8A);
    public static WpfColor TextPrimary => WpfColor.FromRgb(0xFF, 0xFF, 0xFF);
    public static WpfColor TextSecondary => WpfColor.FromArgb(0xA6, 0xFF, 0xFF, 0xFF);
    public static WpfColor ProgressTrack => WpfColor.FromArgb(0x38, 0xFF, 0xFF, 0xFF);
    public static WpfColor ScratchYellow => WpfColor.FromArgb(0x55, 0x3D, 0x35, 0x20);
    public static WpfColor ScratchBlue => WpfColor.FromArgb(0x44, 0x1A, 0x2A, 0x42);
}
