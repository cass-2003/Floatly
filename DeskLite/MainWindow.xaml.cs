using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskLite.Models;
using DeskLite.Services;

namespace DeskLite;

public partial class MainWindow : Window
{
    private static readonly string[] WeekLabels = ["一", "二", "三", "四", "五", "六", "日"];

    private readonly TodoStore _todoStore = new();
    private readonly WeatherService _weatherService = new();
    private readonly LocationService _locationService = new();
    private readonly TodoReminderService _reminderService = new();
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _clockTimer;
    private DateTime _lastWeatherFetch = DateTime.MinValue;
    private DateTime? _calendarPreviewDate;
    private CalendarViewMode _calendarMode = CalendarViewMode.Week;
    private DateTime _calendarAnchor = DateTime.Today;
    private TrayService? _tray;
    private GlobalHotkeyService? _hotkeyService;
    private AppThemePalette _palette = AppThemePalette.For(ThemeMode.Dark);

    public MainWindow()
    {
        InitializeComponent();

        _settings = JsonStore.LoadSettings();
        LoadCalendarState();
        SyncAutoStartSetting();
        ApplySettings();

        RefreshClock();
        RefreshExtras();
        RefreshCalendar();
        RefreshTodos();
        LoadScratch();
        _ = RefreshWeatherAsync();

        _clockTimer = new DispatcherTimer();
        ApplyClockTimerInterval();
        _clockTimer.Tick += (_, _) =>
        {
            RefreshClock();
            RefreshExtras();
            CheckTodoReminders();
            if (DateTime.Now - _lastWeatherFetch >= TimeSpan.FromMinutes(60))
            {
                _ = RefreshWeatherAsync();
            }
        };
        _clockTimer.Start();

        _tray = new TrayService(
            this,
            _settings,
            PromptAddTodo,
            ToggleTopmost,
            ToggleAutoStart,
            SetCity,
            DetectLocationByIp,
            ToggleWeather,
            ToggleWeekStrip,
            SetCalendarWeek,
            SetCalendarMonth,
            JumpToCalendarDate,
            ResetCalendarToday,
            ToggleModule,
            AddCountdown,
            ExportBackup,
            SetTheme,
            SetOpacity,
            ToggleClickThrough,
            ExitApp);

        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };

        LocationChanged += (_, _) => SaveWindowPosition();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowHelper.SetClickThrough(this, _settings.ClickThrough);
        if (_settings.EnableGlobalHotkey)
        {
            _hotkeyService = new GlobalHotkeyService(this, ToggleWindow, QuickAddTodo);
            _hotkeyService.Register();
        }
    }

    private void SyncAutoStartSetting()
    {
        var actual = AutoStartService.IsEnabled();
        if (_settings.AutoStart != actual)
        {
            _settings.AutoStart = actual;
            JsonStore.SaveSettings(_settings);
        }
    }

    private void ApplySettings()
    {
        Topmost = _settings.AlwaysOnTop;
        Left = _settings.Left;
        Top = _settings.Top;
        Opacity = Math.Clamp(_settings.Opacity, 0.45, 1.0);
        ApplyTheme();

        WeatherText.Visibility = _settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
        CityText.Visibility = _settings.ShowWeather && _settings.ShowCityName ? Visibility.Visible : Visibility.Collapsed;
        YearProgressText.Visibility = _settings.ShowYearProgress ? Visibility.Visible : Visibility.Collapsed;
        CountdownText.Visibility = _settings.ShowCountdown ? Visibility.Visible : Visibility.Collapsed;
        DailyQuoteText.Visibility = _settings.ShowDailyQuote ? Visibility.Visible : Visibility.Collapsed;
        ScratchBox.Visibility = _settings.ShowScratch ? Visibility.Visible : Visibility.Collapsed;

        var showWeatherExtra = _settings.ShowWeather && (_settings.ShowSunriseSunset || _settings.ShowTomorrowWeather);
        WeatherExtraText.Visibility = showWeatherExtra ? Visibility.Visible : Visibility.Collapsed;

        var showWeek = _settings.ShowWeekStrip;
        CalendarSection.Visibility = showWeek ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadCalendarState()
    {
        _calendarMode = CalendarViewHelper.ParseMode(_settings.CalendarMode);
        _calendarAnchor = DateTime.TryParse(_settings.CalendarAnchorDate, out var anchor)
            ? anchor
            : DateTime.Today;
    }

    private void SaveCalendarState()
    {
        _settings.CalendarMode = CalendarViewHelper.ToSettingValue(_calendarMode);
        _settings.CalendarAnchorDate = _calendarAnchor.ToString("yyyy-MM-dd");
        JsonStore.SaveSettings(_settings);
    }

    private void ApplyTheme()
    {
        _palette = AppThemePalette.For(AppThemePalette.Parse(_settings.Theme));

        MainBorder.Background = new SolidColorBrush(_palette.PanelBackground);
        MainBorder.BorderBrush = new SolidColorBrush(_palette.PanelBorder);
        DividerBorder.Background = new SolidColorBrush(_palette.Divider);

        ClockText.Foreground = Brush(_palette.TextPrimary);
        DateText.Foreground = Brush(_palette.TextSecondary);
        LunarText.Foreground = Brush(_palette.TextTertiary);
        LunarSubText.Foreground = Brush(_palette.TextSubtle);
        WeatherText.Foreground = Brush(_palette.TextSecondary);
        CityText.Foreground = Brush(_palette.TextMuted);
        WeatherExtraText.Foreground = Brush(_palette.TextSubtle);
        YearProgressText.Foreground = Brush(_palette.TextMuted);
        CountdownText.Foreground = Brush(_palette.TextSecondary);
        DailyQuoteText.Foreground = Brush(_palette.TextMuted);
        TodoTitleText.Foreground = Brush(_palette.TextMuted);
        EmptyTodoText.Foreground = Brush(_palette.TextEmpty);
        CalendarTitleText.Foreground = Brush(_palette.TextEmpty);
        CalPrevBtn.Foreground = Brush(_palette.TextMuted);
        CalNextBtn.Foreground = Brush(_palette.TextMuted);
        UpdateCalendarModeButtons();

        NewTodoBox.Background = Brush(_palette.InputBackground);
        NewTodoBox.BorderBrush = Brush(_palette.InputBorder);
        NewTodoBox.Foreground = Brush(_palette.InputText);
        ScratchBox.Background = Brush(_palette.InputBackground);
        ScratchBox.BorderBrush = Brush(_palette.InputBorder);
        ScratchBox.Foreground = Brush(_palette.TextTertiary);

        Resources["TodoTextBrush"] = Brush(_palette.TodoText);
        Resources["TodoDeleteBrush"] = Brush(_palette.DeleteButton);

        RefreshCalendar();
        RefreshTodoTheme();
    }

    private void UpdateCalendarModeButtons()
    {
        var isWeek = _calendarMode == CalendarViewMode.Week;
        CalWeekModeBtn.Foreground = Brush(isWeek ? _palette.Accent : _palette.TextEmpty);
        CalMonthModeBtn.Foreground = Brush(isWeek ? _palette.TextEmpty : _palette.Accent);
        CalWeekModeBtn.FontWeight = isWeek ? FontWeights.SemiBold : FontWeights.Normal;
        CalMonthModeBtn.FontWeight = isWeek ? FontWeights.Normal : FontWeights.SemiBold;
    }

    private void RefreshTodoTheme()
    {
        var items = TodoList.ItemsSource;
        TodoList.ItemsSource = null;
        TodoList.ItemsSource = items;
    }

    public void SetTheme(ThemeMode mode)
    {
        _settings.Theme = mode == ThemeMode.Light ? "light" : "dark";
        ApplyTheme();
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
    }

    public void SetOpacity(double opacity)
    {
        _settings.Opacity = Math.Clamp(opacity, 0.45, 1.0);
        Opacity = _settings.Opacity;
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
    }

    public void ToggleModule(string key)
    {
        switch (key)
        {
            case "yearProgress": _settings.ShowYearProgress = !_settings.ShowYearProgress; break;
            case "countdown": _settings.ShowCountdown = !_settings.ShowCountdown; break;
            case "dailyQuote": _settings.ShowDailyQuote = !_settings.ShowDailyQuote; break;
            case "sunrise": _settings.ShowSunriseSunset = !_settings.ShowSunriseSunset; break;
            case "tomorrow": _settings.ShowTomorrowWeather = !_settings.ShowTomorrowWeather; break;
            case "scratch": _settings.ShowScratch = !_settings.ShowScratch; break;
            case "cityName": _settings.ShowCityName = !_settings.ShowCityName; break;
            case "seconds":
                _settings.ShowSeconds = !_settings.ShowSeconds;
                ApplyClockTimerInterval();
                RefreshClock();
                break;
            case "todoReminder": _settings.ShowTodoReminder = !_settings.ShowTodoReminder; break;
            case "hotkey":
                _settings.EnableGlobalHotkey = !_settings.EnableGlobalHotkey;
                if (_settings.EnableGlobalHotkey)
                {
                    _hotkeyService ??= new GlobalHotkeyService(this, ToggleWindow, QuickAddTodo);
                    _hotkeyService.Register();
                }
                else
                {
                    _hotkeyService?.Unregister();
                }
                break;
            default: return;
        }

        ApplySettings();
        RefreshExtras();
        if (key is "cityName")
        {
            RefreshCityDisplay(_weatherService.LoadCache());
        }
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
        if (key is "sunrise" or "tomorrow")
        {
            _ = RefreshWeatherAsync();
        }
    }

    private void ApplyClockTimerInterval() =>
        _clockTimer.Interval = _settings.ShowSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

    private string FormatClock(DateTime now) =>
        _settings.ShowSeconds
            ? now.ToString(_settings.Time24h ? "HH:mm:ss" : "hh:mm:ss", CultureInfo.InvariantCulture)
            : now.ToString(_settings.Time24h ? "HH:mm" : "hh:mm", CultureInfo.InvariantCulture);

    private void RefreshClock()
    {
        var now = DateTime.Now;
        ClockText.Text = FormatClock(now);

        var displayDate = _calendarPreviewDate ?? now;
        var info = LunarCalendar.Get(displayDate);

        DateText.Text = _calendarPreviewDate is null
            ? $"周{info.WeekName} {now.Month}月{now.Day}日"
            : $"周{info.WeekName} {displayDate.Month}月{displayDate.Day}日";

        LunarText.Text = info.Line;
        LunarSubText.Text = info.SubLine;
        RefreshCalendar();
    }

    private void RefreshExtras()
    {
        if (_settings.ShowYearProgress)
        {
            YearProgressText.Text = YearProgressService.GetLine(DateTime.Today);
        }

        if (_settings.ShowCountdown)
        {
            CountdownText.Text = CountdownService.GetLine(_todoStore.Data.Countdowns, DateTime.Today)
                                 ?? "⏳ 暂无倒数日（托盘可添加）";
        }

        if (_settings.ShowDailyQuote)
        {
            DailyQuoteText.Text = DailyQuoteService.GetToday(DateTime.Today);
        }
    }

    private void RefreshCalendar()
    {
        if (!_settings.ShowWeekStrip)
        {
            return;
        }

        CalendarTitleText.Text = CalendarViewHelper.GetTitle(_calendarMode, _calendarAnchor, _calendarPreviewDate);
        UpdateCalendarModeButtons();
        Height = _calendarMode == CalendarViewMode.Month ? 640 : 540;

        CalendarPanel.Children.Clear();
        var today = DateTime.Today;
        var preview = _calendarPreviewDate?.Date;
        var days = (_calendarMode == CalendarViewMode.Month
            ? CalendarViewHelper.GetMonthGridDays(_calendarAnchor)
            : CalendarViewHelper.GetWeekDays(_calendarAnchor)).ToList();

        WeekdayHeader.Visibility = _calendarMode == CalendarViewMode.Month
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_calendarMode == CalendarViewMode.Month)
        {
            WeekdayHeader.Children.Clear();
            for (var i = 0; i < 7; i++)
            {
                WeekdayHeader.Children.Add(new TextBlock
                {
                    Text = WeekLabels[i],
                    FontSize = 9,
                    Foreground = Brush(_palette.WeekLabel),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                });
            }
        }

        var grid = new System.Windows.Controls.Primitives.UniformGrid
        {
            Rows = _calendarMode == CalendarViewMode.Month ? 6 : 1,
            Columns = 7
        };

        foreach (var day in days)
        {
            grid.Children.Add(BuildCalendarCell(day, today, preview));
        }

        CalendarPanel.Children.Add(grid);
    }

    private UIElement BuildCalendarCell(DateTime day, DateTime today, DateTime? preview)
    {
        var isToday = day == today;
        var isPreview = preview == day;
        var inMonth = _calendarMode == CalendarViewMode.Week || CalendarViewHelper.IsSameMonth(day, _calendarAnchor);
        var dayInfo = LunarCalendar.Get(day);
        var hasMark = dayInfo.Mark is not null;
        var holiday = HolidayService.GetMark(day);

        var cell = new StackPanel
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = day,
            Margin = new Thickness(0, 1, 0, 1)
        };
        cell.MouseLeftButtonDown += CalendarDay_Click;

        if (_calendarMode == CalendarViewMode.Week)
        {
            cell.Children.Add(new TextBlock
            {
                Text = WeekLabels[((int)day.DayOfWeek + 6) % 7],
                FontSize = 10,
                Foreground = isPreview ? Brush(_palette.Accent) : Brush(_palette.WeekLabel),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            });
        }

        cell.Children.Add(new TextBlock
        {
            Text = isToday && preview is null ? "●" : day.Day.ToString(),
            FontSize = _calendarMode == CalendarViewMode.Month ? 10 : isToday ? 10 : 11,
            FontWeight = isPreview ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = !inMonth
                ? Brush(0x4B, 0x55, 0x63)
                : isToday || isPreview ? Brush(_palette.Accent) : Brush(_palette.WeekSolar),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 1, 0, 0)
        });

        var lunarText = hasMark ? dayInfo.Mark! : dayInfo.ShortLunar;
        if (_calendarMode == CalendarViewMode.Month && lunarText.Length > 3)
        {
            lunarText = dayInfo.ShortLunar;
        }

        cell.Children.Add(new TextBlock
        {
            Text = lunarText,
            FontSize = 8,
            Foreground = hasMark ? Brush(_palette.Mark) : Brush(_palette.WeekLunar),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        if (holiday is not null)
        {
            cell.Children.Add(new TextBlock
            {
                Text = holiday,
                FontSize = 7,
                Foreground = holiday == "休" ? Brush(0x22, 0xC5, 0x5E) : Brush(0xEF, 0x44, 0x44),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            });
        }

        return cell;
    }

    private void RefreshCityDisplay(WeatherCache? cache = null)
    {
        if (!_settings.ShowWeather || !_settings.ShowCityName)
        {
            CityText.Visibility = Visibility.Collapsed;
            return;
        }

        var city = cache?.City ?? _settings.ResolvedCityName ?? _settings.City;
        var region = cache?.Region ?? _settings.ResolvedRegion;
        var source = cache?.LocationSource ?? "manual";
        var sourceLabel = source == "ip" ? "IP定位" : "手动";

        CityText.Text = string.IsNullOrWhiteSpace(region)
            ? $"📍 {city}（{sourceLabel}）"
            : $"📍 {city} · {region}";
        CityText.Visibility = Visibility.Visible;
    }

    private SolidColorBrush Brush(System.Windows.Media.Color color) => new(color);

    private static SolidColorBrush Brush(byte r, byte g, byte b) =>
        new(System.Windows.Media.Color.FromRgb(r, g, b));

    private void CalendarDay_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not StackPanel { Tag: DateTime day })
        {
            return;
        }

        if (e.ClickCount >= 2 && day == DateTime.Today)
        {
            ResetCalendarToday();
            return;
        }

        _calendarPreviewDate = day == _calendarPreviewDate ? null : day;
        RefreshClock();
    }

    private async Task RefreshWeatherAsync()
    {
        if (!_settings.ShowWeather)
        {
            return;
        }

        var oldCache = _weatherService.LoadCache();
        if (oldCache is not null)
        {
            ApplyWeather(oldCache);
            RefreshCityDisplay(oldCache);
        }

        var result = await _weatherService.FetchAsync(
            _settings.City,
            _settings.WeatherLatitude,
            _settings.WeatherLongitude,
            _settings.ResolvedRegion,
            oldCache?.LocationSource);
        _lastWeatherFetch = DateTime.Now;

        if (result is null)
        {
            WeatherText.Text = "天气加载失败";
            RefreshCityDisplay();
            WeatherExtraText.Visibility = Visibility.Collapsed;
            return;
        }

        if (result.Latitude != 0 && result.Longitude != 0)
        {
            _settings.WeatherLatitude = result.Latitude;
            _settings.WeatherLongitude = result.Longitude;
            _settings.ResolvedCityName = result.Cache.City;
            _settings.ResolvedRegion = result.Cache.Region;
            _settings.City = result.Cache.City;
            JsonStore.SaveSettings(_settings);
        }

        ApplyWeather(result.Cache);
        RefreshCityDisplay(result.Cache);
    }

    private void ApplyWeather(WeatherCache cache)
    {
        WeatherText.Text = $"{cache.Icon} {cache.Temperature}°  {cache.Description}  {cache.TempMin}°~{cache.TempMax}°";

        var parts = new List<string>();
        if (_settings.ShowSunriseSunset && cache.Sunrise is not null && cache.Sunset is not null)
        {
            parts.Add($"日出 {cache.Sunrise} · 日落 {cache.Sunset}");
        }

        if (_settings.ShowTomorrowWeather && cache.TomorrowDescription is not null
            && cache.TomorrowMin is not null && cache.TomorrowMax is not null)
        {
            parts.Add($"明天 {cache.TomorrowIcon} {cache.TomorrowDescription} {cache.TomorrowMin}°~{cache.TomorrowMax}°");
        }

        if (parts.Count == 0)
        {
            WeatherExtraText.Visibility = Visibility.Collapsed;
            return;
        }

        WeatherExtraText.Text = string.Join("  ", parts);
        WeatherExtraText.Visibility = _settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CheckTodoReminders()
    {
        if (!_settings.ShowTodoReminder)
        {
            return;
        }

        foreach (var todo in _reminderService.CheckDue(_todoStore.GetTodayTimedTodos(), DateTime.Now))
        {
            var label = string.IsNullOrWhiteSpace(todo.Time) ? todo.Title : $"{todo.Time} {todo.Title}";
            _tray?.ShowBalloon(label, "待办提醒");
        }
    }

    private void RefreshTodos()
    {
        var items = _todoStore.GetTodayTodos()
            .Select(t => new TodoDisplayItem(t.Id, FormatTodo(t)))
            .ToList();

        TodoList.ItemsSource = items;
        EmptyTodoText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatTodo(TodoItem item)
    {
        var prefix = item.Pinned ? "⭐ " : string.Empty;
        var body = string.IsNullOrWhiteSpace(item.Time) ? item.Title : $"{item.Time} {item.Title}";
        return prefix + body;
    }

    private void LoadScratch()
    {
        if (_settings.ShowScratch)
        {
            ScratchBox.Text = _todoStore.Data.Scratch;
        }
    }

    private void SaveScratch()
    {
        _todoStore.SaveScratch(ScratchBox.Text.Trim());
    }

    private void AddTodoFromInput()
    {
        var text = NewTodoBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string? time = null;
        var title = text;

        if (text.Length >= 5 && text[2] == ':' && int.TryParse(text[..2], out _))
        {
            time = text[..5];
            title = text[5..].TrimStart();
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _todoStore.Add(title, time);
        NewTodoBox.Clear();
        RefreshTodos();
    }

    public void PromptAddTodo()
    {
        Show();
        Activate();
        NewTodoBox.Focus();
    }

    public void QuickAddTodo()
    {
        var text = InputPrompt.Show("快速记待办", "输入待办（可加时间如 14:00 周会）：");
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        NewTodoBox.Text = text;
        AddTodoFromInput();
        Show();
        Activate();
    }

    public void ToggleWindow()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
            Activate();
        }
    }

    public void AddCountdown()
    {
        var title = InputPrompt.Show("添加倒数日", "事件名称：");
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var dateText = InputPrompt.Show("添加倒数日", "目标日期 (yyyy-MM-dd)：", DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"));
        if (!DateTime.TryParse(dateText, out var date))
        {
            return;
        }

        _todoStore.AddCountdown(title.Trim(), date, false);
        RefreshExtras();
        Show();
        Activate();
    }

    public void ExportBackup()
    {
        try
        {
            var src = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskLite", "data.json");
            if (!File.Exists(src))
            {
                _tray?.ShowBalloon("暂无数据可导出");
                return;
            }

            var dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"desk-lite-backup-{DateTime.Now:yyyyMMdd}.json");
            File.Copy(src, dest, true);
            _tray?.ShowBalloon($"已导出到桌面：{Path.GetFileName(dest)}");
        }
        catch
        {
            _tray?.ShowBalloon("导出失败");
        }
    }

    private void ToggleTopmost()
    {
        _settings.AlwaysOnTop = !Topmost;
        Topmost = _settings.AlwaysOnTop;
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
    }

    private void ToggleAutoStart()
    {
        _settings.AutoStart = !_settings.AutoStart;
        AutoStartService.SetEnabled(_settings.AutoStart);
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
    }

    private void ToggleWeather()
    {
        _settings.ShowWeather = !_settings.ShowWeather;
        ApplySettings();
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
        if (_settings.ShowWeather)
        {
            _ = RefreshWeatherAsync();
        }
    }

    private void ToggleWeekStrip()
    {
        _settings.ShowWeekStrip = !_settings.ShowWeekStrip;
        ApplySettings();
        RefreshCalendar();
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
    }

    public async void DetectLocationByIp()
    {
        var loc = await _locationService.DetectByIpAsync();
        if (loc is null)
        {
            _tray?.ShowBalloon("定位失败，请检查网络或手动设置城市");
            return;
        }

        _settings.City = loc.City;
        _settings.ResolvedCityName = loc.City;
        _settings.ResolvedRegion = loc.Region;
        _settings.WeatherLatitude = loc.Latitude;
        _settings.WeatherLongitude = loc.Longitude;
        JsonStore.SaveSettings(_settings);

        var result = await _weatherService.FetchAsync(loc.City, loc.Latitude, loc.Longitude, loc.Region, "ip");
        if (result is not null)
        {
            ApplyWeather(result.Cache);
            RefreshCityDisplay(result.Cache);
        }
        else
        {
            RefreshCityDisplay();
        }

        _tray?.ShowBalloon($"已定位到 {loc.City}");
    }

    public void SetCalendarWeek() => SetCalendarMode(CalendarViewMode.Week);

    public void SetCalendarMonth() => SetCalendarMode(CalendarViewMode.Month);

    public void ResetCalendarToday()
    {
        _calendarAnchor = DateTime.Today;
        _calendarPreviewDate = null;
        SaveCalendarState();
        RefreshClock();
    }

    public void JumpToCalendarDate()
    {
        var text = InputPrompt.Show("跳转日期", "输入日期 (yyyy-MM-dd)：", DateTime.Today.ToString("yyyy-MM-dd"));
        if (!DateTime.TryParse(text, out var date))
        {
            return;
        }

        _calendarAnchor = date;
        _calendarPreviewDate = date;
        SaveCalendarState();
        RefreshClock();
        Show();
        Activate();
    }

    private void SetCalendarMode(CalendarViewMode mode)
    {
        _calendarMode = mode;
        SaveCalendarState();
        RefreshCalendar();
        _tray?.RefreshMenu();
    }

    private void CalPrev_Click(object sender, RoutedEventArgs e)
    {
        _calendarAnchor = _calendarMode == CalendarViewMode.Month
            ? _calendarAnchor.AddMonths(-1)
            : _calendarAnchor.AddDays(-7);
        SaveCalendarState();
        RefreshCalendar();
    }

    private void CalNext_Click(object sender, RoutedEventArgs e)
    {
        _calendarAnchor = _calendarMode == CalendarViewMode.Month
            ? _calendarAnchor.AddMonths(1)
            : _calendarAnchor.AddDays(7);
        SaveCalendarState();
        RefreshCalendar();
    }

    private void CalWeekMode_Click(object sender, RoutedEventArgs e) => SetCalendarMode(CalendarViewMode.Week);

    private void CalMonthMode_Click(object sender, RoutedEventArgs e) => SetCalendarMode(CalendarViewMode.Month);

    private void CalToday_Click(object sender, RoutedEventArgs e) => ResetCalendarToday();

    private void CalJump_Click(object sender, RoutedEventArgs e) => JumpToCalendarDate();

    private void SetCity()
    {
        var city = InputPrompt.Show("设置城市", "输入城市名称：", _settings.City);
        if (string.IsNullOrWhiteSpace(city))
        {
            return;
        }

        _settings.City = city.Trim();
        _settings.ResolvedCityName = city.Trim();
        _settings.ResolvedRegion = null;
        _settings.WeatherLatitude = null;
        _settings.WeatherLongitude = null;
        JsonStore.SaveSettings(_settings);
        _ = RefreshWeatherAsync();
    }

    private void ToggleClickThrough()
    {
        _settings.ClickThrough = !_settings.ClickThrough;
        WindowHelper.SetClickThrough(this, _settings.ClickThrough);
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();

        if (_settings.ClickThrough)
        {
            _tray?.ShowBalloon("已开启鼠标穿透，窗口无法拖动。请从托盘菜单取消勾选「鼠标穿透」。");
        }
    }

    private void ExitApp()
    {
        SaveWindowPosition();
        _hotkeyService?.Dispose();
        _tray?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void SaveWindowPosition()
    {
        _settings.Left = Left;
        _settings.Top = Top;
        JsonStore.SaveSettings(_settings);
    }

    private void Grid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        HandleWindowDrag(e);
    }

    private void HandleWindowDrag(System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2 && IsInHeaderArea(e.OriginalSource as DependencyObject))
        {
            ToggleClickThrough();
            e.Handled = true;
            return;
        }

        if (_settings.ClickThrough)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch
        {
            // 拖动边界偶发异常，忽略
        }
    }

    private bool IsInHeaderArea(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source == ClockText || source == DateText || source == LunarText || source == LunarSubText)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            switch (source)
            {
                case System.Windows.Controls.TextBox:
                case System.Windows.Controls.Button:
                case System.Windows.Controls.CheckBox:
                    return true;
                case StackPanel { Tag: DateTime }:
                    return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void AddTodo_Click(object sender, RoutedEventArgs e) => AddTodoFromInput();

    private void NewTodoBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            AddTodoFromInput();
        }
    }

    private void ScratchBox_LostFocus(object sender, RoutedEventArgs e) => SaveScratch();

    private void ScratchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            SaveScratch();
            e.Handled = true;
        }
    }

    private void TodoCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox { Tag: string id })
        {
            _todoStore.ToggleDone(id);
            RefreshTodos();
        }
    }

    private void TodoDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string id })
        {
            _todoStore.Remove(id);
            RefreshTodos();
        }
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e) => SetTheme(ThemeMode.Dark);

    private void ThemeLight_Click(object sender, RoutedEventArgs e) => SetTheme(ThemeMode.Light);

    private void Opacity100_Click(object sender, RoutedEventArgs e) => SetOpacity(1.0);

    private void Opacity85_Click(object sender, RoutedEventArgs e) => SetOpacity(0.85);

    private void Opacity70_Click(object sender, RoutedEventArgs e) => SetOpacity(0.70);

    private void Opacity55_Click(object sender, RoutedEventArgs e) => SetOpacity(0.55);

    private sealed record TodoDisplayItem(string Id, string Display);
}
