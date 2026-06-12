using DeskLite.Models;

namespace DeskLite.Services;

public sealed class TodoStore
{
    private const int MainPanelLimit = 3;
    public const int ScratchNoteLimit = 20;
    private AppDataFile _data;

    public TodoStore()
    {
        _data = JsonStore.LoadData();
        MigrateLegacyScratch();
    }

    public AppDataFile Data => _data;

    public int MainPanelMax => MainPanelLimit;

    public IReadOnlyList<TodoItem> GetTodayActiveTodos()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return _data.Todos
            .Where(t => !t.Done && (string.IsNullOrWhiteSpace(t.Date) || t.Date == today))
            .OrderByDescending(t => t.Pinned)
            .ThenBy(t => string.IsNullOrWhiteSpace(t.Time) ? "99:99" : t.Time)
            .ThenBy(t => t.Title)
            .ToList();
    }

    public IReadOnlyList<TodoItem> GetTodayTodos() =>
        GetTodayActiveTodos().Take(MainPanelLimit).ToList();

    public int GetTodayHiddenCount()
    {
        var total = GetTodayActiveTodos().Count;
        return Math.Max(0, total - MainPanelLimit);
    }

    public IReadOnlyList<TodoItem> GetTodayTimedTodos()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return _data.Todos
            .Where(t => !t.Done
                        && !string.IsNullOrWhiteSpace(t.Time)
                        && GetReminderDate(t) == today)
            .ToList();
    }

    public static string GetReminderDate(TodoItem item) =>
        string.IsNullOrWhiteSpace(item.DueDate) ? item.Date : item.DueDate!;

    public IReadOnlyList<TodoItem> GetActiveTodos() =>
        _data.Todos
            .Where(t => !t.Done)
            .OrderByDescending(t => t.Pinned)
            .ThenBy(t => t.Date)
            .ThenBy(t => string.IsNullOrWhiteSpace(t.Time) ? "99:99" : t.Time)
            .ThenBy(t => t.Title)
            .ToList();

    public IReadOnlyList<TodoItem> GetCompletedTodos() =>
        _data.Todos
            .Where(t => t.Done)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => string.IsNullOrWhiteSpace(t.Time) ? "00:00" : t.Time)
            .ThenBy(t => t.Title)
            .ToList();

    public IReadOnlyList<TodoItem> GetAllTodos() =>
        _data.Todos
            .OrderByDescending(t => t.Pinned)
            .ThenBy(t => t.Done)
            .ThenBy(t => t.Date)
            .ThenBy(t => string.IsNullOrWhiteSpace(t.Time) ? "99:99" : t.Time)
            .ThenBy(t => t.Title)
            .ToList();

    public TodoItem? GetById(string id) =>
        _data.Todos.FirstOrDefault(t => t.Id == id);

    public void Add(string title, string? time = null, string? dueDate = null)
    {
        title = title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        time = NormalizeTime(time);
        dueDate = NormalizeDate(dueDate);

        _data.Todos.Add(new TodoItem
        {
            Title = title,
            Time = time,
            DueDate = dueDate,
            Date = DateTime.Today.ToString("yyyy-MM-dd")
        });
        Save();
    }

    public void ToggleDone(string id)
    {
        var item = _data.Todos.FirstOrDefault(t => t.Id == id);
        if (item is null)
        {
            return;
        }

        item.Done = !item.Done;
        Save();
    }

    public void SetPinned(string id, bool pinned)
    {
        var item = _data.Todos.FirstOrDefault(t => t.Id == id);
        if (item is null)
        {
            return;
        }

        item.Pinned = pinned;
        Save();
    }

    public void Update(string id, string title, string? time = null, string? dueDate = null)
    {
        var item = _data.Todos.FirstOrDefault(t => t.Id == id);
        if (item is null)
        {
            return;
        }

        title = title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        item.Title = title;
        item.Time = NormalizeTime(time);
        item.DueDate = NormalizeDate(dueDate);
        Save();
    }

    private static string? NormalizeTime(string? time)
    {
        if (string.IsNullOrWhiteSpace(time) || !TimeSpan.TryParse(time, out _))
        {
            return null;
        }

        return time.Length >= 5 ? time[..5] : time;
    }

    private static string? NormalizeDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        if (DateTime.TryParse(date, out var dt))
        {
            return dt.ToString("yyyy-MM-dd");
        }

        return null;
    }

    public void Remove(string id)
    {
        _data.Todos.RemoveAll(t => t.Id == id);
        Save();
    }

    private void MigrateLegacyScratch()
    {
        if (_data.Notes.Count > 0 || string.IsNullOrWhiteSpace(_data.Scratch))
        {
            return;
        }

        var now = DateTime.Now;
        _data.Notes.Add(new ScratchNote
        {
            Content = _data.Scratch.Trim(),
            Title = ScratchColorHelper.DeriveTitle(_data.Scratch, 1),
            CreatedAt = now,
            UpdatedAt = now
        });
        _data.Scratch = string.Empty;
        Save();
    }

    public IReadOnlyList<ScratchNote> GetScratchNotes() =>
        _data.Notes
            .OrderByDescending(n => n.Pinned)
            .ThenByDescending(n => n.UpdatedAt)
            .ToList();

    public ScratchNote? GetScratchNote(string id) =>
        _data.Notes.FirstOrDefault(n => n.Id == id);

    public ScratchNote? GetScratchPreviewNote()
    {
        var notes = GetScratchNotes();
        return notes.FirstOrDefault(n => n.Pinned) ?? notes.FirstOrDefault();
    }

    public bool CanAddScratchNote() => _data.Notes.Count < ScratchNoteLimit;

    public ScratchNote AddScratchNote(string? title = null, string? content = null)
    {
        if (!CanAddScratchNote())
        {
            throw new InvalidOperationException($"最多 {ScratchNoteLimit} 条便签");
        }

        var now = DateTime.Now;
        var index = _data.Notes.Count + 1;
        var note = new ScratchNote
        {
            Title = string.IsNullOrWhiteSpace(title)
                ? ScratchColorHelper.DeriveTitle(content, index)
                : title.Trim(),
            Content = content?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };
        _data.Notes.Add(note);
        Save();
        return note;
    }

    public void UpdateScratchNote(string id, string title, string content)
    {
        var note = GetScratchNote(id);
        if (note is null)
        {
            return;
        }

        note.Title = string.IsNullOrWhiteSpace(title)
            ? ScratchColorHelper.DeriveTitle(content, _data.Notes.IndexOf(note) + 1)
            : title.Trim();
        note.Content = content;
        note.UpdatedAt = DateTime.Now;
        Save();
    }

    public void SetScratchPinned(string id, bool pinned)
    {
        var note = GetScratchNote(id);
        if (note is null)
        {
            return;
        }

        note.Pinned = pinned;
        note.UpdatedAt = DateTime.Now;
        Save();
    }

    public void SetScratchColor(string id, string color)
    {
        var note = GetScratchNote(id);
        if (note is null)
        {
            return;
        }

        note.Color = ScratchNoteColors.All.Contains(color) ? color : ScratchNoteColors.Default;
        note.UpdatedAt = DateTime.Now;
        Save();
    }

    public ScratchNote? DuplicateScratchNote(string id)
    {
        var source = GetScratchNote(id);
        if (source is null || !CanAddScratchNote())
        {
            return null;
        }

        var now = DateTime.Now;
        var copy = new ScratchNote
        {
            Title = source.Title + " (副本)",
            Content = source.Content,
            Color = source.Color,
            Pinned = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        _data.Notes.Add(copy);
        Save();
        return copy;
    }

    public void RemoveScratchNote(string id)
    {
        _data.Notes.RemoveAll(n => n.Id == id);
        Save();
    }

    public void AddCountdown(string title, DateTime date, bool repeatYearly)
    {
        title = title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _data.Countdowns.Add(new CountdownItem
        {
            Title = title,
            Date = date.ToString("yyyy-MM-dd"),
            RepeatYearly = repeatYearly
        });
        Save();
    }

    private void Save() => JsonStore.SaveData(_data);
}
