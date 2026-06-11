namespace DeskLite.Models;

public sealed class TodoDisplayItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Time { get; init; }
    public string? DueDate { get; init; }
    public bool Pinned { get; init; }
    public bool Done { get; init; }
    public string Date { get; init; } = string.Empty;
    public bool HasTime => !string.IsNullOrWhiteSpace(Time);
    public bool HasDueDate => !string.IsNullOrWhiteSpace(DueDate);
    public string DueDateLabel => FormatDueDateLabel(DueDate);
    public string PinIcon => Pinned ? "★" : "☆";
    public string Display { get; init; } = string.Empty;

    public static TodoDisplayItem From(TodoItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        Time = item.Time,
        DueDate = item.DueDate,
        Pinned = item.Pinned,
        Done = item.Done,
        Date = item.Date,
        Display = FormatDisplay(item)
    };

    private static string FormatDueDateLabel(string? dueDate)
    {
        if (string.IsNullOrWhiteSpace(dueDate) || !DateTime.TryParse(dueDate, out var dt))
        {
            return dueDate ?? string.Empty;
        }

        if (dt.Date == DateTime.Today)
        {
            return "今天截止";
        }

        if (dt.Date == DateTime.Today.AddDays(1))
        {
            return "明天截止";
        }

        if (dt.Date < DateTime.Today)
        {
            return $"已逾期 {dt:M/d}";
        }

        return $"截止 {dt:M/d}";
    }

    public static string FormatDisplay(TodoItem item)
    {
        var body = string.IsNullOrWhiteSpace(item.Time) ? item.Title : $"{item.Time} {item.Title}";
        return body;
    }
}
