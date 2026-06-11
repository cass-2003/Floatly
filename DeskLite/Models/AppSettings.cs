namespace DeskLite.Models;

public sealed class AppSettings
{
    public List<string> ModuleOrder { get; set; } = [.. DeskModuleIds.DefaultOrder];
    public bool Time24h { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool AutoStart { get; set; }
    public bool ClickThrough { get; set; }
    public bool ShowWeather { get; set; } = true;
    public bool ShowCityName { get; set; } = true;
    public bool AutoLocateCity { get; set; } = true;
    public bool ShowWeekStrip { get; set; } = true;
    public bool ShowHuangLi { get; set; } = true;
    public bool HuangLiCollapsed { get; set; }
    public bool ShowYearProgress { get; set; } = true;
    public bool ShowCountdown { get; set; } = true;
    public bool ShowDailyQuote { get; set; } = true;
    public bool ShowSunriseSunset { get; set; } = true;
    public bool ShowTomorrowWeather { get; set; } = true;
    public bool ShowScratch { get; set; } = true;
    public bool ShowPomodoro { get; set; } = true;
    public int PomodoroWorkMinutes { get; set; } = 25;
    public int PomodoroBreakMinutes { get; set; } = 5;
    public int PomodoroLongBreakMinutes { get; set; } = 15;
    public int PomodoroSessionsBeforeLongBreak { get; set; } = 4;
    public bool ShowTodoReminder { get; set; } = true;
    public bool EnableGlobalHotkey { get; set; } = true;
    public string HotkeyShowHide { get; set; } = "Ctrl+Shift+D";
    public string HotkeyQuickTodo { get; set; } = "Ctrl+Shift+N";
    public string? PrimaryTextColor { get; set; }
    public bool ShowOffWorkCountdown { get; set; } = true;
    public string WorkStartTime { get; set; } = "09:00";
    public string WorkEndTime { get; set; } = "18:00";
    public bool OffWorkWeekdaysOnly { get; set; } = true;
    public bool ShowSalaryHelper { get; set; } = true;
    public decimal MonthlySalary { get; set; } = 10000;
    public int WorkDaysPerMonth { get; set; } = 22;
    public double WorkHoursPerDay { get; set; } = 8;
    public string Theme { get; set; } = "dark";
    public double Opacity { get; set; } = 1.0;
    public string City { get; set; } = "北京";
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 1040;
    public bool UserCustomSize { get; set; }
    public double FontScale { get; set; } = 1.0;
    /// <summary>UI font size in pt (10–16). Null in older settings files — migrate from <see cref="FontScale"/>.</summary>
    public int? FontSizePt { get; set; }
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public string? ResolvedCityName { get; set; }
    public string? ResolvedRegion { get; set; }
    public string? LastAutoLocateAt { get; set; }
    public string CalendarMode { get; set; } = "week";
    public string? CalendarAnchorDate { get; set; }
    /// <summary>default | solid | image | video</summary>
    public string SkinMode { get; set; } = "default";
    public string? SkinImagePath { get; set; }
    public string? SkinVideoPath { get; set; }
    public double SkinOverlayOpacity { get; set; } = 0.55;
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
}
