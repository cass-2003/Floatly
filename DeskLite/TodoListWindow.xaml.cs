using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeskLite.Models;
using DeskLite.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfPath = System.Windows.Shapes.Path;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace DeskLite;

public partial class TodoListWindow : Window
{
    private enum TodoFilter { All, Today, Tomorrow, Week, Done }
    private enum TodoPriority { High, Medium, Low }

    private readonly TodoStore _store;
    private readonly AppSettings _settings;
    private readonly Action _onChanged;
    private readonly AppThemePalette _palette;
    private TodoFilter _filter = TodoFilter.All;
    private string _search = string.Empty;

    public TodoListWindow(TodoStore store, AppSettings settings, Action onChanged)
    {
        InitializeComponent();
        _store = store;
        _settings = settings;
        _onChanged = onChanged;
        _palette = AppThemePalette.For(AppThemePalette.Parse(settings.Theme));
        FontFamilyHelper.Apply(this, settings.FontFamily);
        ApplyTheme();
        RefreshList();
    }

    public void RefreshFromOutside() => RefreshList();

    private void ApplyTheme()
    {
        TodoThemeHelper.ApplyResources(Resources, _palette);

        RootBorder.Background = IsLightTheme()
            ? Brush(0xF2, 0xFF, 0xFF, 0xFF)
            : Brush(0xE8, 0x12, 0x1E, 0x2E);
        RootBorder.BorderBrush = IsLightTheme()
            ? Brush(0x36, 0x15, 0x23, 0x42)
            : Brush(0x24, 0xE4, 0xF0, 0xFF);

        HeaderText.Foreground = Brush(_palette.TextPrimary);
        HeaderSubtitleText.Foreground = Brush(_palette.TextMuted);
        SearchShell.Background = IsLightTheme()
            ? Brush(0x8A, 0xF8, 0xFA, 0xFC)
            : Brush(0x26, 0xE8, 0xF4, 0xFF);
        SearchShell.BorderBrush = Brush(_palette.TodoCardBorder);
        SearchBox.Foreground = Brush(_palette.InputText);
        SearchBox.CaretBrush = Brush(_palette.Accent);
        SearchBox.FontSize = Scaled(13);

        TaskPanel.Background = IsLightTheme()
            ? Brush(0x84, 0xFF, 0xFF, 0xFF)
            : Brush(0x18, 0xE8, 0xF4, 0xFF);
        TaskPanel.BorderBrush = Brush(_palette.TodoCardBorder);
        FocusCard.Background = CardBrush();
        FocusCard.BorderBrush = Brush(_palette.TodoCardBorder);
        FocusTaskCard.Background = IsLightTheme()
            ? Brush(0x72, 0xF8, 0xFA, 0xFC)
            : Brush(0x22, 0xE8, 0xF4, 0xFF);
        FocusTaskCard.BorderBrush = Brush(_palette.TodoCardBorder);
        StatsCard.Background = CardBrush();
        StatsCard.BorderBrush = Brush(_palette.TodoCardBorder);
        FooterBar.Background = IsLightTheme()
            ? Brush(0x72, 0xFF, 0xFF, 0xFF)
            : Brush(0x18, 0xE8, 0xF4, 0xFF);
        FooterBar.BorderBrush = Brush(_palette.TodoCardBorder);

        EmptyHintText.Foreground = Brush(_palette.TextPrimary);
        EmptySubText.Foreground = Brush(_palette.TextMuted);
        FooterHintText.Foreground = Brush(_palette.TextSubtle);
        FocusTitleText.Foreground = Brush(_palette.TextPrimary);
        FocusDetailText.Foreground = Brush(_palette.TextMuted);
        CompletionText.Foreground = Brush(_palette.TextPrimary);
        TotalCountText.Foreground = Brush(_palette.TextPrimary);
        ActiveCountText.Foreground = Brush(_palette.TextPrimary);
        DoneCountText.Foreground = Brush(_palette.TextPrimary);

        StylePrimaryButton(AddTaskBtn);
        StylePrimaryButton(EmptyAddBtn);
        StylePrimaryButton(FooterAddBtn);
        StyleIconButton(MinimizeBtn);
        StyleIconButton(CloseBtn);
        RefreshFilterButtons();
    }

    private void RefreshList()
    {
        RefreshFilterButtons();

        var all = _store.GetAllTodos();
        var active = all.Where(t => !t.Done).ToList();
        var completed = all.Where(t => t.Done).ToList();
        var filtered = _filter switch
        {
            TodoFilter.Today => active.Where(IsToday).ToList(),
            TodoFilter.Tomorrow => active.Where(IsTomorrow).ToList(),
            TodoFilter.Week => active.Where(IsThisWeek).ToList(),
            TodoFilter.Done => completed,
            _ => active
        };

        if (!string.IsNullOrWhiteSpace(_search))
        {
            filtered = filtered
                .Where(t => t.Title.Contains(_search, StringComparison.OrdinalIgnoreCase)
                            || (t.Time?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (t.DueDate?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (t.Date?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        TodoSectionsPanel.Children.Clear();
        if (filtered.Count == 0)
        {
            EmptyStatePanel.Visibility = Visibility.Visible;
            EmptyHintText.Text = _filter switch
            {
                TodoFilter.Done => "暂无已完成任务",
                TodoFilter.Tomorrow => "明天还没有任务",
                TodoFilter.Week => "本周还没有任务",
                _ => string.IsNullOrWhiteSpace(_search) ? "今天还没有任务" : "没有匹配的任务"
            };
        }
        else
        {
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            foreach (var group in BuildGroups(filtered))
            {
                AddSection(group.Title, group.Color, group.Items);
            }
        }

        RefreshFocus(active);
        RefreshStats(all);
    }

    private void AddSection(string title, WpfColor color, IReadOnlyList<TodoItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        var header = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, TodoSectionsPanel.Children.Count == 0 ? 0 : 18, 0, 8)
        };
        header.Children.Add(new WpfEllipse
        {
            Width = 9,
            Height = 9,
            Fill = Brush(color),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        header.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Brush(_palette.TextSecondary),
            FontSize = Scaled(12),
            FontWeight = FontWeights.SemiBold
        });
        TodoSectionsPanel.Children.Add(header);

        foreach (var item in items)
        {
            TodoSectionsPanel.Children.Add(BuildRow(item, color));
        }
    }

    private UIElement BuildRow(TodoItem item, WpfColor accent)
    {
        var display = TodoDisplayItem.From(item);
        var row = new Border
        {
            Background = CardBrush(),
            BorderBrush = Brush(_palette.TodoCardBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 8),
            Tag = item.Id
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new Border
        {
            Width = 4,
            Background = Brush(accent),
            CornerRadius = new CornerRadius(12, 0, 0, 12)
        });

        var check = new CircleCheckBox
        {
            IsChecked = item.Done,
            TagId = item.Id,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 12, 0)
        };
        check.ApplyTheme(_palette);
        check.Click += TodoCheck_Click;
        Grid.SetColumn(check, 1);
        grid.Children.Add(check);

        var content = new StackPanel
        {
            Margin = new Thickness(0, 12, 8, 12),
            VerticalAlignment = VerticalAlignment.Center
        };
        var titleBlock = new TextBlock
        {
            Text = item.Title,
            FontSize = Scaled(14),
            FontWeight = FontWeights.SemiBold,
            Foreground = item.Done ? Brush(_palette.TextEmpty) : Brush(_palette.TodoText),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Tag = item.Id,
            TextDecorations = item.Done ? TextDecorations.Strikethrough : null
        };
        titleBlock.MouseLeftButtonDown += TodoTitle_DoubleClick;
        content.Children.Add(titleBlock);

        var meta = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 7, 0, 0) };
        meta.Children.Add(MakeChip(IsPinned(item) ? "今日聚焦" : "开发任务", _palette.TodoTimeBadge, _palette.Accent));
        if (display.HasTime)
        {
            meta.Children.Add(MakeMetaText("◷ " + item.Time));
        }
        else
        {
            meta.Children.Add(MakeMetaText(FormatDateLabel(item.Date)));
        }

        if (display.HasDueDate)
        {
            meta.Children.Add(MakeMetaText(display.DueDateLabel));
        }

        content.Children.Add(meta);
        Grid.SetColumn(content, 2);
        grid.Children.Add(content);

        var actions = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        actions.Children.Add(MakeActionButton(item.Pinned ? "★" : "☆", item.Pinned ? _palette.TodoPinActive : _palette.TodoPinInactive, item.Id, TodoPin_Click));
        actions.Children.Add(MakeActionButton("✎", _palette.TextMuted, item.Id, TodoEdit_Click));
        actions.Children.Add(MakeActionButton("×", _palette.DeleteButton, item.Id, TodoDelete_Click));
        Grid.SetColumn(actions, 3);
        grid.Children.Add(actions);

        row.Child = grid;
        return row;
    }

    private Border MakeChip(string text, WpfColor bg, WpfColor fg) =>
        new()
        {
            Background = Brush(bg),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 8, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = Scaled(10),
                Foreground = Brush(fg)
            }
        };

    private TextBlock MakeMetaText(string text) =>
        new()
        {
            Text = text,
            FontSize = Scaled(11),
            Foreground = Brush(_palette.TextMuted),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

    private WpfButton MakeActionButton(string content, WpfColor color, string id, RoutedEventHandler handler)
    {
        var btn = new WpfButton
        {
            Content = content,
            Width = 28,
            Height = 28,
            Tag = id,
            Foreground = Brush(color)
        };
        TodoThemeHelper.StyleActionButton(btn, Scaled(14));
        btn.Click += handler;
        return btn;
    }

    private void RefreshFocus(IReadOnlyList<TodoItem> active)
    {
        var focus = active.FirstOrDefault(t => t.Pinned) ?? active.FirstOrDefault();
        if (focus is null)
        {
            FocusTitleText.Text = "暂无聚焦任务";
            FocusDetailText.Text = "星标任务会优先出现在这里";
            FocusMetaText.Text = "今日聚焦";
            return;
        }

        FocusTitleText.Text = focus.Title;
        FocusDetailText.Text = BuildMetaSummary(focus);
        FocusMetaText.Text = focus.Pinned ? "已星标" : "当前任务";
    }

    private void RefreshStats(IReadOnlyList<TodoItem> all)
    {
        var total = all.Count;
        var done = all.Count(t => t.Done);
        var active = Math.Max(0, total - done);
        var percent = total == 0 ? 0 : (int)Math.Round(done * 100.0 / total);

        TotalCountText.Text = total.ToString();
        ActiveCountText.Text = active.ToString();
        DoneCountText.Text = done.ToString();
        CompletionText.Text = $"{percent}%";
        UpdateArc(StatsArc, percent, 58, 7);
    }

    private IEnumerable<(string Title, WpfColor Color, IReadOnlyList<TodoItem> Items)> BuildGroups(IReadOnlyList<TodoItem> items)
    {
        if (_filter == TodoFilter.Done)
        {
            yield return ("已完成", WpfColor.FromRgb(0x22, 0xC5, 0x5E), items);
            yield break;
        }

        var high = items.Where(t => GetPriority(t) == TodoPriority.High).ToList();
        var medium = items.Where(t => GetPriority(t) == TodoPriority.Medium).ToList();
        var low = items.Where(t => GetPriority(t) == TodoPriority.Low).ToList();

        yield return ("高优先级", WpfColor.FromRgb(0xFB, 0x71, 0x52), high);
        yield return ("中优先级", WpfColor.FromRgb(0xF5, 0xC5, 0x42), medium);
        yield return ("低优先级", WpfColor.FromRgb(0x34, 0xD3, 0x99), low);
    }

    private TodoPriority GetPriority(TodoItem item)
    {
        if (item.Pinned || IsDueTodayOrOverdue(item))
        {
            return TodoPriority.High;
        }

        if (IsToday(item) || IsTomorrow(item))
        {
            return TodoPriority.Medium;
        }

        return TodoPriority.Low;
    }

    private bool IsDueTodayOrOverdue(TodoItem item) =>
        DateTime.TryParse(item.DueDate, out var due) && due.Date <= DateTime.Today;

    private static bool IsPinned(TodoItem item) => item.Pinned;

    private bool IsToday(TodoItem item) => ResolveTaskDate(item).Date == DateTime.Today;

    private bool IsTomorrow(TodoItem item) => ResolveTaskDate(item).Date == DateTime.Today.AddDays(1);

    private bool IsThisWeek(TodoItem item)
    {
        var date = ResolveTaskDate(item).Date;
        var today = DateTime.Today;
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var end = start.AddDays(7);
        return date >= start && date < end;
    }

    private static DateTime ResolveTaskDate(TodoItem item)
    {
        if (DateTime.TryParse(item.DueDate, out var due))
        {
            return due.Date;
        }

        return DateTime.TryParse(item.Date, out var date)
            ? date.Date
            : DateTime.Today;
    }

    private string BuildMetaSummary(TodoItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.Time))
        {
            parts.Add(item.Time);
        }

        if (!string.IsNullOrWhiteSpace(item.DueDate))
        {
            parts.Add(TodoDisplayItem.From(item).DueDateLabel);
        }

        if (parts.Count == 0)
        {
            parts.Add(FormatDateLabel(item.Date));
        }

        return string.Join(" · ", parts);
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

    private void RefreshFilterButtons()
    {
        StyleFilterButton(FilterAllBtn, _filter == TodoFilter.All);
        StyleFilterButton(FilterTodayBtn, _filter == TodoFilter.Today);
        StyleFilterButton(FilterTomorrowBtn, _filter == TodoFilter.Tomorrow);
        StyleFilterButton(FilterWeekBtn, _filter == TodoFilter.Week);
        StyleFilterButton(FilterDoneBtn, _filter == TodoFilter.Done);
    }

    private void StyleFilterButton(WpfButton btn, bool selected)
    {
        btn.FontSize = Scaled(12);
        btn.Background = selected
            ? Brush(0x28, _palette.Accent.R, _palette.Accent.G, _palette.Accent.B)
            : System.Windows.Media.Brushes.Transparent;
        btn.Foreground = selected ? Brush(_palette.Accent) : Brush(_palette.TextSecondary);
        btn.BorderBrush = selected ? Brush(_palette.Accent) : Brush(_palette.TodoCardBorder);
    }

    private void StylePrimaryButton(WpfButton btn)
    {
        btn.Background = Brush(_palette.TodoAccentButton);
        btn.Foreground = System.Windows.Media.Brushes.White;
        btn.FontWeight = FontWeights.SemiBold;
    }

    private void StyleIconButton(WpfButton btn)
    {
        btn.Foreground = Brush(_palette.TextSecondary);
        btn.Background = System.Windows.Media.Brushes.Transparent;
    }

    private void AddTask_Click(object sender, RoutedEventArgs e)
    {
        var result = TodoEditPrompt.Show("新建任务", "添加任务内容与可选提醒：", palette: _palette);
        if (result is null || string.IsNullOrWhiteSpace(result.Title))
        {
            return;
        }

        _store.Add(result.Title, result.ReminderTime, result.DueDate);
        NotifyChanged();
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

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.All;
        RefreshList();
    }

    private void FilterToday_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Today;
        RefreshList();
    }

    private void FilterTomorrow_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Tomorrow;
        RefreshList();
    }

    private void FilterWeek_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Week;
        RefreshList();
    }

    private void FilterDone_Click(object sender, RoutedEventArgs e)
    {
        _filter = TodoFilter.Done;
        RefreshList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _search = SearchBox.Text.Trim();
        RefreshList();
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private double Scaled(double baseSize) =>
        FontScaleHelper.ScaledSize(baseSize, _settings.FontScale);

    private bool IsLightTheme() => AppThemePalette.Parse(_settings.Theme) == ThemeMode.Light;

    private SolidColorBrush CardBrush() => IsLightTheme()
        ? Brush(0x9C, 0xFF, 0xFF, 0xFF)
        : Brush(0x20, 0xE8, 0xF4, 0xFF);

    private static SolidColorBrush Brush(WpfColor color) => new(color);

    private static SolidColorBrush Brush(byte a, byte r, byte g, byte b) => new(WpfColor.FromArgb(a, r, g, b));

    private static void UpdateArc(WpfPath path, double percent, double size, double thickness)
    {
        percent = Math.Clamp(percent, 0, 100);
        if (percent <= 0)
        {
            path.Data = Geometry.Empty;
            return;
        }

        var radius = (size - thickness) / 2;
        var center = new WpfPoint(size / 2, size / 2);
        var startAngle = -90d;
        var endAngle = startAngle + 359.9 * percent / 100d;
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, endAngle);
        var largeArc = endAngle - startAngle > 180;

        path.Data = new PathGeometry(new[]
        {
            new PathFigure(
                start,
                new PathSegment[]
                {
                    new ArcSegment(end, new WpfSize(radius, radius), 0, largeArc, SweepDirection.Clockwise, true)
                },
                false)
        });
    }

    private static WpfPoint PointOnCircle(WpfPoint center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180d;
        return new WpfPoint(
            center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }
}
