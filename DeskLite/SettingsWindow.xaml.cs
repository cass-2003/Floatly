using System.Text.Json;
using System.Windows;
using DeskLite.Models;
using DeskLite.Services;

namespace DeskLite;

public partial class SettingsWindow : Window
{
    private sealed record ModuleOrderItem(string Id, string DisplayName);
    private static readonly (string Label, double Value)[] OpacityPresets =
    [
        ("不透明 (100%)", 1.0),
        ("轻微透明 (85%)", 0.85),
        ("半透明 (70%)", 0.70),
        ("高透明 (55%)", 0.55)
    ];

    private static readonly (string Label, double Value)[] FontScalePresets =
    [
        ("小 (85%)", 0.85),
        ("标准 (100%)", 1.0),
        ("大 (115%)", 1.15),
        ("特大 (130%)", 1.3)
    ];

    private readonly AppSettings _original;
    private readonly LocationService _locationService = new();

    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _original = Clone(settings);
        InitOpacityCombo();
        InitFontScaleCombo();
        LoadFromSettings(_original);
    }

    private static AppSettings Clone(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    private void InitOpacityCombo()
    {
        foreach (var (label, _) in OpacityPresets)
        {
            CmbOpacity.Items.Add(label);
        }
    }

    private void InitFontScaleCombo()
    {
        foreach (var (label, _) in FontScalePresets)
        {
            CmbFontScale.Items.Add(label);
        }
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

        var opacityIndex = 0;
        for (var i = 0; i < OpacityPresets.Length; i++)
        {
            if (Math.Abs(s.Opacity - OpacityPresets[i].Value) < 0.001)
            {
                opacityIndex = i;
                break;
            }
        }
        CmbOpacity.SelectedIndex = opacityIndex;

        var fontIndex = 1;
        for (var i = 0; i < FontScalePresets.Length; i++)
        {
            if (Math.Abs(s.FontScale - FontScalePresets[i].Value) < 0.001)
            {
                fontIndex = i;
                break;
            }
        }
        CmbFontScale.SelectedIndex = fontIndex;

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
        ChkDailyQuote.IsChecked = s.ShowDailyQuote;
        ChkScratch.IsChecked = s.ShowScratch;
        ChkTodoReminder.IsChecked = s.ShowTodoReminder;
        LoadModuleOrder(s.ModuleOrder);
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
        var opacityIndex = Math.Max(0, CmbOpacity.SelectedIndex);
        s.Opacity = OpacityPresets[opacityIndex].Value;
        var fontIndex = Math.Max(0, CmbFontScale.SelectedIndex);
        s.FontScale = FontScalePresets[fontIndex].Value;

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
