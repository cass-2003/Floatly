using System.Windows.Forms;
using DeskLite.Models;
using DeskLite.Services;

namespace DeskLite;

public sealed class TrayService : IDisposable
{
    private static readonly (string Label, double Value)[] OpacityPresets =
    [
        ("不透明 (100%)", 1.0),
        ("轻微透明 (85%)", 0.85),
        ("半透明 (70%)", 0.70),
        ("高透明 (55%)", 0.55)
    ];

    private readonly NotifyIcon _icon;
    private readonly MainWindow _window;
    private readonly AppSettings _settings;
    private readonly Action _onAddTodo;
    private readonly Action _onToggleTopmost;
    private readonly Action _onToggleAutoStart;
    private readonly Action _onSetCity;
    private readonly Action _onDetectLocation;
    private readonly Action _onToggleWeather;
    private readonly Action _onToggleWeekStrip;
    private readonly Action _onSetCalendarWeek;
    private readonly Action _onSetCalendarMonth;
    private readonly Action _onJumpCalendarDate;
    private readonly Action _onResetCalendarToday;
    private readonly Action<string> _onToggleModule;
    private readonly Action _onAddCountdown;
    private readonly Action _onExportBackup;
    private readonly Action<ThemeMode> _onSetTheme;
    private readonly Action<double> _onSetOpacity;
    private readonly Action _onToggleClickThrough;
    private readonly Action _onExit;

    public TrayService(
        MainWindow window,
        AppSettings settings,
        Action onAddTodo,
        Action onToggleTopmost,
        Action onToggleAutoStart,
        Action onSetCity,
        Action onDetectLocation,
        Action onToggleWeather,
        Action onToggleWeekStrip,
        Action onSetCalendarWeek,
        Action onSetCalendarMonth,
        Action onJumpCalendarDate,
        Action onResetCalendarToday,
        Action<string> onToggleModule,
        Action onAddCountdown,
        Action onExportBackup,
        Action<ThemeMode> onSetTheme,
        Action<double> onSetOpacity,
        Action onToggleClickThrough,
        Action onExit)
    {
        _window = window;
        _settings = settings;
        _onAddTodo = onAddTodo;
        _onToggleTopmost = onToggleTopmost;
        _onToggleAutoStart = onToggleAutoStart;
        _onSetCity = onSetCity;
        _onDetectLocation = onDetectLocation;
        _onToggleWeather = onToggleWeather;
        _onToggleWeekStrip = onToggleWeekStrip;
        _onSetCalendarWeek = onSetCalendarWeek;
        _onSetCalendarMonth = onSetCalendarMonth;
        _onJumpCalendarDate = onJumpCalendarDate;
        _onResetCalendarToday = onResetCalendarToday;
        _onToggleModule = onToggleModule;
        _onAddCountdown = onAddCountdown;
        _onExportBackup = onExportBackup;
        _onSetTheme = onSetTheme;
        _onSetOpacity = onSetOpacity;
        _onToggleClickThrough = onToggleClickThrough;
        _onExit = onExit;

        _icon = new NotifyIcon
        {
            Text = "DeskLite",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true
        };

        _icon.DoubleClick += (_, _) => ToggleWindow();
        RefreshMenu();
    }

    public void RefreshMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示/隐藏", null, (_, _) => ToggleWindow());
        menu.Items.Add("添加待办", null, (_, _) => _onAddTodo());
        menu.Items.Add("切换置顶", null, (_, _) => _onToggleTopmost());
        menu.Items.Add(CreateCheckItem("开机自启", _settings.AutoStart, _onToggleAutoStart));
        menu.Items.Add(CreateCheckItem("鼠标穿透", _settings.ClickThrough, _onToggleClickThrough));
        menu.Items.Add(CreateCheckItem("显示天气", _settings.ShowWeather, _onToggleWeather));
        menu.Items.Add(CreateCheckItem("显示城市", _settings.ShowCityName, () => _onToggleModule("cityName")));
        menu.Items.Add(CreateCheckItem("显示日历", _settings.ShowWeekStrip, _onToggleWeekStrip));
        menu.Items.Add("设置城市...", null, (_, _) => _onSetCity());
        menu.Items.Add("定位当前城市", null, (_, _) => _onDetectLocation());
        menu.Items.Add(BuildCalendarMenu());
        menu.Items.Add(BuildModuleMenu());
        menu.Items.Add("添加倒数日...", null, (_, _) => _onAddCountdown());
        menu.Items.Add("导出数据备份", null, (_, _) => _onExportBackup());
        menu.Items.Add(BuildAppearanceMenu());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => _onExit());
        _icon.ContextMenuStrip = menu;
    }

    private ToolStripMenuItem BuildCalendarMenu()
    {
        var calendar = new ToolStripMenuItem("日历");
        var isWeek = CalendarViewHelper.ParseMode(_settings.CalendarMode) == CalendarViewMode.Week;
        calendar.DropDownItems.Add(CreateRadioItem("周历视图", isWeek, _onSetCalendarWeek));
        calendar.DropDownItems.Add(CreateRadioItem("月历视图", !isWeek, _onSetCalendarMonth));
        calendar.DropDownItems.Add(new ToolStripSeparator());
        calendar.DropDownItems.Add("回到今天", null, (_, _) => _onResetCalendarToday());
        calendar.DropDownItems.Add("跳转日期...", null, (_, _) => _onJumpCalendarDate());
        return calendar;
    }

    private ToolStripMenuItem BuildModuleMenu()
    {
        var modules = new ToolStripMenuItem("模块管理");
        modules.DropDownItems.Add(CreateCheckItem("年进度", _settings.ShowYearProgress, () => _onToggleModule("yearProgress")));
        modules.DropDownItems.Add(CreateCheckItem("倒数日", _settings.ShowCountdown, () => _onToggleModule("countdown")));
        modules.DropDownItems.Add(CreateCheckItem("每日一句", _settings.ShowDailyQuote, () => _onToggleModule("dailyQuote")));
        modules.DropDownItems.Add(CreateCheckItem("日出日落", _settings.ShowSunriseSunset, () => _onToggleModule("sunrise")));
        modules.DropDownItems.Add(CreateCheckItem("明日天气", _settings.ShowTomorrowWeather, () => _onToggleModule("tomorrow")));
        modules.DropDownItems.Add(CreateCheckItem("速记便签", _settings.ShowScratch, () => _onToggleModule("scratch")));
        modules.DropDownItems.Add(CreateCheckItem("显示秒", _settings.ShowSeconds, () => _onToggleModule("seconds")));
        modules.DropDownItems.Add(CreateCheckItem("待办到时提醒", _settings.ShowTodoReminder, () => _onToggleModule("todoReminder")));
        modules.DropDownItems.Add(CreateCheckItem("全局快捷键", _settings.EnableGlobalHotkey, () => _onToggleModule("hotkey")));
        return modules;
    }

    private ToolStripMenuItem BuildAppearanceMenu()
    {
        var appearance = new ToolStripMenuItem("外观");
        var currentTheme = AppThemePalette.Parse(_settings.Theme);

        appearance.DropDownItems.Add(CreateRadioItem("深色主题", currentTheme == ThemeMode.Dark, () => _onSetTheme(ThemeMode.Dark)));
        appearance.DropDownItems.Add(CreateRadioItem("浅色主题", currentTheme == ThemeMode.Light, () => _onSetTheme(ThemeMode.Light)));
        appearance.DropDownItems.Add(new ToolStripSeparator());

        foreach (var (label, value) in OpacityPresets)
        {
            var isCurrent = Math.Abs(_settings.Opacity - value) < 0.001;
            appearance.DropDownItems.Add(CreateRadioItem(label, isCurrent, () => _onSetOpacity(value)));
        }

        return appearance;
    }

    private static ToolStripMenuItem CreateCheckItem(string text, bool isChecked, Action onClick)
    {
        var item = new ToolStripMenuItem(text) { Checked = isChecked };
        item.Click += (_, _) => onClick();
        return item;
    }

    private static ToolStripMenuItem CreateRadioItem(string text, bool isChecked, Action onClick)
    {
        var item = new ToolStripMenuItem(text) { Checked = isChecked };
        item.Click += (_, _) => onClick();
        return item;
    }

    private void ToggleWindow()
    {
        if (_window.IsVisible)
        {
            _window.Hide();
        }
        else
        {
            _window.Show();
            _window.Activate();
        }
    }

    public void ShowBalloon(string message, string title = "DeskLite")
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
