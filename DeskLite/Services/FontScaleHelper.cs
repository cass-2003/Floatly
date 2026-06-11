using System.Windows;
using System.Windows.Controls;
using DeskLite.Models;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DeskLite.Services;

public static class FontScaleHelper
{
    public const double BaseFontSizePt = 12;
    public const int MinFontSizePt = 10;
    public const int MaxFontSizePt = 16;

    public static double ClampScale(double scale) => Math.Clamp(scale, 0.85, 1.35);

    public static int ScaleToPt(double scale) =>
        (int)Math.Clamp(Math.Round(ClampScale(scale) * BaseFontSizePt), MinFontSizePt, MaxFontSizePt);

    public static double PtToScale(int pt) =>
        ClampScale(pt / BaseFontSizePt);

    public static int ResolvePt(AppSettings settings) =>
        settings.FontSizePt is >= MinFontSizePt and <= MaxFontSizePt
            ? settings.FontSizePt.Value
            : ScaleToPt(settings.FontScale);

    public static void NormalizeFontSettings(AppSettings settings)
    {
        var pt = ResolvePt(settings);
        settings.FontSizePt = pt;
        settings.FontScale = PtToScale(pt);
    }

    public static void Apply(MainWindow window, double scale)
    {
        scale = ClampScale(scale);

        Set(window.ClockText, FloatlyDesignTokens.ClockFontSize, scale);
        window.ClockText.LineHeight = FloatlyDesignTokens.ClockLineHeight * scale;
        Set(window.ClockSecondsText, FloatlyDesignTokens.ClockSecondsFontSize, scale);
        Set(window.DateText, FloatlyDesignTokens.DateFontSize, scale);
        Set(window.LunarText, 10, scale);
        Set(window.LunarSubText, 9, scale);
        Set(window.YearProgressLabel, 13, scale);
        Set(window.YearProgressPercent, 22, scale);
        Set(window.YearProgressDetail, 10, scale);
        window.YearProgressTrack.Height = FloatlyDesignTokens.ProgressBarHeight * scale;
        Set(window.CityText, 11, scale);
        window.WeatherIconImage.Width = 42 * scale;
        window.WeatherIconImage.Height = 42 * scale;
        window.SunriseIconImage.Width = 14 * scale;
        window.SunriseIconImage.Height = 14 * scale;
        window.SunsetIconImage.Width = 14 * scale;
        window.SunsetIconImage.Height = 14 * scale;
        window.TomorrowIconImage.Width = 13 * scale;
        window.TomorrowIconImage.Height = 13 * scale;
        Set(window.WeatherTempText, 30, scale);
        Set(window.WeatherDescText, 11, scale);
        Set(window.WeatherRangeText, 11, scale);
        Set(window.WeatherFeelsText, 11, scale);
        Set(window.SunriseLineText, 11, scale);
        Set(window.SunsetLineText, 11, scale);
        Set(window.TomorrowLineText, 9, scale);
        Set(window.CountdownLabel, 13, scale);
        Set(window.CountdownDays, 22, scale);
        Set(window.CountdownHint, 10, scale);
        window.CountdownTrack.Height = FloatlyDesignTokens.ProgressBarHeight * scale;
        window.OffWorkTrack.Height = FloatlyDesignTokens.ProgressBarHeight * scale;
        Set(window.PomodoroPhaseText, 13, scale);
        Set(window.PomodoroSessionText, 10, scale);
        Set(window.PomodoroCountdownText, 34, scale);
        Set(window.PomodoroCenterStatusText, 14, scale);
        Set(window.PomodoroStartBtn, 15, scale);
        Set(window.PomodoroHintText, 11, scale);
        Set(window.PomodoroResetBtn, 11, scale);
        Set(window.OffWorkMainText, 36, scale);
        Set(window.OffWorkCaption, 12, scale);
        Set(window.OffWorkModePill, 10, scale);
        Set(window.SalaryAmount, 38, scale);
        Set(window.SalaryPerSecondText, 12, scale);
        Set(window.SalaryHourlyValueText, 11, scale);
        Set(window.SalaryWorkDurationText, 11, scale);
        Set(window.DailyQuoteText, 20, scale);
        Set(window.DailyQuoteSourceText, 12, scale);
        Set(window.ScratchTitleText, 13, scale);
        Set(window.ScratchCountText, 10, scale);
        Set(window.ScratchExpandText, 18, scale);
        Set(window.ScratchPreviewTitle, 11, scale);
        Set(window.ScratchPreviewContent, 10, scale);
        Set(window.ScratchEmptyText, 11, scale);
        Set(window.TodoTitleText, 13, scale);
        Set(window.TodoCountText, 10, scale);
        Set(window.EmptyTodoText, 12, scale);
        Set(window.TodoOverflowText, 11, scale);
        Set(window.TodoViewAllText, 11, scale);
        Set(window.WeekCalendarTitleText, 13, scale);
        Set(window.CalendarTitleText, 13, scale);
        Set(window.NewTodoBox, 12, scale);
        Set(window.AddButton, 12, scale);

        window.CalPrevBtn.FontSize = 14 * scale;
        window.CalNextBtn.FontSize = 14 * scale;
        window.CalWeekModeBtn.FontSize = 11 * scale;
        window.CalMonthModeBtn.FontSize = 11 * scale;

        ApplyHuangLi(window, scale);
    }

    public static double CalendarScale(double fontScale) => ClampScale(fontScale);

    public static double CalSize(double baseSize, double fontScale) => baseSize * CalendarScale(fontScale);

    public static double ScaledSize(double baseSize, double scale) => baseSize * ClampScale(scale);

    private static void ApplyHuangLi(MainWindow window, double scale)
    {
        Set(window.HuangLiSolarDate, 12, scale);
        Set(window.HuangLiLunarLarge, 12, scale);
        window.HuangLiPrevBtn.FontSize = 20 * scale;
        window.HuangLiNextBtn.FontSize = 20 * scale;
        Set(window.HuangLiMetaLineStrip, 10, scale);
        Set(window.HuangLiDetailNavLine, 10, scale);
        Set(window.HuangLiYiCircleText, 9, scale);
        Set(window.HuangLiJiCircleText, 9, scale);
        Set(window.HuangLiYiText, 10, scale);
        Set(window.HuangLiJiText, 10, scale);
        Set(window.HuangLiWuXingLabel, 10, scale);
        Set(window.HuangLiWuXingVal, 11, scale);
        Set(window.HuangLiChongLabel, 10, scale);
        Set(window.HuangLiChongVal, 11, scale);
        Set(window.HuangLiZhiLabel, 10, scale);
        Set(window.HuangLiZhiVal, 11, scale);
        Set(window.HuangLiTimeTitle, 10, scale);
        Set(window.HuangLiJianLabel, 10, scale);
        Set(window.HuangLiJianVal, 11, scale);
        Set(window.HuangLiJiShenLabel, 10, scale);
        Set(window.HuangLiJiShenVal, 10, scale);
        Set(window.HuangLiTaiShenLabel, 10, scale);
        Set(window.HuangLiTaiShenVal, 10, scale);
        Set(window.HuangLiXiongLabel, 10, scale);
        Set(window.HuangLiXiongVal, 10, scale);
        Set(window.HuangLiPengZuLabel, 10, scale);
        Set(window.HuangLiPengZuVal, 10, scale);
        Set(window.HuangLiXiuLabel, 10, scale);
        Set(window.HuangLiXiuVal, 10, scale);
        Set(window.HuangLiCurrentTitle, 12, scale);
        Set(window.HuangLiCurrentZhi, 20, scale);
        Set(window.HuangLiCurrentSummary, 12, scale);
        Set(window.HuangLiCurrentDirections, 10, scale);
        Set(window.HuangLiCurrentLuckText, 10, scale);
        Set(window.HuangLiCurrentYiCircleText, 9, scale);
        Set(window.HuangLiCurrentJiCircleText, 9, scale);
        Set(window.HuangLiCurrentYiText, 11, scale);
        Set(window.HuangLiCurrentJiText, 11, scale);
        Set(window.HuangLiMoreText, 11, scale);

        window.HuangLiYiCircle.Width = window.HuangLiYiCircle.Height = 18 * scale;
        window.HuangLiJiCircle.Width = window.HuangLiJiCircle.Height = 18 * scale;
        window.HuangLiCurrentYiCircle.Width = window.HuangLiCurrentYiCircle.Height = 16 * scale;
        window.HuangLiCurrentJiCircle.Width = window.HuangLiCurrentJiCircle.Height = 16 * scale;
        window.HuangLiCurrentZhiCircle.Width = window.HuangLiCurrentZhiCircle.Height = 40 * scale;
        window.HuangLiCurrentLuckBadge.Width = window.HuangLiCurrentLuckBadge.Height = 20 * scale;
    }

    private static void Set(DependencyObject element, double baseSize, double scale)
    {
        switch (element)
        {
            case TextBlock tb:
                tb.FontSize = baseSize * scale;
                break;
            case WpfTextBox box:
                box.FontSize = baseSize * scale;
                break;
            case WpfButton btn:
                btn.FontSize = baseSize * scale;
                break;
        }
    }
}
