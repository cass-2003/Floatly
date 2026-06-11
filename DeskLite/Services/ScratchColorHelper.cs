using DeskLite.Models;
using WpfColor = System.Windows.Media.Color;

namespace DeskLite.Services;

public static class ScratchColorHelper
{
    public static WpfColor GetCardBackground(string color, ThemeMode theme)
    {
        var isLight = theme == ThemeMode.Light;
        return color switch
        {
            ScratchNoteColors.Yellow => isLight
                ? WpfColor.FromArgb(0x55, 0xFE, 0xF3, 0xC7)
                : WpfColor.FromArgb(0x33, 0xCA, 0x8A, 0x04),
            ScratchNoteColors.Green => isLight
                ? WpfColor.FromArgb(0x55, 0xD1, 0xFA, 0xE5)
                : WpfColor.FromArgb(0x33, 0x16, 0xA3, 0x4A),
            ScratchNoteColors.Blue => isLight
                ? WpfColor.FromArgb(0x55, 0xDB, 0xEA, 0xFE)
                : WpfColor.FromArgb(0x33, 0x25, 0x63, 0xEB),
            ScratchNoteColors.Pink => isLight
                ? WpfColor.FromArgb(0x55, 0xFC, 0xE7, 0xF3)
                : WpfColor.FromArgb(0x33, 0xDB, 0x27, 0x77),
            _ => isLight
                ? WpfColor.FromArgb(0x44, 0xF1, 0xF5, 0xF9)
                : WpfColor.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
        };
    }

    public static WpfColor GetAccentDot(string color) => color switch
    {
        ScratchNoteColors.Yellow => WpfColor.FromRgb(0xCA, 0x8A, 0x04),
        ScratchNoteColors.Green => WpfColor.FromRgb(0x16, 0xA3, 0x4A),
        ScratchNoteColors.Blue => WpfColor.FromRgb(0x25, 0x63, 0xEB),
        ScratchNoteColors.Pink => WpfColor.FromRgb(0xDB, 0x27, 0x77),
        _ => WpfColor.FromRgb(0x94, 0xA3, 0xB8)
    };

    public static string DeriveTitle(string? content, int noteIndex)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var firstLine = content.Split('\n', '\r')[0].Trim();
            if (!string.IsNullOrEmpty(firstLine))
            {
                return firstLine.Length > 32 ? firstLine[..32] + "…" : firstLine;
            }
        }

        return $"便签 {noteIndex}";
    }

    public static string FormatRelativeTime(DateTime dt)
    {
        var diff = DateTime.Now - dt;
        if (diff.TotalMinutes < 1)
        {
            return "刚刚";
        }

        if (diff.TotalHours < 1)
        {
            return $"{(int)diff.TotalMinutes} 分钟前";
        }

        if (diff.TotalDays < 1)
        {
            return $"{(int)diff.TotalHours} 小时前";
        }

        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays} 天前";
        }

        return dt.ToString("M月d日 HH:mm");
    }
}
