using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeskLite.Models;
using DeskLite.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfColor = System.Windows.Media.Color;

namespace DeskLite;

public partial class TodoListWindow : Window
{
    private enum TodoFilter { Active, Done, All }

    private readonly TodoStore _store;
    private readonly AppSettings _settings;
    private readonly Action _onChanged;
    private readonly AppThemePalette _palette;
    private TodoFilter _filter = TodoFilter.Active;
    private string _search = string.Empty;

    public TodoListWindow(TodoStore store, AppSettings settings, Action onChanged)
    {
        InitializeComponent();
        _store = store;
        _settings = settings;
        _onChanged = onChanged;
        _palette = AppThemePalette.For(AppThemePalette.Parse(settings.Theme));
        ApplyTheme();
        RefreshList();
    }

    public void RefreshFromOutside() => RefreshList();

    private void ApplyTheme()
    {
        TodoThemeHelper.ApplyResources(Resources, _palette);
        Background = Brush(_palette.PanelBackground);
        Foreground = Brush(_palette.TextPrimary);
        RootBorder.Background = Brush(_palette.PanelBackground);
        RootBorder.BorderBrush = Brush(_palette.PanelBorder);
        HeaderText.Foreground = Brush(_palette.TextPrimary);
        EmptyHintText.Foreground = Brush(_palette.TextEmpty);
        FooterHintText.Foreground = Brush(_palette.TextSubtle);
        TodoThemeHelper.StyleInput(SearchBox, _palette, Scaled(12));
        StyleFilterButton(FilterActiveBtn, _filter == TodoFilter.Active);
        StyleFilterButton(FilterDoneBtn, _filter == TodoFilter.Done);
        StyleFilterButton(FilterAllBtn, _filter == TodoFilter.All);
    }

    private void StyleFilterButton(WpfButton btn, bool selected)
    {
        btn.FontSize = Scaled(12);
        btn.Background = selected ? Brush(_palette.TodoAccentButton) : Brush(_palette.TodoCardBackground);
        btn.Foreground = selected ? System.Windows.Media.Brushes.White : Brush(_palette.TextSecondary);
        btn.BorderBrush = selected ? Brush(_palette.TodoAccentButton) : Brush(_palette.TodoCardBorder);
        btn.BorderThickness = new Thickness(1);
    }

    private void RefreshList()
    {
        StyleFilterButton(FilterActiveBtn, _filter == TodoFilter.Active);
        StyleFilterButton(FilterDoneBtn, _filter == TodoFilter.Done);
        StyleFilterButton(FilterAllBtn, _filter == TodoFilter.All);

        var items = _filter switch
        {
            TodoFilter.Active => _store.GetActiveTodos(),
            TodoFilter.Done => _store.GetCompletedTodos(),
            _ => _store.GetAllTodos()
        };

        if (!string.IsNullOrWhiteSpace(_search))
        {
            items = items
                .Where(t => t.Title.Contains(_search, StringComparison.OrdinalIgnoreCase)
                            || (t.Time?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (t.DueDate?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        TodoItemsPanel.Children.Clear();
        foreach (var item in items)
        {
            TodoItemsPanel.Children.Add(BuildRow(item));
        }

        EmptyHintText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyHintText.Text = _filter switch
        {
            TodoFilter.Done => "暂无已完成待办",
            TodoFilter.All => "暂无待办记录",
            _ => string.IsNullOrWhiteSpace(_search) ? "暂无进行中的待办" : "没有匹配的待办"
        };
    }

    private UIElement BuildRow(TodoItem item)
    {
        var display = TodoDisplayItem.From(item);
        var row = new Border
        {
            Background = Brush(_palette.TodoCardBackground),
            BorderBrush = Brush(_palette.TodoCardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 6, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Tag = item.Id
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var check = new CircleCheckBox
        {
            IsChecked = item.Done,
            TagId = item.Id,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        check.ApplyTheme(_palette);
        check.Click += TodoCheck_Click;
        Grid.SetColumn(check, 0);
        grid.Children.Add(check);

        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (!string.IsNullOrWhiteSpace(item.Date) && item.Date != DateTime.Today.ToString("yyyy-MM-dd"))
        {
            content.Children.Add(new TextBlock
            {
                Text = FormatDateLabel(item.Date),
                FontSize = Scaled(10),
                Foreground = Brush(_palette.TextSubtle),
                Margin = new Thickness(0, 0, 0, 2)
            });
        }

        var titleRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        if (display.HasTime)
        {
            titleRow.Children.Add(new Border
            {
                Background = Brush(_palette.TodoTimeBadge),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = item.Time,
                    FontSize = Scaled(10),
                    Foreground = Brush(_palette.Accent),
                    FontWeight = FontWeights.SemiBold,
                    ToolTip = "提醒时间"
                }
            });
        }

        if (display.HasDueDate)
        {
            titleRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(0x22, 0xF5, 0x9E, 0x0B)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = display.DueDateLabel,
                    FontSize = Scaled(10),
                    Foreground = Brush(_palette.TodoPinActive),
                    FontWeight = FontWeights.SemiBold
                }
            });
        }

        var titleBlock = new TextBlock
        {
            Text = item.Title,
            FontSize = Scaled(13),
            Foreground = item.Done ? Brush(_palette.TextEmpty) : Brush(_palette.TodoText),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = item.Id,
            TextDecorations = item.Done ? TextDecorations.Strikethrough : null
        };
        titleBlock.MouseLeftButtonDown += TodoTitle_DoubleClick;
        titleRow.Children.Add(titleBlock);
        content.Children.Add(titleRow);
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var actions = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actions.Children.Add(MakeActionButton(item.Pinned ? "★" : "☆", item.Pinned ? _palette.TodoPinActive : _palette.TodoPinInactive, item.Id, TodoPin_Click));
        actions.Children.Add(MakeActionButton("✎", _palette.TextMuted, item.Id, TodoEdit_Click));
        actions.Children.Add(MakeActionButton("×", _palette.DeleteButton, item.Id, TodoDelete_Click));
        Grid.SetColumn(actions, 2);
        grid.Children.Add(actions);

        row.Child = grid;
        return row;
    }

    private WpfButton MakeActionButton(string content, WpfColor color, string id, RoutedEventHandler handler)
    {
        var btn = new WpfButton
        {
            Content = content,
            Width = 26,
            Height = 26,
            Tag = id,
            Foreground = Brush(color)
        };
        TodoThemeHelper.StyleActionButton(btn, Scaled(13));
        btn.Click += handler;
        return btn;
    }

    private static string FormatDateLabel(string date)
    {
        if (!DateTime.TryParse(date, out var dt))
        {
            return date;
        }

        if (dt.Date == DateTime.Today)
        {
            return "今天";
        }

        if (dt.Date == DateTime.Today.AddDays(1))
        {
            return "明天";
        }

        return dt.ToString("M月d日");
    }

    private void TodoCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CircleCheckBox { TagId: string id })
        {
            _store.ToggleDone(id);
            NotifyChanged();
        }
    }

    private void TodoPin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: string id })
        {
            var item = _store.GetById(id);
            if (item is not null)
            {
                _store.SetPinned(id, !item.Pinned);
                NotifyChanged();
            }
        }
    }

    private void TodoEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: string id })
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
        if (sender is WpfButton { Tag: string id })
        {
            _store.Remove(id);
            NotifyChanged();
        }
    }

    private void EditTodo(string id)
    {
        var item = _store.GetById(id);
        if (item is null)
        {
            return;
        }

        var result = TodoEditPrompt.Show("编辑待办", "修改待办内容与截止时间：", item, _palette);
        if (result is null || string.IsNullOrWhiteSpace(result.Title))
        {
            return;
        }

        _store.Update(id, result.Title, result.ReminderTime, result.DueDate);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        RefreshList();
        _onChanged();
    }

    private void FilterActive_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Active;
        RefreshList();
    }

    private void FilterDone_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Done;
        RefreshList();
    }

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.All;
        RefreshList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _search = SearchBox.Text.Trim();
        RefreshList();
    }

    private double Scaled(double baseSize) =>
        FontScaleHelper.ScaledSize(baseSize, _settings.FontScale);

    private static SolidColorBrush Brush(WpfColor color) => new(color);
}
