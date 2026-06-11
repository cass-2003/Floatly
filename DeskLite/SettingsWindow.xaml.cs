using System.Text.Json;
using System.Windows;
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
            System.Windows.MessageBox.Show(this, "无法导入所选图片，请换一张 png/jpg/webp 图片。", "DeskLite",
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
        s.Time24h = ChkTime24h.IsChecked == true;
        s.ShowSeconds = ChkShowSeconds.IsChecked == true;

        s.Theme = RbThemeLight.IsChecked == true ? "light" : "dark";
        s.Opacity = ReadOpacityPercent() / 100.0;
        var fontPt = ReadFontSizePt();
        s.FontSizePt = fontPt;
        s.FontScale = FontScaleHelper.PtToScale(fontPt);
        s.FontFamily = FontFamilyHelper.ResolveName(CmbFontFamily.Text);
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
        if (string.IsNullOrWhiteSpace(TxtCity.Text) && ChkShowWeather.IsChecked == true && ChkAutoLocate.IsChecked != true)
        {
            System.Windows.MessageBox.Show(this, "请填写城市名称，或开启自动定位。", "DeskLite", MessageBoxButton.OK, MessageBoxImage.Information);
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
