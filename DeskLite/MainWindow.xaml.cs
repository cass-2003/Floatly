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
    private readonly PomodoroService _pomodoro = new();
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _clockTimer;
    private DateTime _lastWeatherFetch = DateTime.MinValue;
    private DateTime? _calendarPreviewDate;
    private CalendarViewMode _calendarMode = CalendarViewMode.Week;
    private DateTime _calendarAnchor = DateTime.Today;
    private TrayService? _tray;
    private GlobalHotkeyService? _hotkeyService;
    private AppThemePalette _palette = AppThemePalette.For(ThemeMode.Dark);
    private bool _suppressSizePersist;
    private TodoListWindow? _todoListWindow;

    public MainWindow()
    {
        InitializeComponent();

        if (AppIconService.GetIconUri() is { } iconUri)
        {
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }

        _settings = JsonStore.LoadSettings();
        LoadCalendarState();
        SyncAutoStartSetting();
        InitializePomodoro();
        ApplySettings();

        RefreshClock();
        RefreshExtras();
        RefreshCalendar();
        RefreshTodos();
        LoadScratch();
        _ = InitializeWeatherAsync();

        _clockTimer = new DispatcherTimer();
        ApplyClockTimerInterval();
        _clockTimer.Tick += (_, _) =>
        {
            RefreshClock();
            RefreshExtras();
            CheckTodoReminders();
            if (_settings.AutoLocateCity && ShouldRefreshAutoLocate())
            {
                _ = AutoLocateCityAsync(notify: false);
            }
            else if (DateTime.Now - _lastWeatherFetch >= TimeSpan.FromMinutes(60))
            {
                _ = RefreshWeatherAsync();
            }
        };
        _clockTimer.Start();

        _tray = new TrayService(
            this,
            OpenSettings,
            PromptAddTodo,
            OpenTodoListWindow,
            AddCountdown,
            JumpToCalendarDate,
            ResetCalendarToday,
            ExportBackup,
            ExitApp);

        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };

        LocationChanged += (_, _) => SaveWindowPosition();
        SizeChanged += OnWindowSizeChanged;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        WindowHelper.EnableBorderlessResize(this);
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
        Opacity = Math.Clamp(_settings.Opacity, 0.30, 1.0);
        ApplyTheme();

        WeatherText.Visibility = _settings.ShowWeather ? Visibility.Visible : Visibility.Collapsed;
        CityText.Visibility = _settings.ShowWeather && _settings.ShowCityName ? Visibility.Visible : Visibility.Collapsed;
        YearProgressPanel.Visibility = _settings.ShowYearProgress ? Visibility.Visible : Visibility.Collapsed;
        CountdownPanel.Visibility = _settings.ShowCountdown ? Visibility.Visible : Visibility.Collapsed;
        PomodoroPanel.Visibility = _settings.ShowPomodoro ? Visibility.Visible : Visibility.Collapsed;
        DailyQuoteText.Visibility = _settings.ShowDailyQuote ? Visibility.Visible : Visibility.Collapsed;
        ScratchBox.Visibility = _settings.ShowScratch ? Visibility.Visible : Visibility.Collapsed;

        var showWeatherExtra = _settings.ShowWeather && (_settings.ShowSunriseSunset || _settings.ShowTomorrowWeather);
        WeatherExtraText.Visibility = showWeatherExtra ? Visibility.Visible : Visibility.Collapsed;

        var showWeek = _settings.ShowWeekStrip;
        CalendarSection.Visibility = showWeek ? Visibility.Visible : Visibility.Collapsed;
        HuangLiPanel.Visibility = _settings.ShowHuangLi ? Visibility.Visible : Visibility.Collapsed;
        LunarText.Visibility = _settings.ShowHuangLi ? Visibility.Collapsed : Visibility.Visible;
        LunarSubText.Visibility = _settings.ShowHuangLi ? Visibility.Collapsed : Visibility.Visible;

        Width = Math.Clamp(_settings.WindowWidth, 260, 480);
        if (_settings.UserCustomSize)
        {
            _suppressSizePersist = true;
            Height = Math.Clamp(_settings.WindowHeight, MinHeight, 900);
            _suppressSizePersist = false;
        }
        else
        {
            UpdateWindowHeight();
        }

        FontScaleHelper.Apply(this, _settings.FontScale);
        if (_settings.ShowHuangLi)
        {
            var displayDate = _calendarPreviewDate ?? DateTime.Today;
            PopulateHuangLiTimeStrip(
                HuangLiService.Get(displayDate, includeCurrentTime: _calendarPreviewDate is null).TimeSlots);
        }

        ApplyModuleOrder();
    }

    private void ApplyModuleOrder()
    {
        var order = DeskModuleIds.Normalize(_settings.ModuleOrder);
        var modules = new Dictionary<string, UIElement>
        {
            [DeskModuleIds.HuangLi] = HuangLiPanel,
            [DeskModuleIds.YearProgress] = YearProgressPanel,
            [DeskModuleIds.Weather] = WeatherPanel,
            [DeskModuleIds.Countdown] = CountdownPanel,
            [DeskModuleIds.Pomodoro] = PomodoroPanel,
            [DeskModuleIds.DailyQuote] = DailyQuoteText,
            [DeskModuleIds.Scratch] = ScratchBox,
            [DeskModuleIds.Todos] = TodoPanel
        };

        ModuleContentPanel.Children.Clear();
        foreach (var id in order)
        {
            if (modules.TryGetValue(id, out var element))
            {
                ModuleContentPanel.Children.Add(element);
            }
        }
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
        ApplyHuangLiTheme();
        WeatherText.Foreground = Brush(_palette.TextSecondary);
        CityText.Foreground = Brush(_palette.TextMuted);
        WeatherExtraText.Foreground = Brush(_palette.TextSubtle);
        ApplyProgressTheme();
        ApplyPomodoroTheme();
        DailyQuoteText.Foreground = Brush(_palette.TextMuted);
        TodoTitleText.Foreground = Brush(_palette.TextMuted);
        EmptyTodoText.Foreground = Brush(_palette.TextEmpty);
        TodoCountBadge.Background = Brush(_palette.TodoCountBadge);
        TodoCountText.Foreground = Brush(_palette.Accent);
        TodoOverflowBtn.Background = Brush(_palette.HuangLiMutedButton);
        TodoOverflowText.Foreground = Brush(_palette.TodoLink);
        TodoViewAllText.Foreground = Brush(_palette.TodoLink);
        CalendarTitleText.Foreground = Brush(_palette.TextEmpty);
        CalPrevBtn.Foreground = Brush(_palette.TextMuted);
        CalNextBtn.Foreground = Brush(_palette.TextMuted);
        UpdateCalendarModeButtons();

        NewTodoBox.Background = Brush(_palette.InputBackground);
        NewTodoBox.BorderBrush = Brush(_palette.InputBorder);
        NewTodoBox.Foreground = Brush(_palette.InputText);
        AddButton.Background = Brush(_palette.TodoAccentButton);
        AddButton.Foreground = System.Windows.Media.Brushes.White;
        ScratchBox.Background = Brush(_palette.InputBackground);
        ScratchBox.BorderBrush = Brush(_palette.InputBorder);
        ScratchBox.Foreground = Brush(_palette.TextTertiary);

        Resources["TodoTextBrush"] = Brush(_palette.TodoText);
        Resources["TodoDeleteBrush"] = Brush(_palette.DeleteButton);
        TodoThemeHelper.ApplyResources(Resources, _palette);

        RefreshCalendar();
        RefreshTodoTheme();
        if (_settings.ShowHuangLi)
        {
            RefreshHuangLi(_calendarPreviewDate ?? DateTime.Today);
        }
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
        Dispatcher.BeginInvoke(ApplyCircleCheckTheme, DispatcherPriority.Loaded);
    }

    private void ApplyCircleCheckTheme()
    {
        foreach (var check in FindVisualChildren<CircleCheckBox>(TodoList))
        {
            check.ApplyTheme(_palette);
        }
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
        _settings.Opacity = Math.Clamp(opacity, 0.30, 1.0);
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
            case "pomodoro": _settings.ShowPomodoro = !_settings.ShowPomodoro; break;
            case "cityName": _settings.ShowCityName = !_settings.ShowCityName; break;
            case "huangLi": _settings.ShowHuangLi = !_settings.ShowHuangLi; break;
            case "autoLocate":
                _settings.AutoLocateCity = !_settings.AutoLocateCity;
                if (_settings.AutoLocateCity)
                {
                    _ = AutoLocateCityAsync(notify: true);
                }
                break;
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

        if (key is "huangLi")
        {
            RefreshClock();
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
        RefreshHuangLi(displayDate);
        RefreshCalendar();
    }

    private void RefreshHuangLi(DateTime displayDate)
    {
        if (!_settings.ShowHuangLi)
        {
            HuangLiPanel.Visibility = Visibility.Collapsed;
            LunarText.Visibility = Visibility.Visible;
            LunarSubText.Visibility = Visibility.Visible;
            LunarSubText.Text = LunarCalendar.Get(displayDate).SubLine;
            return;
        }

        HuangLiPanel.Visibility = Visibility.Visible;
        LunarText.Visibility = Visibility.Collapsed;
        LunarSubText.Visibility = Visibility.Collapsed;

        var huangLi = HuangLiService.Get(displayDate, includeCurrentTime: _calendarPreviewDate is null);

        HuangLiSolarDate.Text = huangLi.SolarDateText;
        HuangLiLunarLarge.Text = huangLi.LunarDateLarge;
        HuangLiMetaLine.Text = huangLi.MetaLine;
        HuangLiYiText.Text = JoinHuangLiItems(huangLi.YiItems);
        HuangLiJiText.Text = JoinHuangLiItems(huangLi.JiItems);

        HuangLiWuXingVal.Text = huangLi.WuXing;
        HuangLiChongVal.Text = huangLi.ChongSha;
        HuangLiZhiVal.Text = huangLi.ZhiShen;
        HuangLiJianVal.Text = huangLi.JianChu;

        HuangLiJiShenVal.Text = JoinHuangLiItems(huangLi.JiShen);
        HuangLiTaiShenVal.Text = huangLi.TaiShen;
        HuangLiXiongVal.Text = JoinHuangLiItems(huangLi.XiongShen);
        HuangLiPengZuVal.Text = huangLi.PengZu;
        HuangLiXiuVal.Text = huangLi.Xiu;

        PopulateHuangLiTimeStrip(huangLi.TimeSlots);
        PopulateHuangLiCurrentCard(huangLi.CurrentTime);

        ApplyHuangLiCollapsedState();
        UpdateWindowHeight();
    }

    private void ApplyHuangLiCollapsedState()
    {
        HuangLiDetailsPanel.Visibility = _settings.HuangLiCollapsed ? Visibility.Collapsed : Visibility.Visible;
        HuangLiCollapseBtn.Content = _settings.HuangLiCollapsed ? "▶" : "▼";
    }

    private void HuangLiCollapse_Click(object sender, RoutedEventArgs e)
    {
        _settings.HuangLiCollapsed = !_settings.HuangLiCollapsed;
        ApplyHuangLiCollapsedState();
        UpdateWindowHeight();
        JsonStore.SaveSettings(_settings);
    }

    private static string JoinHuangLiItems(IReadOnlyList<string> items) =>
        string.Join(" ", items);

    private void HuangLiPrev_Click(object sender, RoutedEventArgs e) => ShiftHuangLiDay(-1);

    private void HuangLiNext_Click(object sender, RoutedEventArgs e) => ShiftHuangLiDay(1);

    private void HuangLiMore_Click(object sender, MouseButtonEventArgs e)
    {
        // Stub: reserved for expanded HuangLi view.
    }

    private void ShiftHuangLiDay(int delta)
    {
        var baseDate = _calendarPreviewDate ?? DateTime.Today;
        _calendarPreviewDate = baseDate.AddDays(delta);
        SaveCalendarState();
        RefreshClock();
    }

    private void PopulateHuangLiCurrentCard(HuangLiCurrentTimeInfo? currentTime)
    {
        if (currentTime is null)
        {
            HuangLiCurrentCard.Visibility = Visibility.Collapsed;
            HuangLiCurrentDivider.Visibility = Visibility.Collapsed;
            return;
        }

        HuangLiCurrentCard.Visibility = Visibility.Visible;
        HuangLiCurrentDivider.Visibility = Visibility.Visible;
        HuangLiCurrentZhi.Text = currentTime.Zhi;
        HuangLiCurrentSummary.Text = $"{currentTime.GanZhiLabel} {currentTime.TimeRange} {currentTime.ChongSha}";
        HuangLiCurrentDirections.Text = $"喜神{currentTime.XiShen} 财神{currentTime.CaiShen} 福神{currentTime.FuShen}";
        HuangLiCurrentLuckText.Text = currentTime.Luck;
        HuangLiCurrentYiText.Text = JoinHuangLiItems(currentTime.YiItems);
        HuangLiCurrentJiText.Text = JoinHuangLiItems(currentTime.JiItems);

        var luckIsGood = currentTime.Luck == "吉";
        HuangLiCurrentLuckBadge.Background = Brush(luckIsGood ? _palette.HuangLiYiCircle : _palette.HuangLiJiCircle);
    }

    private void ApplyHuangLiTheme()
    {
        var border = Brush(_palette.HuangLiBorder);
        HuangLiPanel.Background = Brush(_palette.HuangLiBackground);
        HuangLiSolarDate.Foreground = Brush(_palette.TextEmpty);
        HuangLiCollapseBtn.Foreground = Brush(_palette.TextEmpty);
        HuangLiLunarLarge.Foreground = Brush(_palette.HuangLiAccent);
        HuangLiMetaLine.Foreground = Brush(_palette.TextEmpty);
        HuangLiPrevBtn.Foreground = Brush(_palette.HuangLiAccent);
        HuangLiNextBtn.Foreground = Brush(_palette.HuangLiAccent);
        HuangLiYiCircle.Background = Brush(_palette.HuangLiYiCircle);
        HuangLiJiCircle.Background = Brush(_palette.HuangLiJiCircle);
        HuangLiCurrentYiCircle.Background = Brush(_palette.HuangLiYiCircle);
        HuangLiCurrentJiCircle.Background = Brush(_palette.HuangLiJiCircle);
        HuangLiCurrentZhiCircle.Background = Brush(_palette.HuangLiAccent);
        HuangLiYiText.Foreground = Brush(_palette.TextPrimary);
        HuangLiJiText.Foreground = Brush(_palette.TextPrimary);
        HuangLiWuXingLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiChongLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiZhiLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiJianLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiJiShenLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiTaiShenLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiPengZuLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiXiuLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiTimeTitle.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiXiongLabel.Foreground = Brush(_palette.HuangLiLabel);
        HuangLiWuXingVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiChongVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiZhiVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiJianVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiJiShenVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiTaiShenVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiXiongVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiPengZuVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiXiuVal.Foreground = Brush(_palette.TextSecondary);
        HuangLiMetaGridBorder.BorderBrush = border;
        HuangLiTimeStripBorder.BorderBrush = border;
        HuangLiDetailBorder.BorderBrush = border;
        HuangLiCurrentDivider.Background = border;
        HuangLiMetaWuXing.BorderBrush = border;
        HuangLiMetaChong.BorderBrush = border;
        HuangLiJianCell.BorderBrush = border;
        HuangLiJiShenInner.BorderBrush = border;
        HuangLiTaiShenInner.BorderBrush = border;
        HuangLiXiongInner.BorderBrush = border;
        HuangLiXiuCell.BorderBrush = border;
        HuangLiCurrentTitle.Foreground = Brush(_palette.TextPrimary);
        HuangLiCurrentSummary.Foreground = Brush(_palette.TextPrimary);
        HuangLiCurrentDirections.Foreground = Brush(_palette.TextEmpty);
        HuangLiCurrentYiText.Foreground = Brush(_palette.TextSecondary);
        HuangLiCurrentJiText.Foreground = Brush(_palette.TextSecondary);
        HuangLiCurrentLuckText.Foreground = Brush(_palette.HuangLiCircleText);
        HuangLiMoreBtn.Background = Brush(_palette.HuangLiMutedButton);
        HuangLiMoreText.Foreground = Brush(_palette.TextEmpty);
        FontScaleHelper.Apply(this, _settings.FontScale);
    }

    private void PopulateHuangLiTimeStrip(IReadOnlyList<HuangLiTimeSlot> slots)
    {
        var scale = _settings.FontScale;
        HuangLiTimeStrip.Children.Clear();
        foreach (var slot in slots.Take(12))
        {
            var luckBrush = slot.Luck == "吉"
                ? Brush(_palette.HuangLiLuckGood)
                : slot.Luck == "凶"
                    ? Brush(_palette.HuangLiLuckBad)
                    : Brush(_palette.TextMuted);

            var cell = new Border
            {
                BorderBrush = slot.IsCurrent ? Brush(_palette.HuangLiCurrentBorder) : Brush(_palette.HuangLiBorder),
                BorderThickness = slot.IsCurrent ? new Thickness(1) : new Thickness(0, 0, 1, 0),
                Background = Brush(_palette.HuangLiBackground),
                Padding = new Thickness(0, 2, 0, 2),
                Child = new StackPanel
                {
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = slot.GanZhi,
                            FontSize = FontScaleHelper.ScaledSize(10, scale),
                            Foreground = Brush(_palette.TextSecondary),
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = slot.Luck,
                            FontSize = FontScaleHelper.ScaledSize(11, scale),
                            Foreground = luckBrush,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        }
                    }
                }
            };
            HuangLiTimeStrip.Children.Add(cell);
        }
    }

    private void UpdateWindowHeight()
    {
        if (_settings.UserCustomSize)
        {
            return;
        }

        const double headerHeight = 72;
        var scrollBody = _settings.ShowHuangLi ? 460 : 240;
        const double todoInput = 40;
        const double chrome = 28;

        var height = headerHeight + scrollBody + todoInput + chrome;

        if (_settings.ShowWeekStrip)
        {
            height += _calendarMode == CalendarViewMode.Month ? 200 : 95;
        }

        _suppressSizePersist = true;
        Height = Math.Clamp(height, MinHeight, 720);
        _settings.WindowHeight = Height;
        _suppressSizePersist = false;
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_suppressSizePersist || !IsLoaded)
        {
            return;
        }

        _settings.WindowWidth = ActualWidth;
        _settings.WindowHeight = ActualHeight;
        _settings.UserCustomSize = true;
        JsonStore.SaveSettings(_settings);
    }

    private void RefreshExtras()
    {
        if (_settings.ShowYearProgress)
        {
            var info = YearProgressService.GetInfo(DateTime.Today);
            YearProgressLabel.Text = $"{info.Year} 年进度";
            YearProgressPercent.Text = $"{info.Percent:F1}%";
            YearProgressDetail.Text = $"已过 {info.DaysElapsed} 天 / 全年 {info.DaysInYear} 天";
            QueueProgressBarUpdate(YearProgressTrack, YearProgressFill, info.Percent);
        }

        if (_settings.ShowCountdown)
        {
            var info = CountdownService.GetInfo(_todoStore.Data.Countdowns, DateTime.Today);
            if (info is null)
            {
                CountdownLabel.Text = "⏳ 暂无倒数日";
                CountdownDays.Text = "托盘可添加";
                CountdownHint.Text = "在托盘菜单中添加目标日期";
                QueueProgressBarUpdate(CountdownTrack, CountdownFill, 0);
            }
            else
            {
                CountdownLabel.Text = $"距离{info.Value.Title}还有";
                CountdownDays.Text = $"{info.Value.Days} 天";
                CountdownHint.Text = info.Value.Days <= 7
                    ? "目标即将到来"
                    : info.Value.Days <= 30
                        ? "进入倒计时阶段"
                        : "越接近目标，进度条越满";
                QueueProgressBarUpdate(CountdownTrack, CountdownFill, info.Value.ProgressPercent);
            }
        }

        if (_settings.ShowDailyQuote)
        {
            DailyQuoteText.Text = DailyQuoteService.GetToday(DateTime.Today);
        }
    }

    private void ApplyProgressTheme()
    {
        var fillBrush = new LinearGradientBrush(
            _palette.ProgressFillStart,
            _palette.ProgressFillEnd,
            new System.Windows.Point(0, 0),
            new System.Windows.Point(1, 0));

        YearProgressLabel.Foreground = Brush(_palette.TextMuted);
        YearProgressPercent.Foreground = Brush(_palette.TextPrimary);
        YearProgressDetail.Foreground = Brush(_palette.TextSubtle);
        YearProgressTrack.Background = Brush(_palette.ProgressTrack);
        YearProgressFill.Background = fillBrush;

        CountdownLabel.Foreground = Brush(_palette.TextMuted);
        CountdownDays.Foreground = Brush(_palette.TextSecondary);
        CountdownHint.Foreground = Brush(_palette.TextSubtle);
        CountdownTrack.Background = Brush(_palette.ProgressTrack);
        CountdownFill.Background = fillBrush;
    }

    private void ApplyPomodoroTheme()
    {
        PomodoroPanel.Background = Brush(_palette.HuangLiMutedButton);
        PomodoroPhaseText.Foreground = Brush(_palette.TextMuted);
        PomodoroSessionText.Foreground = Brush(_palette.TextSubtle);
        PomodoroCountdownText.Foreground = Brush(_palette.TextPrimary);
        PomodoroTrack.Background = Brush(_palette.ProgressTrack);
        PomodoroStartBtn.Background = Brush(_palette.TodoAccentButton);
        PomodoroStartBtn.Foreground = System.Windows.Media.Brushes.White;
        PomodoroResetBtn.Foreground = Brush(_palette.TodoLink);
        PomodoroResetBtn.BorderBrush = Brush(_palette.InputBorder);
        RefreshPomodoroUi();
    }

    private void InitializePomodoro()
    {
        ConfigurePomodoroService();
        _pomodoro.Tick += RefreshPomodoroUi;
        _pomodoro.PhaseChanged += _ => RefreshPomodoroUi();
        _pomodoro.Completed += OnPomodoroCompleted;
        RefreshPomodoroUi();
    }

    private void ConfigurePomodoroService()
    {
        _pomodoro.Configure(
            _settings.PomodoroWorkMinutes,
            _settings.PomodoroBreakMinutes,
            _settings.PomodoroLongBreakMinutes,
            _settings.PomodoroSessionsBeforeLongBreak);
    }

    private void RefreshPomodoroUi()
    {
        if (!_settings.ShowPomodoro)
        {
            return;
        }

        var remaining = _pomodoro.Remaining;
        PomodoroCountdownText.Text = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";

        PomodoroPhaseText.Text = _pomodoro.Phase switch
        {
            PomodoroPhase.Working => "专注中",
            PomodoroPhase.ShortBreak => "休息中",
            PomodoroPhase.LongBreak => "长休息",
            _ => "番茄钟"
        };

        var sessions = _pomodoro.CompletedWorkSessions;
        PomodoroSessionText.Text = sessions > 0 ? $"第 {sessions} 个番茄" : "准备开始";

        PomodoroStartBtn.Content = _pomodoro.Phase switch
        {
            PomodoroPhase.Idle => "开始",
            _ when _pomodoro.IsRunning => "暂停",
            _ => "继续"
        };

        var fillColor = _pomodoro.Phase is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak
            ? _palette.PomodoroBreak
            : _palette.PomodoroWork;
        PomodoroFill.Background = Brush(fillColor);
        QueueProgressBarUpdate(PomodoroTrack, PomodoroFill, _pomodoro.ProgressPercent);
    }

    private void OnPomodoroCompleted(PomodoroPhase phase)
    {
        if (!_settings.ShowPomodoro)
        {
            return;
        }

        switch (phase)
        {
            case PomodoroPhase.Working:
                var nextSession = _pomodoro.CompletedWorkSessions + 1;
                var breakMin = nextSession % _settings.PomodoroSessionsBeforeLongBreak == 0
                    ? _settings.PomodoroLongBreakMinutes
                    : _settings.PomodoroBreakMinutes;
                _tray?.ShowBalloon($"专注完成，休息 {breakMin} 分钟", "番茄钟");
                break;
            case PomodoroPhase.ShortBreak:
            case PomodoroPhase.LongBreak:
                _tray?.ShowBalloon("休息结束，开始下一个番茄", "番茄钟");
                break;
        }
    }

    private void PomodoroStart_Click(object sender, RoutedEventArgs e)
    {
        _pomodoro.StartOrToggle();
        RefreshPomodoroUi();
    }

    private void PomodoroReset_Click(object sender, RoutedEventArgs e)
    {
        _pomodoro.Reset();
        RefreshPomodoroUi();
    }

    private void QueueProgressBarUpdate(Border track, Border fill, double percent)
    {
        void Update()
        {
            if (track.ActualWidth <= 0)
            {
                return;
            }

            fill.Width = Math.Max(0, track.ActualWidth * percent / 100.0);
        }

        if (track.ActualWidth > 0)
        {
            Update();
            return;
        }

        void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            track.SizeChanged -= OnSizeChanged;
            Update();
        }

        track.SizeChanged += OnSizeChanged;
        Dispatcher.BeginInvoke(Update, DispatcherPriority.Loaded);
    }

    private void RefreshCalendar()
    {
        if (!_settings.ShowWeekStrip)
        {
            return;
        }

        CalendarTitleText.Text = CalendarViewHelper.GetTitle(_calendarMode, _calendarAnchor, _calendarPreviewDate);
        UpdateCalendarModeButtons();
        UpdateWindowHeight();

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
        var source = cache?.LocationSource ?? (_settings.AutoLocateCity ? "ip" : "manual");
        var sourceLabel = _settings.AutoLocateCity ? "自动定位" : source == "ip" ? "IP定位" : "手动";

        CityText.Text = string.IsNullOrWhiteSpace(region)
            ? $"📍 {city}（{sourceLabel}）"
            : $"📍 {city} · {region}（{sourceLabel}）";
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
            _settings.AutoLocateCity ? "ip" : oldCache?.LocationSource ?? "manual");
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

    private async Task InitializeWeatherAsync()
    {
        if (_settings.AutoLocateCity)
        {
            await AutoLocateCityAsync(notify: false);
            return;
        }

        await RefreshWeatherAsync();
    }

    private bool ShouldRefreshAutoLocate()
    {
        if (!DateTime.TryParse(_settings.LastAutoLocateAt, out var last))
        {
            return true;
        }

        return DateTime.Now - last >= TimeSpan.FromHours(24);
    }

    private async Task AutoLocateCityAsync(bool notify)
    {
        if (!_settings.ShowWeather)
        {
            return;
        }

        var loc = await _locationService.DetectByIpAsync();
        if (loc is null)
        {
            if (notify)
            {
                _tray?.ShowBalloon("自动定位失败，请检查网络或手动设置城市");
            }

            await RefreshWeatherAsync();
            return;
        }

        await ApplyDetectedLocationAsync(loc, notify);
    }

    private async Task ApplyDetectedLocationAsync(LocationService.DetectedLocation loc, bool notify)
    {
        _settings.City = loc.City;
        _settings.ResolvedCityName = loc.City;
        _settings.ResolvedRegion = loc.Region;
        _settings.WeatherLatitude = loc.Latitude;
        _settings.WeatherLongitude = loc.Longitude;
        _settings.LastAutoLocateAt = DateTime.Now.ToString("O");
        JsonStore.SaveSettings(_settings);

        var result = await _weatherService.FetchAsync(loc.City, loc.Latitude, loc.Longitude, loc.Region, "ip");
        _lastWeatherFetch = DateTime.Now;

        if (result is not null)
        {
            ApplyWeather(result.Cache);
            RefreshCityDisplay(result.Cache);
        }
        else
        {
            RefreshCityDisplay();
        }

        if (notify)
        {
            _tray?.ShowBalloon($"已定位到 {loc.City}");
        }
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
        var allToday = _todoStore.GetTodayActiveTodos();
        var shown = allToday.Take(_todoStore.MainPanelMax).Select(TodoDisplayItem.From).ToList();
        var hidden = _todoStore.GetTodayHiddenCount();

        TodoList.ItemsSource = shown;
        EmptyTodoPanel.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TodoCountText.Text = $"{shown.Count}/{allToday.Count}";

        if (hidden > 0)
        {
            TodoOverflowBtn.Visibility = Visibility.Visible;
            TodoOverflowText.Text = $"还有 {hidden} 条未显示 ›";
            TodoViewAllBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            TodoOverflowBtn.Visibility = Visibility.Collapsed;
            TodoViewAllBtn.Visibility = allToday.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public void OpenTodoListWindow()
    {
        if (_todoListWindow is { IsVisible: true })
        {
            _todoListWindow.Activate();
            return;
        }

        _todoListWindow = new TodoListWindow(_todoStore, _settings, RefreshTodos)
        {
            Owner = this
        };
        _todoListWindow.Closed += (_, _) => _todoListWindow = null;
        _todoListWindow.Show();
        Show();
        Activate();
    }

    private static void ParseTodoInput(string text, out string title, out string? time)
    {
        text = text.Trim();
        time = null;
        title = text;

        if (text.Length >= 5 && text[2] == ':' && int.TryParse(text[..2], out _))
        {
            time = text[..5];
            title = text[5..].TrimStart();
        }
    }

    private void EditTodo(string id)
    {
        var item = _todoStore.GetById(id);
        if (item is null)
        {
            return;
        }

        var result = TodoEditPrompt.Show("编辑待办", "修改待办内容与截止时间：", item, _palette);
        if (result is null || string.IsNullOrWhiteSpace(result.Title))
        {
            return;
        }

        _todoStore.Update(id, result.Title, result.ReminderTime, result.DueDate);
        RefreshTodos();
        _todoListWindow?.RefreshFromOutside();
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

        ParseTodoInput(text, out var title, out var time);
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

    public void OpenSettings()
    {
        var dlg = new SettingsWindow(_settings);
        dlg.Owner = this;
        if (dlg.ShowDialog() == true && dlg.Result is not null)
        {
            CommitSettings(dlg.Result);
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void CommitSettings(AppSettings next)
    {
        var prevAutoStart = _settings.AutoStart;
        var prevClickThrough = _settings.ClickThrough;
        var prevHotkey = _settings.EnableGlobalHotkey;
        var prevShowWeather = _settings.ShowWeather;
        var prevShowSeconds = _settings.ShowSeconds;
        var prevCity = _settings.City;
        var prevAutoLocate = _settings.AutoLocateCity;
        var prevSunrise = _settings.ShowSunriseSunset;
        var prevTomorrow = _settings.ShowTomorrowWeather;

        var left = _settings.Left;
        var top = _settings.Top;
        var lastAutoLocate = _settings.LastAutoLocateAt;
        var lat = _settings.WeatherLatitude;
        var lon = _settings.WeatherLongitude;
        var resolvedCity = _settings.ResolvedCityName;
        var resolvedRegion = _settings.ResolvedRegion;

        _settings.Time24h = next.Time24h;
        _settings.ShowSeconds = next.ShowSeconds;
        _settings.AlwaysOnTop = next.AlwaysOnTop;
        _settings.AutoStart = next.AutoStart;
        _settings.ClickThrough = next.ClickThrough;
        _settings.ShowWeather = next.ShowWeather;
        _settings.ShowCityName = next.ShowCityName;
        _settings.AutoLocateCity = next.AutoLocateCity;
        _settings.ShowWeekStrip = next.ShowWeekStrip;
        _settings.ShowHuangLi = next.ShowHuangLi;
        _settings.ShowYearProgress = next.ShowYearProgress;
        _settings.ShowCountdown = next.ShowCountdown;
        _settings.ShowPomodoro = next.ShowPomodoro;
        _settings.PomodoroWorkMinutes = next.PomodoroWorkMinutes;
        _settings.PomodoroBreakMinutes = next.PomodoroBreakMinutes;
        _settings.PomodoroLongBreakMinutes = next.PomodoroLongBreakMinutes;
        _settings.PomodoroSessionsBeforeLongBreak = next.PomodoroSessionsBeforeLongBreak;
        _settings.ShowDailyQuote = next.ShowDailyQuote;
        _settings.ShowSunriseSunset = next.ShowSunriseSunset;
        _settings.ShowTomorrowWeather = next.ShowTomorrowWeather;
        _settings.ShowScratch = next.ShowScratch;
        _settings.ShowTodoReminder = next.ShowTodoReminder;
        _settings.ModuleOrder = DeskModuleIds.Normalize(next.ModuleOrder);
        _settings.EnableGlobalHotkey = next.EnableGlobalHotkey;
        _settings.Theme = next.Theme;
        _settings.Opacity = next.Opacity;
        _settings.FontSizePt = next.FontSizePt;
        _settings.FontScale = next.FontScale;
        _settings.City = next.City;
        _settings.CalendarMode = next.CalendarMode;
        _settings.Left = left;
        _settings.Top = top;
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;

        var cityChanged = !string.Equals(prevCity, next.City, StringComparison.Ordinal);
        var autoLocateChanged = prevAutoLocate != next.AutoLocateCity;

        if (next.AutoLocateCity && autoLocateChanged)
        {
            // Re-detect on save.
        }
        else if (!next.AutoLocateCity && (cityChanged || autoLocateChanged))
        {
            _settings.ResolvedCityName = next.City;
            _settings.ResolvedRegion = null;
            _settings.WeatherLatitude = null;
            _settings.WeatherLongitude = null;
        }
        else
        {
            _settings.ResolvedCityName = resolvedCity;
            _settings.ResolvedRegion = resolvedRegion;
            _settings.WeatherLatitude = lat;
            _settings.WeatherLongitude = lon;
            _settings.LastAutoLocateAt = lastAutoLocate;
        }

        if (_settings.AutoStart != prevAutoStart)
        {
            AutoStartService.SetEnabled(_settings.AutoStart);
        }

        if (_settings.ClickThrough != prevClickThrough)
        {
            WindowHelper.SetClickThrough(this, _settings.ClickThrough);
            if (_settings.ClickThrough)
            {
                _tray?.ShowBalloon("已开启鼠标穿透，窗口无法拖动。请在设置中关闭。");
            }
        }

        if (_settings.EnableGlobalHotkey != prevHotkey)
        {
            if (_settings.EnableGlobalHotkey)
            {
                _hotkeyService ??= new GlobalHotkeyService(this, ToggleWindow, QuickAddTodo);
                _hotkeyService.Register();
            }
            else
            {
                _hotkeyService?.Unregister();
            }
        }

        _calendarMode = CalendarViewHelper.ParseMode(_settings.CalendarMode);

        ConfigurePomodoroService();
        ApplySettings();
        RefreshExtras();
        RefreshClock();
        if (_settings.ShowSeconds != prevShowSeconds)
        {
            ApplyClockTimerInterval();
        }

        RefreshCalendar();
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();

        var needWeatherRefresh = _settings.ShowWeather && (
            !prevShowWeather ||
            cityChanged ||
            autoLocateChanged ||
            prevSunrise != _settings.ShowSunriseSunset ||
            prevTomorrow != _settings.ShowTomorrowWeather);

        if (needWeatherRefresh)
        {
            if (_settings.AutoLocateCity && (autoLocateChanged || cityChanged))
            {
                _ = AutoLocateCityAsync(notify: false);
            }
            else
            {
                _ = RefreshWeatherAsync();
            }
        }
        else if (!_settings.ShowWeather)
        {
            RefreshCityDisplay(_weatherService.LoadCache());
        }
    }

    public void DetectLocationByIp() => _ = AutoLocateCityAsync(notify: true);

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
        _settings.AutoLocateCity = false;
        JsonStore.SaveSettings(_settings);
        _tray?.RefreshMenu();
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
        if (ActualWidth > 0 && ActualHeight > 0)
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }

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
        if (sender is not CircleCheckBox { TagId: string id })
        {
            return;
        }

        var titleBlock = FindTitleBlock(sender as DependencyObject, id);
        if (titleBlock is not null)
        {
            titleBlock.Foreground = Brush(_palette.TextEmpty);
            titleBlock.TextDecorations = TextDecorations.Strikethrough;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _todoStore.ToggleDone(id);
            RefreshTodos();
            _todoListWindow?.RefreshFromOutside();
        };
        timer.Start();
    }

    private static TextBlock? FindTitleBlock(DependencyObject? start, string id)
    {
        var border = FindAncestor<Border>(start);
        if (border is null)
        {
            return null;
        }

        return FindVisualChildren<TextBlock>(border).FirstOrDefault(tb => tb.Tag as string == id);
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private void TodoPin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string id })
        {
            var item = _todoStore.GetById(id);
            if (item is not null)
            {
                _todoStore.SetPinned(id, !item.Pinned);
                RefreshTodos();
            }
        }
    }

    private void TodoEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string id })
        {
            EditTodo(id);
        }
    }

    private void TodoTitle_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2 && sender is TextBlock { Tag: string id })
        {
            EditTodo(id);
            e.Handled = true;
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

    private void TodoViewAll_Click(object sender, RoutedEventArgs e) => OpenTodoListWindow();

    private void NewTodoBox_GotFocus(object sender, RoutedEventArgs e)
    {
        NewTodoBox.BorderBrush = Brush(_palette.Accent);
    }

    private void NewTodoBox_LostFocus(object sender, RoutedEventArgs e)
    {
        NewTodoBox.BorderBrush = Brush(_palette.InputBorder);
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e) => SetTheme(ThemeMode.Dark);

    private void ThemeLight_Click(object sender, RoutedEventArgs e) => SetTheme(ThemeMode.Light);

    private void Opacity100_Click(object sender, RoutedEventArgs e) => SetOpacity(1.0);

    private void Opacity85_Click(object sender, RoutedEventArgs e) => SetOpacity(0.85);

    private void Opacity70_Click(object sender, RoutedEventArgs e) => SetOpacity(0.70);

    private void Opacity55_Click(object sender, RoutedEventArgs e) => SetOpacity(0.55);
}
