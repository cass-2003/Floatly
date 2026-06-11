using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeskLite.Models;
using DeskLite.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfMessageBox = System.Windows.MessageBox;
using WpfClipboard = System.Windows.Clipboard;

namespace DeskLite;

public partial class ScratchPadWindow : Window
{
    private readonly TodoStore _store;
    private readonly AppSettings _settings;
    private readonly Action _onChanged;
    private readonly AppThemePalette _palette;
    private readonly ThemeMode _themeMode;
    private readonly DispatcherTimer _saveTimer;
    private string _search = string.Empty;
    private string? _selectedId;
    private bool _suppressEditorEvents;

    public ScratchPadWindow(TodoStore store, AppSettings settings, Action onChanged, string? selectNoteId = null)
    {
        InitializeComponent();
        _store = store;
        _settings = settings;
        _onChanged = onChanged;
        _themeMode = AppThemePalette.Parse(settings.Theme);
        _palette = AppThemePalette.For(_themeMode);
        _selectedId = selectNoteId;

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            FlushSave();
        };

        FontFamilyHelper.Apply(this, settings.FontFamily);
        BuildColorButtons();
        ApplyTheme();
        RefreshList();
        SelectNote(_selectedId ?? _store.GetScratchPreviewNote()?.Id);
    }

    public void RefreshFromOutside()
    {
        RefreshList();
        if (_selectedId is not null && _store.GetScratchNote(_selectedId) is null)
        {
            SelectNote(_store.GetScratchPreviewNote()?.Id);
        }
    }

    private void BuildColorButtons()
    {
        ColorPanel.Children.Clear();
        foreach (var color in ScratchNoteColors.All)
        {
            var dot = ScratchColorHelper.GetAccentDot(color);
            var btn = new WpfButton
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(0, 0, 6, 0),
                Tag = color,
                ToolTip = color,
                Cursor = WpfCursors.Hand,
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(dot)
            };
            btn.Click += ColorBtn_Click;
            ColorPanel.Children.Add(btn);
        }
    }

    private void ApplyTheme()
    {
        Background = Brush(_palette.PanelBackground);
        Foreground = Brush(_palette.TextPrimary);
        RootBorder.Background = Brush(_palette.PanelBackground);
        RootBorder.BorderBrush = Brush(_palette.PanelBorder);
        SidebarTitle.Foreground = Brush(_palette.TextPrimary);
        ListEmptyHint.Foreground = Brush(_palette.TextEmpty);
        EditorEmptyHint.Foreground = Brush(_palette.TextEmpty);
        SaveStatusText.Foreground = Brush(_palette.TextSubtle);
        TodoThemeHelper.StyleInput(SearchBox, _palette, Scaled(12));
        TodoThemeHelper.StyleInput(TitleBox, _palette, Scaled(13));
        TodoThemeHelper.StyleInput(ContentBox, _palette, Scaled(13));
        TodoThemeHelper.StyleAccentButton(NewNoteBtn, _palette, Scaled(12));
        TodoThemeHelper.StyleAccentButton(EditorNewBtn, _palette, Scaled(12));
        StyleSecondaryButton(PinBtn);
        StyleSecondaryButton(DuplicateBtn);
        StyleSecondaryButton(CopyBtn);
        StyleSecondaryButton(DeleteBtn);
        DeleteBtn.Foreground = Brush(_palette.DeleteButton);
    }

    private void StyleSecondaryButton(WpfButton btn)
    {
        btn.FontSize = Scaled(12);
        btn.Background = Brush(_palette.TodoCardBackground);
        btn.Foreground = Brush(_palette.TextSecondary);
        btn.BorderBrush = Brush(_palette.TodoCardBorder);
        btn.BorderThickness = new Thickness(1);
    }

    private void RefreshList()
    {
        var notes = _store.GetScratchNotes();
        if (!string.IsNullOrWhiteSpace(_search))
        {
            notes = notes
                .Where(n => n.Title.Contains(_search, StringComparison.OrdinalIgnoreCase)
                            || n.Content.Contains(_search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        NoteListPanel.Children.Clear();
        foreach (var note in notes)
        {
            NoteListPanel.Children.Add(BuildListItem(note));
        }

        var hasNotes = _store.GetScratchNotes().Count > 0;
        ListEmptyHint.Visibility = hasNotes && notes.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        ListEmptyHint.Text = "没有匹配的便签";
        if (!hasNotes)
        {
            ListEmptyHint.Visibility = Visibility.Visible;
            ListEmptyHint.Text = "点击 + 新建便签";
        }
    }

    private UIElement BuildListItem(ScratchNote note)
    {
        var selected = note.Id == _selectedId;
        var row = new Border
        {
            Background = new SolidColorBrush(ScratchColorHelper.GetCardBackground(note.Color, _themeMode)),
            BorderBrush = selected ? Brush(_palette.Accent) : Brush(_palette.TodoCardBorder),
            BorderThickness = new Thickness(selected ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 6, 6),
            Margin = new Thickness(0, 0, 0, 6),
            Tag = note.Id,
            Cursor = WpfCursors.Hand
        };
        row.MouseLeftButtonUp += (_, e) =>
        {
            if (e.OriginalSource is WpfButton)
            {
                return;
            }

            SelectNote(note.Id);
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var content = new StackPanel();
        var titleRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
        if (note.Pinned)
        {
            titleRow.Children.Add(new TextBlock
            {
                Text = "★ ",
                FontSize = Scaled(10),
                Foreground = Brush(_palette.TodoPinActive),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        titleRow.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(note.Title) ? "无标题" : note.Title,
            FontSize = Scaled(12),
            FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Brush(_palette.TextPrimary),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 120
        });
        content.Children.Add(titleRow);

        var preview = note.Content.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (!string.IsNullOrEmpty(preview))
        {
            content.Children.Add(new TextBlock
            {
                Text = preview,
                FontSize = Scaled(10),
                Foreground = Brush(_palette.TextSubtle),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = ScratchColorHelper.FormatRelativeTime(note.UpdatedAt),
            FontSize = Scaled(9),
            Foreground = Brush(_palette.TextMuted),
            Margin = new Thickness(0, 4, 0, 0)
        });
        Grid.SetColumn(content, 0);
        grid.Children.Add(content);

        var dot = new Border
        {
            Width = 8,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(ScratchColorHelper.GetAccentDot(note.Color)),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 2, 0, 0)
        };
        Grid.SetColumn(dot, 1);
        grid.Children.Add(dot);

        row.Child = grid;
        return row;
    }

    private void SelectNote(string? id)
    {
        FlushSave();
        _selectedId = id;
        _suppressEditorEvents = true;

        var note = id is null ? null : _store.GetScratchNote(id);
        var hasSelection = note is not null;
        TitleBox.IsEnabled = hasSelection;
        ContentBox.IsEnabled = hasSelection;
        PinBtn.IsEnabled = hasSelection;
        DuplicateBtn.IsEnabled = hasSelection;
        CopyBtn.IsEnabled = hasSelection;
        DeleteBtn.IsEnabled = hasSelection;
        ColorPanel.IsEnabled = hasSelection;

        EditorEmptyPanel.Visibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
        TitleBox.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        ContentBox.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        ColorPanel.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        PinBtn.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        DuplicateBtn.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        CopyBtn.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        DeleteBtn.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        SaveStatusText.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;

        if (note is not null)
        {
            TitleBox.Text = note.Title;
            ContentBox.Text = note.Content;
            PinBtn.Content = note.Pinned ? "取消置顶" : "置顶";
            SaveStatusText.Text = $"更新于 {note.UpdatedAt:HH:mm}";
        }
        else
        {
            TitleBox.Clear();
            ContentBox.Clear();
            SaveStatusText.Text = string.Empty;
        }

        _suppressEditorEvents = false;
        RefreshList();
    }

    private void ScheduleSave()
    {
        if (_suppressEditorEvents || _selectedId is null)
        {
            return;
        }

        SaveStatusText.Text = "保存中…";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void FlushSave()
    {
        _saveTimer.Stop();
        if (_suppressEditorEvents || _selectedId is null)
        {
            return;
        }

        _store.UpdateScratchNote(_selectedId, TitleBox.Text, ContentBox.Text);
        SaveStatusText.Text = $"已保存 {DateTime.Now:HH:mm}";
        RefreshList();
        _onChanged();
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e) => ScheduleSave();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _search = SearchBox.Text.Trim();
        RefreshList();
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        if (!_store.CanAddScratchNote())
        {
            WpfMessageBox.Show($"最多保存 {TodoStore.ScratchNoteLimit} 条便签，请删除部分后再新建。",
                "速记便签", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var note = _store.AddScratchNote();
        SelectNote(note.Id);
        TitleBox.Focus();
        _onChanged();
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is null)
        {
            return;
        }

        var note = _store.GetScratchNote(_selectedId);
        if (note is null)
        {
            return;
        }

        _store.SetScratchPinned(_selectedId, !note.Pinned);
        PinBtn.Content = note.Pinned ? "取消置顶" : "置顶";
        RefreshList();
        _onChanged();
    }

    private void ColorBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is null || sender is not WpfButton { Tag: string color })
        {
            return;
        }

        _store.SetScratchColor(_selectedId, color);
        RefreshList();
        _onChanged();
    }

    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is null)
        {
            return;
        }

        var copy = _store.DuplicateScratchNote(_selectedId);
        if (copy is null)
        {
            WpfMessageBox.Show($"最多保存 {TodoStore.ScratchNoteLimit} 条便签。",
                "速记便签", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectNote(copy.Id);
        _onChanged();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is null)
        {
            return;
        }

        var note = _store.GetScratchNote(_selectedId);
        if (note is null)
        {
            return;
        }

        var text = string.IsNullOrWhiteSpace(note.Title)
            ? note.Content
            : $"{note.Title}\n\n{note.Content}";
        WpfClipboard.SetText(text);
        SaveStatusText.Text = "已复制到剪贴板";
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is null)
        {
            return;
        }

        var note = _store.GetScratchNote(_selectedId);
        if (note is null)
        {
            return;
        }

        var result = WpfMessageBox.Show($"删除便签「{note.Title}」？", "确认删除",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var id = _selectedId;
        _store.RemoveScratchNote(id);
        SelectNote(_store.GetScratchPreviewNote()?.Id);
        _onChanged();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        FlushSave();
        base.OnClosing(e);
    }

    private double Scaled(double baseSize) =>
        FontScaleHelper.ScaledSize(baseSize, _settings.FontScale);

    private static SolidColorBrush Brush(WpfColor color) => new(color);
}
