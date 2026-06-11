using System.Windows;
using System.Windows.Controls;
using DeskLite.Services;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace DeskLite;

public partial class CircleCheckBox : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(CircleCheckBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

    public static readonly DependencyProperty TagIdProperty =
        DependencyProperty.Register(nameof(TagId), typeof(string), typeof(CircleCheckBox));

    public event RoutedEventHandler? Click;

    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public string? TagId
    {
        get => (string?)GetValue(TagIdProperty);
        set => SetValue(TagIdProperty, value);
    }

    public CircleCheckBox()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyTheme();
        UpdateVisual();
    }

    public void ApplyTheme(AppThemePalette? palette = null)
    {
        palette ??= AppThemePalette.For(ThemeMode.Dark);
        Resources["CircleCheckBorderBrush"] = new WpfSolidColorBrush(palette.TextMuted);
        Resources["CircleCheckFillBrush"] = new WpfSolidColorBrush(palette.Accent);
        Resources["CircleCheckMarkBrush"] = new WpfSolidColorBrush(WpfColors.White);
        UpdateVisual();
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircleCheckBox box)
        {
            box.UpdateVisual();
        }
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        IsChecked = Toggle.IsChecked;
        Tag = TagId;
        Click?.Invoke(this, e);
    }

    private void UpdateVisual()
    {
        if (Circle is null || CheckMark is null || Toggle is null)
        {
            return;
        }

        var checkedState = IsChecked == true;
        Toggle.IsChecked = checkedState;
        CheckMark.Visibility = checkedState ? Visibility.Visible : Visibility.Collapsed;

        if (checkedState && Resources["CircleCheckFillBrush"] is WpfSolidColorBrush fill)
        {
            Circle.Fill = fill;
            Circle.Stroke = fill;
        }
        else
        {
            Circle.Fill = WpfBrushes.Transparent;
            Circle.Stroke = Resources["CircleCheckBorderBrush"] as WpfBrush ?? WpfBrushes.Gray;
        }
    }
}
