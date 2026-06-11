using System.Windows;
using DeskLite.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace DeskLite;

public static class DateNoteDialog
{
    public static string? Show(DateTime date, string? existingNote, AppThemePalette palette, string? fontFamily = null)
    {
        var noteBox = new WpfTextBox
        {
            Text = existingNote ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 72,
            MaxHeight = 120,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(6, 4, 6, 4),
            MinWidth = 260
        };
        TodoThemeHelper.StyleInput(noteBox, palette, 12);

        var ok = false;
        string? result = null;
        Window? dialog = null;

        dialog = new Window
        {
            Title = $"日期备注 · {date:yyyy年M月d日}",
            Width = 320,
            Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Background = new System.Windows.Media.SolidColorBrush(palette.PanelBackground),
            Foreground = new System.Windows.Media.SolidColorBrush(palette.TextPrimary),
            FontFamily = FontFamilyHelper.Resolve(fontFamily)
        };

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "为该日期添加备注（留空并确定可清除）",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new System.Windows.Media.SolidColorBrush(palette.TextSecondary)
        });
        panel.Children.Add(noteBox);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var clearBtn = new WpfButton
        {
            Content = "清除",
            Width = 56,
            Margin = new Thickness(0, 0, 8, 0)
        };
        clearBtn.Background = new System.Windows.Media.SolidColorBrush(palette.TodoCardBackground);
        clearBtn.Foreground = new System.Windows.Media.SolidColorBrush(palette.TextSecondary);
        clearBtn.BorderBrush = new System.Windows.Media.SolidColorBrush(palette.TodoCardBorder);
        clearBtn.BorderThickness = new Thickness(1);
        clearBtn.Click += (_, _) =>
        {
            ok = true;
            result = string.Empty;
            dialog!.Close();
        };

        var okBtn = new WpfButton { Content = "确定", Width = 64, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        TodoThemeHelper.StyleAccentButton(okBtn, palette, 12);
        okBtn.Click += (_, _) =>
        {
            ok = true;
            result = noteBox.Text.Trim();
            dialog!.Close();
        };

        var cancelBtn = new WpfButton { Content = "取消", Width = 64, IsCancel = true };
        cancelBtn.Background = new System.Windows.Media.SolidColorBrush(palette.TodoCardBackground);
        cancelBtn.Foreground = new System.Windows.Media.SolidColorBrush(palette.TextSecondary);
        cancelBtn.BorderBrush = new System.Windows.Media.SolidColorBrush(palette.TodoCardBorder);
        cancelBtn.BorderThickness = new Thickness(1);
        cancelBtn.Click += (_, _) => dialog!.Close();

        buttons.Children.Add(clearBtn);
        buttons.Children.Add(okBtn);
        buttons.Children.Add(cancelBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
        return ok ? result : null;
    }
}
