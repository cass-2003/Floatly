namespace DeskLite.Models;

public sealed class AppSettings
{
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
    public bool ShowYearProgress { get; set; }
    public bool ShowCountdown { get; set; } = true;
    public bool ShowDailyQuote { get; set; } = true;
    public bool ShowSunriseSunset { get; set; } = true;
    public bool ShowTomorrowWeather { get; set; } = true;
    public bool ShowScratch { get; set; }
    public bool ShowTodoReminder { get; set; } = true;
    public bool EnableGlobalHotkey { get; set; } = true;
    public string Theme { get; set; } = "dark";
    public double Opacity { get; set; } = 1.0;
    public string City { get; set; } = "北京";
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public string? ResolvedCityName { get; set; }
    public string? ResolvedRegion { get; set; }
    public string? LastAutoLocateAt { get; set; }
    public string CalendarMode { get; set; } = "week";
    public string? CalendarAnchorDate { get; set; }
}
