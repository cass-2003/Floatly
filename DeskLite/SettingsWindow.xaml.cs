using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeskLite.Models;
using DeskLite.Services;
using Microsoft.Win32;

namespace DeskLite;

public partial class SettingsWindow : Window
{
    private sealed record ModuleOrderItem(string Id, string DisplayName);

    private const int MinOpacityPercent = 30;
    private const int MaxOpacityPercent = 100;

    private readonly AppSettings _original;
    private readonly LocationService _locationService = new();
    private bool _syncOpacity;
    private bool _syncFontSize;
    private bool _syncSkinOverlay;
    private bool _isInitializing = true;
    private System.Windows.Controls.TextBox? _recordingHotkeyBox;
    private string? _selectedFontColor;

    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        _original = Clone(settings);
        FontScaleHelper.NormalizeFontSettings(_original);
        _original.FontFamily = FontFamilyHelper.ResolveName(_original.FontFamily);
        _original.SkinMode = SkinService.NormalizeMode(_original.SkinMode);
        _original.SkinOverlayOpacity = SkinService.ClampOverlayOpacity(_original.SkinOverlayOpacity);
        InitializeComponent();
        FontFamilyHelper.Apply(this, _original.FontFamily);
        RbSkinDefault.Checked += (_, _) => UpdateSkinControls();
        RbSkinSolid.Checked += (_, _) => UpdateSkinControls();
        RbSkinImage.Checked += (_, _) => UpdateSkinControls();
        LoadFromSettings(_original);
        _isInitializing = false;
    }

    private static AppSettings Clone(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    private void LoadFromSettings(AppSettings s)
    {
        ChkAlwaysOnTop.IsChecked = s.AlwaysOnTop;
        ChkAutoStart.IsChecked = s.AutoStart;
        ChkClickThrough.IsChecked = s.ClickThrough;
        ChkGlobalHotkey.IsChecked = s.EnableGlobalHotkey;
        TxtHotkeyShowHide.Text = HotkeyComboHelper.Sanitize(s.HotkeyShowHide, HotkeyComboHelper.DefaultShowHide);
        TxtHotkeyQuickTodo.Text = HotkeyComboHelper.Sanitize(s.HotkeyQuickTodo, HotkeyComboHelper.DefaultQuickTodo);
        TxtWorkStartTime.Text = s.WorkStartTime;
        TxtWorkEndTime.Text = s.WorkEndTime;
        ChkOffWorkWeekdaysOnly.IsChecked = s.OffWorkWeekdaysOnly;
        TxtMonthlySalary.Text = s.MonthlySalary > 0 ? s.MonthlySalary.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        TxtWorkDaysPerMonth.Text = s.WorkDaysPerMonth.ToString();
        TxtWorkHoursPerDay.Text = s.WorkHoursPerDay.ToString("0.##", CultureInfo.InvariantCulture);
        ChkTime24h.IsChecked = s.Time24h;
        ChkShowSeconds.IsChecked = s.ShowSeconds;

        if (AppThemePalette.Parse(s.Theme) == ThemeMode.Light)
        {
            RbThemeLight.IsChecked = true;
        }
        else
        {
            RbThemeDark.IsChecked = true;
        }

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
        ChkAutoLocate.IsChecked = s.AutoLocateCity;
        TxtCity.Text = s.ResolvedCityName ?? s.City;

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

        ChkYearProgress.IsChecked = s.ShowYearProgress;
        ChkOffWorkCountdown.IsChecked = s.ShowOffWorkCountdown;
        ChkSalaryHelper.IsChecked = s.ShowSalaryHelper;
        ChkCountdown.IsChecked = s.ShowCountdown;
        ChkPomodoro.IsChecked = s.ShowPomodoro;
        TxtPomodoroWork.Text = s.PomodoroWorkMinutes.ToString();
        TxtPomodoroBreak.Text = s.PomodoroBreakMinutes.ToString();
        TxtPomodoroLongBreak.Text = s.PomodoroLongBreakMinutes.ToString();
        TxtPomodoroSessions.Text = s.PomodoroSessionsBeforeLongBreak.ToString();
        ChkDailyQuote.IsChecked = s.ShowDailyQuote;
        ChkScratch.IsChecked = s.ShowScratch;
        ChkTodoReminder.IsChecked = s.ShowTodoReminder;
        LoadModuleOrder(s.ModuleOrder);
    }

    private void SetOpacityUi(int percent)
    {
        if (SliderOpacity is null || TxtOpacity is null)
        {
            return;
        }

        _syncOpacity = true;
        SliderOpacity.Value = percent;
        TxtOpacity.Text = percent.ToString();
        _syncOpacity = false;
    }

    private void LoadFontFamilies(string? selected)
    {
        CmbFontFamily.Items.Clear();
        foreach (var family in FontFamilyHelper.GetSelectableFamilies())
        {
            CmbFontFamily.Items.Add(family);
        }

        CmbFontFamily.Text = FontFamilyHelper.ResolveName(selected);
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
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 8),
                Background = new SolidColorBrush(color.Value),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD1, 0xD5, 0xDB)),
                BorderThickness = new Thickness(1),
                Tag = hex,
                ToolTip = hex,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btn.Click += FontColorChip_Click;
            FontColorPalette.Children.Add(btn);
        }

        TxtCustomFontColor.Text = _selectedFontColor ?? string.Empty;
        UpdateFontColorSelection();
    }

    private void UpdateFontColorSelection()
    {
        foreach (var child in FontColorPalette.Children.OfType<System.Windows.Controls.Button>())
        {
            var hex = child.Tag as string;
            var selected = string.Equals(hex, _selectedFontColor, StringComparison.OrdinalIgnoreCase);
            child.BorderThickness = new Thickness(selected ? 2 : 1);
            child.BorderBrush = selected
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x25, 0x63, 0xEB))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD1, 0xD5, 0xDB));
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

    private void LoadSkinSettings(AppSettings s)
    {
        var mode = SkinService.NormalizeMode(s.SkinMode);
        RbSkinDefault.IsChecked = mode == SkinService.ModeDefault;
        RbSkinSolid.IsChecked = mode == SkinService.ModeSolid;
        RbSkinImage.IsChecked = mode == SkinService.ModeImage;
        TxtSkinImagePath.Text = s.SkinImagePath ?? string.Empty;
        SetSkinOverlayUi((int)Math.Round(SkinService.ClampOverlayOpacity(s.SkinOverlayOpacity) * 100));
        UpdateSkinControls();
    }

    private void UpdateSkinControls()
    {
        var imageMode = RbSkinImage.IsChecked == true;
        TxtSkinImagePath.IsEnabled = imageMode;
        BtnBrowseSkin.IsEnabled = imageMode;
        SliderSkinOverlay.IsEnabled = imageMode;
        TxtSkinOverlay.IsEnabled = imageMode;
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
        TxtSkinOverlay.Text = percent.ToString();
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
        TxtFontSize.Text = $"{pt}pt";
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
        box.Text = "请按下快捷键…";
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
                       _recordingHotkeyBox.Text == "请按下快捷键…"))
        {
            _recordingHotkeyBox.Text = _recordingHotkeyBox == TxtHotkeyShowHide
                ? HotkeyComboHelper.DefaultShowHide
                : HotkeyComboHelper.DefaultQuickTodo;
        }

        _recordingHotkeyBox = null;
        HotkeyStatusText.Text = "点击录制或聚焦输入框后按下组合键";
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

    private static string ReadWorkTime(string text, string fallback)
    {
        return OffWorkService.TryParseTime(text, out _) ? text.Trim() : fallback;
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

        s.WorkStartTime = ReadWorkTime(TxtWorkStartTime.Text, "09:00");
        s.WorkEndTime = ReadWorkTime(TxtWorkEndTime.Text, "18:00");
        s.OffWorkWeekdaysOnly = ChkOffWorkWeekdaysOnly.IsChecked == true;
        s.MonthlySalary = ReadMonthlySalary(TxtMonthlySalary.Text);
        s.WorkDaysPerMonth = ReadPositiveInt(TxtWorkDaysPerMonth.Text, 22, 1, 31);
        s.WorkHoursPerDay = ReadPositiveDouble(TxtWorkHoursPerDay.Text, 8, 1, 24);
        s.Time24h = ChkTime24h.IsChecked == true;
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
        s.SkinMode = RbSkinImage.IsChecked == true
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

        s.ShowWeather = ChkShowWeather.IsChecked == true;
        s.ShowCityName = ChkShowCityName.IsChecked == true;
        s.ShowSunriseSunset = ChkShowSunrise.IsChecked == true;
        s.ShowTomorrowWeather = ChkShowTomorrow.IsChecked == true;
        s.AutoLocateCity = ChkAutoLocate.IsChecked == true;
        s.City = TxtCity.Text.Trim();
        if (!string.IsNullOrWhiteSpace(s.City))
        {
            s.ResolvedCityName = s.City;
        }

        s.ShowWeekStrip = ChkShowWeekStrip.IsChecked == true;
        s.ShowHuangLi = ChkShowHuangLi.IsChecked == true;
        s.CalendarMode = RbCalMonth.IsChecked == true ? "month" : "week";

        s.ShowYearProgress = ChkYearProgress.IsChecked == true;
        s.ShowOffWorkCountdown = ChkOffWorkCountdown.IsChecked == true;
        s.ShowSalaryHelper = ChkSalaryHelper.IsChecked == true;
        s.ShowCountdown = ChkCountdown.IsChecked == true;
        s.ShowPomodoro = ChkPomodoro.IsChecked == true;
        s.PomodoroWorkMinutes = ReadPomodoroMinutes(TxtPomodoroWork.Text, 25, 1, 120);
        s.PomodoroBreakMinutes = ReadPomodoroMinutes(TxtPomodoroBreak.Text, 5, 1, 60);
        s.PomodoroLongBreakMinutes = ReadPomodoroMinutes(TxtPomodoroLongBreak.Text, 15, 1, 60);
        s.PomodoroSessionsBeforeLongBreak = ReadPomodoroMinutes(TxtPomodoroSessions.Text, 4, 2, 12);
        s.ShowDailyQuote = ChkDailyQuote.IsChecked == true;
        s.ShowScratch = ChkScratch.IsChecked == true;
        s.ShowTodoReminder = ChkTodoReminder.IsChecked == true;
        s.ModuleOrder = ReadModuleOrder();

        return s;
    }

    private void LoadModuleOrder(IEnumerable<string>? order)
    {
        ModuleOrderList.Items.Clear();
        foreach (var id in DeskModuleIds.Normalize(order))
        {
            ModuleOrderList.Items.Add(new ModuleOrderItem(id, DeskModuleIds.DisplayNames[id]));
        }

        ModuleOrderList.DisplayMemberPath = nameof(ModuleOrderItem.DisplayName);
        if (ModuleOrderList.Items.Count > 0)
        {
            ModuleOrderList.SelectedIndex = 0;
        }

        UpdateModuleOrderButtons();
    }

    private List<string> ReadModuleOrder()
    {
        return ModuleOrderList.Items
            .Cast<ModuleOrderItem>()
            .Select(item => item.Id)
            .ToList();
    }

    private void UpdateModuleOrderButtons()
    {
        var index = ModuleOrderList.SelectedIndex;
        var count = ModuleOrderList.Items.Count;
        BtnModuleUp.IsEnabled = index > 0;
        BtnModuleDown.IsEnabled = index >= 0 && index < count - 1;
    }

    private void ModuleOrderList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
        LocationStatusText.Text = "正在定位…";
        try
        {
            var loc = await _locationService.DetectByIpAsync();
            if (loc is null)
            {
                LocationStatusText.Text = "定位失败，请检查网络连接。";
                return;
            }

            TxtCity.Text = loc.City;
            ChkAutoLocate.IsChecked = true;
            LocationStatusText.Text = string.IsNullOrWhiteSpace(loc.Region)
                ? $"已定位：{loc.City}"
                : $"已定位：{loc.City} · {loc.Region}";
        }
        finally
        {
            BtnDetectLocation.IsEnabled = true;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtCustomFontColor.Text) && FontColorHelper.NormalizeHex(TxtCustomFontColor.Text) is null)
        {
            System.Windows.MessageBox.Show(this, "字体颜色格式无效，请使用 #RRGGBB。", AppConstants.DisplayName,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!OffWorkService.TryParseTime(TxtWorkStartTime.Text, out _) || !OffWorkService.TryParseTime(TxtWorkEndTime.Text, out _))
        {
            System.Windows.MessageBox.Show(this, "上下班时间格式无效，请使用 HH:mm（如 09:00）。", AppConstants.DisplayName,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtCity.Text) && ChkShowWeather.IsChecked == true && ChkAutoLocate.IsChecked != true)
        {
            System.Windows.MessageBox.Show(this, "请填写城市名称，或开启自动定位。", AppConstants.DisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Result = ReadToSettings();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
