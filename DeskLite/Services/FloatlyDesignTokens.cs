using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

/// <summary>Floatly 2.0 visual tokens — glassmorphism dashboard.</summary>
public static class FloatlyDesignTokens
{
    public const double PanelCornerRadius = 16;
    public const double CardCornerRadius = 14;
    public const double CardPadding = 14;
    public const double ModuleGap = 12;
    public const double ClockFontSize = 36;
    public const double DateFontSize = 12;
    public const double CardTitleFontSize = 13;
    public const double BodyFontSize = 12;

    public static WpfColor Background => WpfColor.FromRgb(0x13, 0x17, 0x22);
    public static WpfColor PanelBackground => WpfColor.FromArgb(0xD9, 0x13, 0x17, 0x22);
    public static WpfColor CardBackground => WpfColor.FromArgb(0xB8, 0x16, 0x19, 0x22);
    public static WpfColor CardBorder => WpfColor.FromArgb(0x0D, 0xFF, 0xFF, 0xFF);
    public static WpfColor AccentBlue => WpfColor.FromRgb(0x5C, 0x8D, 0xFF);
    public static WpfColor AccentOrange => WpfColor.FromRgb(0xFF, 0x8A, 0x72);
    public static WpfColor AccentGreen => WpfColor.FromRgb(0x55, 0xD3, 0x8A);
    public static WpfColor TextPrimary => WpfColor.FromRgb(0xFF, 0xFF, 0xFF);
    public static WpfColor TextSecondary => WpfColor.FromArgb(0xA6, 0xFF, 0xFF, 0xFF);
    public static WpfColor ProgressTrack => WpfColor.FromArgb(0x28, 0xFF, 0xFF, 0xFF);
}
