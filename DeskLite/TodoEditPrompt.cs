using System.Globalization;
using System.Windows;
using DeskLite.Models;
using DeskLite.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DeskLite;

public static class TodoEditPrompt
{
    public sealed record Result(string Title, string? ReminderTime, string? DueDate);

    public static Result? Show(string title, string message, TodoItem? existing = null, AppThemePalette? palette = null)
    {
        palette ??= AppThemePalette.For(ThemeMode.Dark);

        var titleBox = new WpfTextBox
        {
            Text = existing?.Title ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(6, 4, 6, 4),
            MinWidth = 260
        };
        TodoThemeHelper.StyleInput(titleBox, palette, 13);

        var dueBox = new WpfTextBox
        {
            Text = existing?.DueDate ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(6, 4, 6, 4),
            MinWidth = 260
        };
        TodoThemeHelper.StyleInput(dueBox, palette, 13);

        var timeBox = new WpfTextBox
        {
            Text = existing?.Time ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 0),
            Padding = new Thickness(6, 4, 6, 4),
            MinWidth = 260
        };
        TodoThemeHelper.StyleInput(timeBox, palette, 13);

        var ok = false;
        Result? result = null;
        Window? dialog = null;

        dialog = new Window
        {
            Title = title,
            Width = 320,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Background = new System.Windows.Media.SolidColorBrush(palette.PanelBackground),
            Foreground = new System.Windows.Media.SolidColorBrush(palette.TextPrimary)
        };

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(MakeLabel(message, palette));
        panel.Children.Add(MakeLabel("标题", palette, 11));
        panel.Children.Add(titleBox);
        panel.Children.Add(MakeLabel("截止日期 (yyyy-MM-dd，可选)", palette, 11));
        panel.Children.Add(dueBox);
        panel.Children.Add(MakeLabel("提醒时间 (HH:mm，可选)", palette, 11));
        panel.Children.Add(timeBox);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var okBtn = new WpfButton { Content = "确定", Width = 64, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        TodoThemeHelper.StyleAccentButton(okBtn, palette, 12);
        okBtn.Click += (_, _) =>
        {
            var parsedTitle = titleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(parsedTitle))
            {
                return;
            }

            var due = NormalizeDate(dueBox.Text);
            var time = NormalizeTime(timeBox.Text);
            ok = true;
            result = new Result(parsedTitle, time, due);
            dialog!.Close();
        };
        var cancelBtn = new WpfButton { Content = "取消", Width = 64, IsCancel = true };
        cancelBtn.Background = new System.Windows.Media.SolidColorBrush(palette.TodoCardBackground);
        cancelBtn.Foreground = new System.Windows.Media.SolidColorBrush(palette.TextSecondary);
        cancelBtn.BorderBrush = new System.Windows.Media.SolidColorBrush(palette.TodoCardBorder);
        cancelBtn.BorderThickness = new Thickness(1);
        cancelBtn.Click += (_, _) => dialog!.Close();
        buttons.Children.Add(okBtn);
        buttons.Children.Add(cancelBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
        return ok ? result : null;
    }

    private static System.Windows.Controls.TextBlock MakeLabel(string text, AppThemePalette palette, double size = 12) =>
        new()
        {
            Text = text,
            FontSize = size,
            Foreground = new System.Windows.Media.SolidColorBrush(
                size <= 11 ? palette.TextSubtle : palette.TextPrimary),
            TextWrapping = TextWrapping.Wrap,
            Margin = size <= 11 ? new Thickness(0, 8, 0, 0) : new Thickness(0)
        };

    private static string? NormalizeDate(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return text;
        }

        if (DateTime.TryParse(text, out var dt))
        {
            return dt.ToString("yyyy-MM-dd");
        }

        return null;
    }

    private static string? NormalizeTime(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (TimeSpan.TryParse(text, out _))
        {
            return text.Length >= 5 ? text[..5] : text;
        }

        return null;
    }
}
