using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeskLite.Models;
using DeskLite.Services;
using WpfColor = System.Windows.Media.Color;

namespace DeskLite;

public partial class SettingsWindow : Window
{
    private const string RepositoryUrl = "https://github.com/cass-2003/Floatly";

    private sealed class ModuleOrderItem
    {
        public string Id { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Icon { get; init; } = "\uECAA";
        public bool IsVisible { get; set; }
    }

    private static readonly string[] ConfigurableModuleIds =
    [
        DeskModuleIds.YearProgress,
        DeskModuleIds.OffWork,
        DeskModuleIds.Salary,
        DeskModuleIds.Countdown,
        DeskModuleIds.Pomodoro,
        DeskModuleIds.DailyQuote,
        DeskModuleIds.Scratch,
        DeskModuleIds.Todos
    ];

    private static readonly Dictionary<string, string> SettingsModuleDisplayNames =
        new(StringComparer.Ordinal)
        {
            [DeskModuleIds.YearProgress] = "年进度",
            [DeskModuleIds.OffWork] = "下班倒计时",
            [DeskModuleIds.Salary] = "摸鱼小助手",
            [DeskModuleIds.Countdown] = "倒数日",
            [DeskModuleIds.Pomodoro] = "番茄钟",
            [DeskModuleIds.DailyQuote] = "每日一句",
            [DeskModuleIds.Scratch] = "速记便签",
            [DeskModuleIds.Todos] = "待办提醒"
        };

    private static readonly Dictionary<string, string> SettingsModuleIcons =
        new(StringComparer.Ordinal)
        {
            [DeskModuleIds.YearProgress] = "\uE9D9",
            [DeskModuleIds.OffWork] = "\uEC92",
            [DeskModuleIds.Salary] = "\uEAFD",
            [DeskModuleIds.Countdown] = "\uE916",
            [DeskModuleIds.Pomodoro] = "\uE121",
            [DeskModuleIds.DailyQuote] = "\uE8BD",
            [DeskModuleIds.Scratch] = "\uE70F",
            [DeskModuleIds.Todos] = "\uE8FD"
        };

    private const int MinOpacityPercent = 30;
    private const int MaxOpacityPercent = 100;

    private AppSettings _original;
    private readonly LocationService _locationService = new();
    private bool _syncOpacity;
    private bool _syncFontSize;
    private bool _syncSkinOverlay;
    private bool _isInitializing = true;
    private System.Windows.Controls.TextBox? _recordingHotkeyBox;
    private string? _selectedFontColor;
    private IReadOnlyList<string> _availableFontFamilies = [];

    public AppSettings? Result { get; private set; }
    public event EventHandler<AppSettings>? SettingsApplied;
    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        _original = Clone(settings);
        FontScaleHelper.NormalizeFontSettings(_original);
        _original.FontFamily = FontFamilyHelper.ResolveName(_original.FontFamily);
        _original.SkinMode = SkinService.NormalizeMode(_original.SkinMode);
        _original.SkinOverlayOpacity = SkinService.ClampOverlayOpacity(_original.SkinOverlayOpacity);
        InitializeComponent();
        VersionText.Text = $"版本 {AppConstants.Version}";
        FontFamilyHelper.Apply(this, _original.FontFamily);
        SetupModuleListTemplate();
        RbSkinDefault.Checked += (_, _) => UpdateSkinControls();
        RbSkinSolid.Checked += (_, _) => UpdateSkinControls();
        RbSkinImage.Checked += (_, _) => UpdateSkinControls();
        RbSkinVideo.Checked += (_, _) => UpdateSkinControls();
        RbAutoLocate.Checked += (_, _) => UpdateCityControls();
        RbManualCity.Checked += (_, _) => UpdateCityControls();
        LoadFromSettings(_original);
        _isInitializing = false;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TitleMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleMaximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void TitleClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeIcon.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void SetupModuleListTemplate()
    {
        var template = new DataTemplate();

        var row = new FrameworkElementFactory(typeof(DockPanel));
        row.SetValue(FrameworkElement.HeightProperty, 22.0);
        row.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
        row.SetValue(DockPanel.LastChildFillProperty, true);

        var handle = new FrameworkElementFactory(typeof(TextBlock));
        handle.SetValue(TextBlock.TextProperty, "\uE712");
        handle.SetValue(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Segoe MDL2 Assets"));
        handle.SetValue(TextBlock.FontSizeProperty, 9.0);
        handle.SetValue(TextBlock.ForegroundProperty, FindResource("SettingsMuted"));
        handle.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        handle.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        handle.SetValue(FrameworkElement.WidthProperty, 16.0);
        handle.SetValue(DockPanel.DockProperty, Dock.Left);
        row.AppendChild(handle);

        var checkbox = new FrameworkElementFactory(typeof(System.Windows.Controls.CheckBox));
        checkbox.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty, new System.Windows.Data.Binding(nameof(ModuleOrderItem.IsVisible))
        {
            Mode = System.Windows.Data.BindingMode.TwoWay
        });
        checkbox.SetValue(FrameworkElement.StyleProperty, FindResource("SettingsCheckBox"));
        checkbox.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        checkbox.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        checkbox.SetValue(System.Windows.Controls.CheckBox.CursorProperty, System.Windows.Input.Cursors.Hand);
        checkbox.SetValue(DockPanel.DockProperty, Dock.Left);
        row.AppendChild(checkbox);

        var icon = new FrameworkElementFactory(typeof(TextBlock));
        icon.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(ModuleOrderItem.Icon)));
        icon.SetValue(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Segoe MDL2 Assets"));
        icon.SetValue(TextBlock.FontSizeProperty, 10.0);
        icon.SetValue(TextBlock.ForegroundProperty, FindResource("SettingsMuted"));
        icon.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        icon.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
        icon.SetValue(FrameworkElement.WidthProperty, 24.0);
        icon.SetValue(DockPanel.DockProperty, Dock.Right);
        row.AppendChild(icon);

        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(ModuleOrderItem.DisplayName)));
        name.SetValue(TextBlock.ForegroundProperty, FindResource("SettingsText"));
        name.SetValue(TextBlock.FontSizeProperty, 12.0);
        name.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
        row.AppendChild(name);

        template.VisualTree = row;
        ModuleOrderList.ItemTemplate = template;
    }

    private static AppSettings Clone(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    private static void SetTimePickerValue(HandyControl.Controls.TimePicker picker, string value, string fallback)
    {
        var normalized = OffWorkService.TryParseTime(value, out var time)
            ? time
            : OffWorkService.TryParseTime(fallback, out var fallbackTime)
                ? fallbackTime
                : new TimeOnly(9, 0);
        picker.SelectedTime = DateTime.Today.Add(normalized.ToTimeSpan());
    }

    private static string ReadTimePickerValue(HandyControl.Controls.TimePicker picker)
    {
        return (picker.SelectedTime ?? DateTime.Today).ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private void LoadFromSettings(AppSettings s)
    {
        ChkAlwaysOnTop.IsChecked = s.AlwaysOnTop;
        ChkAutoStart.IsChecked = s.AutoStart;
        ChkClickThrough.IsChecked = s.ClickThrough;
        ChkGlobalHotkey.IsChecked = s.EnableGlobalHotkey;
        TxtHotkeyShowHide.Text = HotkeyComboHelper.Sanitize(s.HotkeyShowHide, HotkeyComboHelper.DefaultShowHide);
        TxtHotkeyQuickTodo.Text = HotkeyComboHelper.Sanitize(s.HotkeyQuickTodo, HotkeyComboHelper.DefaultQuickTodo);
        SetTimePickerValue(WorkStartTimePicker, s.WorkStartTime, "09:00");
        SetTimePickerValue(WorkEndTimePicker, s.WorkEndTime, "18:00");
        ChkOffWorkWeekdaysOnly.IsChecked = s.OffWorkWeekdaysOnly;
        TxtMonthlySalary.Text = s.MonthlySalary > 0 ? s.MonthlySalary.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        TxtWorkDaysPerMonth.Text = s.WorkDaysPerMonth.ToString();
        TxtWorkHoursPerDay.Text = s.WorkHoursPerDay.ToString("0.##", CultureInfo.InvariantCulture);

        if (s.Time24h)
        {
            RbTime24h.IsChecked = true;
        }
        else
        {
            RbTime12h.IsChecked = true;
        }

        ChkShowSeconds.IsChecked = s.ShowSeconds;

        if (AppThemePalette.Parse(s.Theme) == ThemeMode.Light)
        {
            RbThemeLight.IsChecked = true;
        }
        else
        {
            RbThemeDark.IsChecked = true;
        }

        UpdateThemeCards();

        var opacityPercent = (int)Math.Round(Math.Clamp(s.Opacity, MinOpacityPercent / 100.0, 1.0) * 100);
        opacityPercent = Math.Clamp(opacityPercent, MinOpacityPercent, MaxOpacityPercent);
        SetOpacityUi(opacityPercent);

        var fontPt = FontScaleHelper.ResolvePt(s);
        SetFontSizeUi(fontPt);
        LoadFontFamilies(s.FontFamily);
        LoadFontColorSettings(s);
        LoadSkinSettings(s);

        ChkShowWeather.IsChecked = s.ShowWeather;
        ChkShowCityName.IsChecked = s.ShowCityName;
        ChkShowSunrise.IsChecked = s.ShowSunriseSunset;
        ChkShowTomorrow.IsChecked = s.ShowTomorrowWeather;
        if (s.AutoLocateCity)
        {
            RbAutoLocate.IsChecked = true;
        }
        else
        {
            RbManualCity.IsChecked = true;
        }

        TxtCity.Text = ResolveCityText(s);
        UpdateCityControls();
        UpdateLocationStatus(s);

        ChkShowWeekStrip.IsChecked = s.ShowWeekStrip;
        ChkShowHuangLi.IsChecked = s.ShowHuangLi;
        if (CalendarViewHelper.ParseMode(s.CalendarMode) == CalendarViewMode.Month)
        {
            RbCalMonth.IsChecked = true;
        }
        else
        {
            RbCalWeek.IsChecked = true;
        }

        TxtPomodoroWork.Text = s.PomodoroWorkMinutes.ToString();
        TxtPomodoroBreak.Text = s.PomodoroBreakMinutes.ToString();
        TxtPomodoroLongBreak.Text = s.PomodoroLongBreakMinutes.ToString();
        TxtPomodoroSessions.Text = s.PomodoroSessionsBeforeLongBreak.ToString();
        LoadModuleOrder(s);
    }

    private void ThemeDarkCard_Click(object sender, MouseButtonEventArgs e)
    {
        RbThemeDark.IsChecked = true;
        UpdateThemeCards();
    }

    private void ThemeLightCard_Click(object sender, MouseButtonEventArgs e)
    {
        RbThemeLight.IsChecked = true;
        UpdateThemeCards();
    }

    private void UpdateThemeCards()
    {
        var dark = RbThemeDark.IsChecked == true;
        ThemeDarkCard.BorderBrush = dark
            ? (System.Windows.Media.Brush)FindResource("SettingsAccent")
            : (System.Windows.Media.Brush)FindResource("SettingsBorder");
        ThemeLightCard.BorderBrush = dark
            ? (System.Windows.Media.Brush)FindResource("SettingsBorder")
            : (System.Windows.Media.Brush)FindResource("SettingsAccent");
        ThemeDarkCheck.Visibility = dark ? Visibility.Visible : Visibility.Collapsed;
        ThemeLightCheck.Visibility = dark ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SkinToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Primitives.ToggleButton btn || btn.Tag is not string tag)
        {
            return;
        }

        switch (tag)
        {
            case SkinService.ModeSolid:
                RbSkinSolid.IsChecked = true;
                break;
            case SkinService.ModeImage:
                RbSkinImage.IsChecked = true;
                break;
            case SkinService.ModeVideo:
                RbSkinVideo.IsChecked = true;
                break;
            default:
                RbSkinDefault.IsChecked = true;
                break;
        }

        UpdateSkinToggles();
        UpdateSkinControls();
    }

    private void UpdateSkinToggles()
    {
        var mode = RbSkinVideo.IsChecked == true
            ? SkinService.ModeVideo
            : RbSkinImage.IsChecked == true
                ? SkinService.ModeImage
                : RbSkinSolid.IsChecked == true
                    ? SkinService.ModeSolid
                    : SkinService.ModeDefault;

        TbSkinDefault.IsChecked = mode == SkinService.ModeDefault;
        TbSkinSolid.IsChecked = mode == SkinService.ModeSolid;
        TbSkinImage.IsChecked = mode == SkinService.ModeImage;
        TbSkinVideo.IsChecked = mode == SkinService.ModeVideo;

        var accent = (System.Windows.Media.Brush)FindResource("SettingsAccent");
        var border = (System.Windows.Media.Brush)FindResource("SettingsBorder");
        foreach (var tb in new[] { TbSkinDefault, TbSkinSolid, TbSkinImage, TbSkinVideo })
        {
            tb.BorderBrush = tb.IsChecked == true ? accent : border;
            tb.BorderThickness = new Thickness(tb.IsChecked == true ? 2 : 1);
        }
    }

    private void UpdateCityControls()
    {
        var auto = RbAutoLocate.IsChecked == true;
        TxtCity.IsEnabled = !auto;
    }

    private static string ResolveCityText(AppSettings s)
    {
        if (LocationService.HasUsableCityName(s.ResolvedCityName))
        {
            return s.ResolvedCityName!;
        }

        if (LocationService.HasUsableCityName(s.City))
        {
            return s.City;
        }

        return string.Empty;
    }

    private void UpdateLocationStatus(AppSettings s)
    {
        var cache = new WeatherService().LoadCache();
        var source = cache?.LocationSource;
        var method = s.AutoLocateCity
            ? LocationService.DescribeSource(string.IsNullOrWhiteSpace(source) ? "auto" : source)
            : LocationService.DescribeSource("manual");
        var showIpWarning = s.AutoLocateCity &&
                            (string.Equals(source, "ip", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(source, "hybrid", StringComparison.OrdinalIgnoreCase));
        LocationIpWarningText.Visibility = showIpWarning ? Visibility.Visible : Visibility.Collapsed;

        if (LocationService.HasUsableCityName(s.ResolvedCityName))
        {
            LocationStatusText.Text = string.IsNullOrWhiteSpace(s.ResolvedRegion)
                ? $"定位方式：{method} · {s.ResolvedCityName}"
                : $"定位方式：{method} · {s.ResolvedCityName}, {s.ResolvedRegion}";
        }
        else if (LocationService.HasUsableCityName(s.City))
        {
            LocationStatusText.Text = s.AutoLocateCity
                ? $"定位方式：{method} · {s.City}"
                : $"定位方式：手动 · {s.City}";
        }
        else if (s.AutoLocateCity && s.WeatherLatitude is not null && s.WeatherLongitude is not null)
        {
            LocationStatusText.Text = $"定位方式：{method} · 已获取坐标，正在刷新城市名";
        }
        else
        {
            LocationStatusText.Text = s.AutoLocateCity
                ? "定位方式：等待自动定位"
                : "定位方式：手动 · 请输入城市";
        }
    }

    private void UpdateLocationStatus(LocationService.DetectedLocation loc)
    {
        var method = LocationService.DescribeSource(loc.Source);
        LocationIpWarningText.Visibility = loc.IpFallbackWarning ? Visibility.Visible : Visibility.Collapsed;
        LocationStatusText.Text = string.IsNullOrWhiteSpace(loc.Region)
            ? $"定位方式：{method} · {loc.City}"
            : $"定位方式：{method} · {loc.City}, {loc.Region}";
    }

    private void SetOpacityUi(int percent)
    {
        if (SliderOpacity is null || TxtOpacity is null)
        {
            return;
        }

        _syncOpacity = true;
        SliderOpacity.Value = percent;
        TxtOpacity.Text = $"{percent}%";
        _syncOpacity = false;
    }

    private void LoadFontFamilies(string? selected)
    {
        _availableFontFamilies = FontFamilyHelper.GetSelectableFamilies();
        ApplyFontFamilyFilter(string.Empty);
        CmbFontFamily.Text = FontFamilyHelper.ResolveName(selected);
    }

    private void ApplyFontFamilyFilter(string filter)
    {
        CmbFontFamily.Items.Clear();
        var query = filter.Trim();
        var families = string.IsNullOrWhiteSpace(query)
            ? _availableFontFamilies
            : _availableFontFamilies
                .Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(80)
                .ToList();

        foreach (var family in families)
        {
            CmbFontFamily.Items.Add(family);
        }
    }

    private void LoadFontColorSettings(AppSettings s)
    {
        FontColorPalette.Children.Clear();
        _selectedFontColor = FontColorHelper.NormalizeHex(s.PrimaryTextColor);

        foreach (var hex in FontColorHelper.PresetHexColors)
        {
            var color = FontColorHelper.TryParseHex(hex);
            if (color is null)
            {
                continue;
            }

            var btn = new System.Windows.Controls.Button
            {
                Background = new SolidColorBrush(color.Value),
                BorderBrush = new SolidColorBrush(WpfColor.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                Tag = hex,
                ToolTip = hex,
                Style = (Style)FindResource("ColorChipButton")
            };
            btn.Click += FontColorChip_Click;
            FontColorPalette.Children.Add(btn);
        }

        TxtCustomFontColor.Text = _selectedFontColor ?? "#FFFFFF";
        UpdateFontColorSelection();
    }

    private void UpdateFontColorSelection()
    {
        var accent = WpfColor.FromRgb(0x5C, 0x8D, 0xFF);
        var border = WpfColor.FromArgb(0x40, 0xFF, 0xFF, 0xFF);

        foreach (var child in FontColorPalette.Children.OfType<System.Windows.Controls.Button>())
        {
            var hex = child.Tag as string;
            var selected = string.Equals(hex, _selectedFontColor, StringComparison.OrdinalIgnoreCase);
            child.BorderThickness = new Thickness(selected ? 2 : 1);
            child.BorderBrush = new SolidColorBrush(selected ? accent : border);
        }
    }

    private void FontColorChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string hex)
        {
            return;
        }

        _selectedFontColor = hex;
        TxtCustomFontColor.Text = hex;
        UpdateFontColorSelection();
    }

    private void BtnClearFontColor_Click(object sender, RoutedEventArgs e)
    {
        _selectedFontColor = null;
        TxtCustomFontColor.Text = string.Empty;
        UpdateFontColorSelection();
    }

    private void TxtFontSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        FontSearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(TxtFontSearch.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_isInitializing)
        {
            return;
        }

        ApplyFontFamilyFilter(TxtFontSearch.Text);
    }

    private void LoadSkinSettings(AppSettings s)
    {
        var mode = SkinService.NormalizeMode(s.SkinMode);
        RbSkinDefault.IsChecked = mode == SkinService.ModeDefault;
        RbSkinSolid.IsChecked = mode == SkinService.ModeSolid;
        RbSkinImage.IsChecked = mode == SkinService.ModeImage;
        RbSkinVideo.IsChecked = mode == SkinService.ModeVideo;
        TxtSkinImagePath.Text = s.SkinImagePath ?? string.Empty;
        TxtSkinVideoPath.Text = s.SkinVideoPath ?? string.Empty;
        SetSkinOverlayUi((int)Math.Round(SkinService.ClampOverlayOpacity(s.SkinOverlayOpacity) * 100));
        UpdateSkinToggles();
        UpdateSkinControls();
    }

    private void UpdateSkinControls()
    {
        var imageMode = RbSkinImage.IsChecked == true;
        var videoMode = RbSkinVideo.IsChecked == true;
        var mediaOverlay = imageMode || videoMode;
        TxtSkinImagePath.IsEnabled = imageMode;
        BtnBrowseSkin.IsEnabled = imageMode;
        TxtSkinVideoPath.IsEnabled = videoMode;
        BtnBrowseSkinVideo.IsEnabled = videoMode;
        SkinImagePathRow.Visibility = imageMode ? Visibility.Visible : Visibility.Collapsed;
        SkinVideoPathRow.Visibility = videoMode ? Visibility.Visible : Visibility.Collapsed;
        SliderSkinOverlay.IsEnabled = mediaOverlay;
        TxtSkinOverlay.IsEnabled = mediaOverlay;
    }

    private void SetSkinOverlayUi(int percent)
    {
        if (SliderSkinOverlay is null || TxtSkinOverlay is null)
        {
            return;
        }

        percent = Math.Clamp(percent, 0, 85);
        _syncSkinOverlay = true;
        SliderSkinOverlay.Value = percent;
        TxtSkinOverlay.Text = $"{percent}%";
        _syncSkinOverlay = false;
    }

    private int ReadSkinOverlayPercent()
    {
        var text = TxtSkinOverlay.Text.Trim().TrimEnd('%');
        if (int.TryParse(text, out var value))
        {
            return Math.Clamp(value, 0, 85);
        }

        return (int)SliderSkinOverlay.Value;
    }

    private void SetFontSizeUi(int pt)
    {
        if (SliderFontSize is null || TxtFontSize is null)
        {
            return;
        }

        _syncFontSize = true;
        SliderFontSize.Value = pt;
        TxtFontSize.Text = $"{pt} pt";
        _syncFontSize = false;
    }

    private int ReadOpacityPercent()
    {
        var text = TxtOpacity.Text.Trim().TrimEnd('%');
        if (int.TryParse(text, out var value))
        {
            return Math.Clamp(value, MinOpacityPercent, MaxOpacityPercent);
        }

        return (int)SliderOpacity.Value;
    }

    private int ReadFontSizePt()
    {
        var text = TxtFontSize.Text.Trim().ToLowerInvariant().Replace("pt", string.Empty).Trim();
        if (int.TryParse(text, out var value))
        {
            return Math.Clamp(value, FontScaleHelper.MinFontSizePt, FontScaleHelper.MaxFontSizePt);
        }

        return (int)SliderFontSize.Value;
    }

    private void BtnRecordShowHide_Click(object sender, RoutedEventArgs e) => BeginHotkeyRecording(TxtHotkeyShowHide);

    private void BtnRecordQuickTodo_Click(object sender, RoutedEventArgs e) => BeginHotkeyRecording(TxtHotkeyQuickTodo);

    private void HotkeyShowHide_GotFocus(object sender, RoutedEventArgs e) => BeginHotkeyRecording(TxtHotkeyShowHide);

    private void HotkeyQuickTodo_GotFocus(object sender, RoutedEventArgs e) => BeginHotkeyRecording(TxtHotkeyQuickTodo);

    private void BeginHotkeyRecording(System.Windows.Controls.TextBox box)
    {
        _recordingHotkeyBox = box;
        box.Text = "请按下快捷键...";
        HotkeyStatusText.Text = "正在录制，请按下组合键（需含 Ctrl/Alt/Shift/Win）";
        box.Focus();
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box || _recordingHotkeyBox != box)
        {
            return;
        }

        e.Handled = true;
        if (e.Key == Key.Escape)
        {
            EndHotkeyRecording(cancel: true);
            return;
        }

        if (!HotkeyComboHelper.TryFormatFromInput(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers, out var display))
        {
            return;
        }

        box.Text = display;
        ValidateHotkeys();
        EndHotkeyRecording(cancel: false);
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox box && _recordingHotkeyBox == box)
        {
            EndHotkeyRecording(cancel: true);
        }
    }

    private void EndHotkeyRecording(bool cancel)
    {
        if (_recordingHotkeyBox is null)
        {
            return;
        }

        if (cancel && (string.IsNullOrWhiteSpace(_recordingHotkeyBox.Text) ||
                       _recordingHotkeyBox.Text == "请按下快捷键..."))
        {
            _recordingHotkeyBox.Text = _recordingHotkeyBox == TxtHotkeyShowHide
                ? HotkeyComboHelper.DefaultShowHide
                : HotkeyComboHelper.DefaultQuickTodo;
        }

        _recordingHotkeyBox = null;
        HotkeyStatusText.Text = "录制模式：点击红色按钮或聚焦输入框后按下组合键";
        ValidateHotkeys();
    }

    private void ValidateHotkeys()
    {
        var showHide = HotkeyComboHelper.Sanitize(TxtHotkeyShowHide.Text, HotkeyComboHelper.DefaultShowHide);
        var quickTodo = HotkeyComboHelper.Sanitize(TxtHotkeyQuickTodo.Text, HotkeyComboHelper.DefaultQuickTodo);
        TxtHotkeyShowHide.Text = showHide;
        TxtHotkeyQuickTodo.Text = quickTodo;

        if (HotkeyComboHelper.Conflicts(showHide, quickTodo))
        {
            HotkeyStatusText.Text = "两个快捷键冲突，快速待办将恢复为 Ctrl+Shift+N";
            TxtHotkeyQuickTodo.Text = HotkeyComboHelper.DefaultQuickTodo;
        }
        else if (!HotkeyComboHelper.TryParse(showHide, out _) || !HotkeyComboHelper.TryParse(TxtHotkeyQuickTodo.Text, out _))
        {
            HotkeyStatusText.Text = "快捷键无效，已恢复为默认值";
        }
        else
        {
            HotkeyStatusText.Text = "快捷键已设置";
        }
    }

    private static decimal ReadMonthlySalary(string text)
    {
        return decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? Math.Max(0, value)
            : 0;
    }

    private static int ReadPositiveInt(string text, int fallback, int min, int max)
    {
        if (int.TryParse(text.Trim(), out var value))
        {
            return Math.Clamp(value, min, max);
        }

        return fallback;
    }

    private static double ReadPositiveDouble(string text, double fallback, double min, double max)
    {
        if (double.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return Math.Clamp(value, min, max);
        }

        return fallback;
    }

    private static int ReadPomodoroMinutes(string text, int fallback, int min, int max)
    {
        if (int.TryParse(text.Trim(), out var value))
        {
            return Math.Clamp(value, min, max);
        }

        return fallback;
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || _syncOpacity || TxtOpacity is null)
        {
            return;
        }

        SetOpacityUi((int)SliderOpacity.Value);
    }

    private void SliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || _syncFontSize || TxtFontSize is null)
        {
            return;
        }

        SetFontSizeUi((int)SliderFontSize.Value);
    }

    private void TxtOpacity_LostFocus(object sender, RoutedEventArgs e) => CommitOpacityText();

    private void TxtOpacity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitOpacityText();
        }
    }

    private void CommitOpacityText()
    {
        SetOpacityUi(ReadOpacityPercent());
    }

    private void TxtFontSize_LostFocus(object sender, RoutedEventArgs e) => CommitFontSizeText();

    private void TxtFontSize_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitFontSizeText();
        }
    }

    private void CommitFontSizeText()
    {
        SetFontSizeUi(ReadFontSizePt());
    }

    private void SliderSkinOverlay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || _syncSkinOverlay || TxtSkinOverlay is null)
        {
            return;
        }

        SetSkinOverlayUi((int)SliderSkinOverlay.Value);
    }

    private void TxtSkinOverlay_LostFocus(object sender, RoutedEventArgs e) => CommitSkinOverlayText();

    private void TxtSkinOverlay_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            CommitSkinOverlayText();
        }
    }

    private void CommitSkinOverlayText()
    {
        SetSkinOverlayUi(ReadSkinOverlayPercent());
    }

    private void BtnBrowseSkin_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择皮肤背景图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.webp|所有文件|*.*"
        };

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        var imported = SkinService.ImportSkinImage(dlg.FileName);
        if (imported is null)
        {
            System.Windows.MessageBox.Show(this, "无法导入所选图片，请换一张 png/jpg/webp 图片。", AppConstants.DisplayName,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtSkinImagePath.Text = imported;
        RbSkinImage.IsChecked = true;
        UpdateSkinToggles();
        UpdateSkinControls();
    }

    private void BtnBrowseSkinVideo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择皮肤背景视频",
            Filter = "视频文件|*.mp4;*.webm;*.wmv;*.avi;*.mov|所有文件|*.*"
        };

        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        if (SkinService.ShouldWarnLargeVideo(dlg.FileName))
        {
            var sizeMb = new FileInfo(dlg.FileName).Length / (1024.0 * 1024.0);
            var confirm = System.Windows.MessageBox.Show(
                this,
                $"所选视频约 {sizeMb:F1} MB，超过 50 MB 建议值，可能占用较多资源。是否继续？",
                AppConstants.DisplayName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var imported = SkinService.ImportSkinVideo(dlg.FileName);
        if (imported is null)
        {
            System.Windows.MessageBox.Show(this, "无法导入所选视频，请换一个 mp4/webm/wmv/avi/mov 文件。", AppConstants.DisplayName,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtSkinVideoPath.Text = imported;
        RbSkinVideo.IsChecked = true;
        UpdateSkinToggles();
        UpdateSkinControls();
    }

    private AppSettings ReadToSettings()
    {
        var s = Clone(_original);

        s.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
        s.AutoStart = ChkAutoStart.IsChecked == true;
        s.ClickThrough = ChkClickThrough.IsChecked == true;
        s.EnableGlobalHotkey = ChkGlobalHotkey.IsChecked == true;
        s.HotkeyShowHide = HotkeyComboHelper.Sanitize(TxtHotkeyShowHide.Text, HotkeyComboHelper.DefaultShowHide);
        s.HotkeyQuickTodo = HotkeyComboHelper.Sanitize(TxtHotkeyQuickTodo.Text, HotkeyComboHelper.DefaultQuickTodo);
        if (HotkeyComboHelper.Conflicts(s.HotkeyShowHide, s.HotkeyQuickTodo))
        {
            s.HotkeyQuickTodo = HotkeyComboHelper.DefaultQuickTodo;
        }

        s.WorkStartTime = ReadTimePickerValue(WorkStartTimePicker);
        s.WorkEndTime = ReadTimePickerValue(WorkEndTimePicker);
        s.OffWorkWeekdaysOnly = ChkOffWorkWeekdaysOnly.IsChecked == true;
        s.MonthlySalary = ReadMonthlySalary(TxtMonthlySalary.Text);
        s.WorkDaysPerMonth = ReadPositiveInt(TxtWorkDaysPerMonth.Text, 22, 1, 31);
        s.WorkHoursPerDay = ReadPositiveDouble(TxtWorkHoursPerDay.Text, 8, 1, 24);
        s.Time24h = RbTime24h.IsChecked == true;
        s.ShowSeconds = ChkShowSeconds.IsChecked == true;

        s.Theme = RbThemeLight.IsChecked == true ? "light" : "dark";
        s.Opacity = ReadOpacityPercent() / 100.0;
        var fontPt = ReadFontSizePt();
        s.FontSizePt = fontPt;
        s.FontScale = FontScaleHelper.PtToScale(fontPt);
        s.FontFamily = FontFamilyHelper.ResolveName(CmbFontFamily.Text);
        var customColor = FontColorHelper.NormalizeHex(TxtCustomFontColor.Text);
        s.PrimaryTextColor = string.IsNullOrWhiteSpace(TxtCustomFontColor.Text)
            ? null
            : customColor ?? _selectedFontColor;
        s.SkinMode = RbSkinVideo.IsChecked == true
            ? SkinService.ModeVideo
            : RbSkinImage.IsChecked == true
                ? SkinService.ModeImage
                : RbSkinSolid.IsChecked == true
                    ? SkinService.ModeSolid
                    : SkinService.ModeDefault;
        s.SkinOverlayOpacity = ReadSkinOverlayPercent() / 100.0;
        if (s.SkinMode != SkinService.ModeImage)
        {
            s.SkinImagePath = string.IsNullOrWhiteSpace(TxtSkinImagePath.Text) ? null : TxtSkinImagePath.Text.Trim();
        }
        else if (string.IsNullOrWhiteSpace(TxtSkinImagePath.Text))
        {
            s.SkinImagePath = null;
        }
        else
        {
            s.SkinImagePath = TxtSkinImagePath.Text.Trim();
        }

        if (s.SkinMode != SkinService.ModeVideo)
        {
            s.SkinVideoPath = string.IsNullOrWhiteSpace(TxtSkinVideoPath.Text) ? null : TxtSkinVideoPath.Text.Trim();
        }
        else if (string.IsNullOrWhiteSpace(TxtSkinVideoPath.Text))
        {
            s.SkinVideoPath = null;
        }
        else
        {
            s.SkinVideoPath = TxtSkinVideoPath.Text.Trim();
        }

        s.ShowWeather = ChkShowWeather.IsChecked == true;
        s.ShowCityName = ChkShowCityName.IsChecked == true;
        s.ShowSunriseSunset = ChkShowSunrise.IsChecked == true;
        s.ShowTomorrowWeather = ChkShowTomorrow.IsChecked == true;
        s.AutoLocateCity = RbAutoLocate.IsChecked == true;
        var cityText = TxtCity.Text.Trim();
        if (LocationService.HasUsableCityName(cityText))
        {
            s.City = cityText;
            s.ResolvedCityName = cityText;
        }
        else if (s.AutoLocateCity)
        {
            s.City = LocationService.HasUsableCityName(_original.City) ? _original.City : string.Empty;
            s.ResolvedCityName = LocationService.HasUsableCityName(_original.ResolvedCityName)
                ? _original.ResolvedCityName
                : s.ResolvedCityName;
        }
        else
        {
            s.City = cityText;
        }

        s.ShowWeekStrip = ChkShowWeekStrip.IsChecked == true;
        s.ShowHuangLi = ChkShowHuangLi.IsChecked == true;
        s.CalendarMode = RbCalMonth.IsChecked == true ? "month" : "week";

        ApplyModuleListToSettings(s);
        s.PomodoroWorkMinutes = ReadPomodoroMinutes(TxtPomodoroWork.Text, 25, 1, 120);
        s.PomodoroBreakMinutes = ReadPomodoroMinutes(TxtPomodoroBreak.Text, 5, 1, 60);
        s.PomodoroLongBreakMinutes = ReadPomodoroMinutes(TxtPomodoroLongBreak.Text, 15, 1, 60);
        s.PomodoroSessionsBeforeLongBreak = ReadPomodoroMinutes(TxtPomodoroSessions.Text, 4, 2, 12);
        s.ModuleOrder = ReadModuleOrder();

        return s;
    }

    private static bool GetModuleVisibility(string id, AppSettings s) => id switch
    {
        DeskModuleIds.YearProgress => s.ShowYearProgress,
        DeskModuleIds.OffWork => s.ShowOffWorkCountdown,
        DeskModuleIds.Salary => s.ShowSalaryHelper,
        DeskModuleIds.Countdown => s.ShowCountdown,
        DeskModuleIds.Pomodoro => s.ShowPomodoro,
        DeskModuleIds.DailyQuote => s.ShowDailyQuote,
        DeskModuleIds.Scratch => s.ShowScratch,
        DeskModuleIds.Todos => s.ShowTodoReminder,
        _ => true
    };

    private static void SetModuleVisibility(string id, AppSettings s, bool visible)
    {
        switch (id)
        {
            case DeskModuleIds.YearProgress:
                s.ShowYearProgress = visible;
                break;
            case DeskModuleIds.OffWork:
                s.ShowOffWorkCountdown = visible;
                break;
            case DeskModuleIds.Salary:
                s.ShowSalaryHelper = visible;
                break;
            case DeskModuleIds.Countdown:
                s.ShowCountdown = visible;
                break;
            case DeskModuleIds.Pomodoro:
                s.ShowPomodoro = visible;
                break;
            case DeskModuleIds.DailyQuote:
                s.ShowDailyQuote = visible;
                break;
            case DeskModuleIds.Scratch:
                s.ShowScratch = visible;
                break;
            case DeskModuleIds.Todos:
                s.ShowTodoReminder = visible;
                break;
        }
    }

    private void ApplyModuleListToSettings(AppSettings s)
    {
        foreach (var item in ModuleOrderList.Items.Cast<ModuleOrderItem>())
        {
            SetModuleVisibility(item.Id, s, item.IsVisible);
        }
    }

    private void LoadModuleOrder(AppSettings s)
    {
        ModuleOrderList.Items.Clear();
        var normalized = DeskModuleIds.Normalize(s.ModuleOrder);
        var ordered = normalized.Where(id => ConfigurableModuleIds.Contains(id)).ToList();
        foreach (var id in ConfigurableModuleIds)
        {
            if (!ordered.Contains(id))
            {
                ordered.Add(id);
            }
        }

        foreach (var id in ordered)
        {
            ModuleOrderList.Items.Add(new ModuleOrderItem
            {
                Id = id,
                DisplayName = SettingsModuleDisplayNames[id],
                Icon = SettingsModuleIcons.GetValueOrDefault(id, "\uECAA"),
                IsVisible = GetModuleVisibility(id, s)
            });
        }

        if (ModuleOrderList.Items.Count > 0)
        {
            ModuleOrderList.SelectedIndex = 0;
        }

        UpdateModuleOrderButtons();
    }

    private List<string> ReadModuleOrder()
    {
        var fromList = ModuleOrderList.Items
            .Cast<ModuleOrderItem>()
            .Select(item => item.Id)
            .ToList();
        var allNormalized = DeskModuleIds.Normalize(_original.ModuleOrder);
        var remaining = allNormalized.Where(id => !fromList.Contains(id)).ToList();
        return fromList.Concat(remaining).ToList();
    }

    private void UpdateModuleOrderButtons()
    {
        var index = ModuleOrderList.SelectedIndex;
        var count = ModuleOrderList.Items.Count;
        BtnModuleUp.IsEnabled = index > 0;
        BtnModuleDown.IsEnabled = index >= 0 && index < count - 1;
    }

    private void ModuleOrderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModuleOrderButtons();
    }

    private void BtnModuleUp_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedModule(-1);
    }

    private void BtnModuleDown_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedModule(1);
    }

    private void BtnResetModuleOrder_Click(object sender, RoutedEventArgs e)
    {
        var current = ReadToSettings();
        current.ModuleOrder = [.. DeskModuleIds.DefaultOrder];
        LoadModuleOrder(current);
    }

    private void MoveSelectedModule(int delta)
    {
        var index = ModuleOrderList.SelectedIndex;
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= ModuleOrderList.Items.Count)
        {
            return;
        }

        var item = ModuleOrderList.Items[index];
        ModuleOrderList.Items.RemoveAt(index);
        ModuleOrderList.Items.Insert(newIndex, item);
        ModuleOrderList.SelectedIndex = newIndex;
        UpdateModuleOrderButtons();
    }

    private async void BtnDetectLocation_Click(object sender, RoutedEventArgs e)
    {
        BtnDetectLocation.IsEnabled = false;
        LocationIpWarningText.Visibility = Visibility.Collapsed;
        LocationStatusText.Text = "定位方式：正在使用 Windows 定位";
        try
        {
            var loc = await _locationService.TryDetectByWindowsAsync();
            if (loc is null)
            {
                LocationStatusText.Text = "定位方式：Windows 定位不可用，正在尝试 IP 定位（备用）";
                loc = await _locationService.DetectByIpAsync();
                if (loc is not null)
                {
                    loc = loc with { IpFallbackWarning = true };
                }
            }

            if (loc is null)
            {
                LocationStatusText.Text = "定位失败：请检查 Windows 位置权限、网络连接，或改用手动输入城市";
                return;
            }

            TxtCity.Text = loc.City;
            RbAutoLocate.IsChecked = true;
            UpdateCityControls();
            UpdateLocationStatus(loc);
        }
        finally
        {
            BtnDetectLocation.IsEnabled = true;
        }
    }

    private bool TryValidateInputs()
    {
        if (!string.IsNullOrWhiteSpace(TxtCustomFontColor.Text) && FontColorHelper.NormalizeHex(TxtCustomFontColor.Text) is null)
        {
            System.Windows.MessageBox.Show(this, "字体颜色格式无效，请使用 #RRGGBB。", AppConstants.DisplayName,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtCity.Text) && ChkShowWeather.IsChecked == true && RbAutoLocate.IsChecked != true)
        {
            System.Windows.MessageBox.Show(this, "请填写城市名称，或开启自动定位。", AppConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        return true;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateInputs())
        {
            return;
        }

        var settings = ReadToSettings();
        Result = settings;
        _original = Clone(settings);
        SettingsApplied?.Invoke(this, settings);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateInputs())
        {
            return;
        }

        Result = ReadToSettings();
        _original = Clone(Result);
        SettingsSaved?.Invoke(this, Result);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnRestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show(
            this,
            "确定要恢复所有设置为默认值吗？此操作在点击「应用」或「保存设置」后才会生效。",
            AppConstants.DisplayName,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        LoadFromSettings(new AppSettings());
    }

    private void BtnOpenRepository_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                this,
                $"无法打开仓库链接：{ex.Message}",
                AppConstants.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}

