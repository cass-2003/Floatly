namespace DeskLite.Models;

public sealed class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    /// <summary>Reminder time HH:mm.</summary>
    public string? Time { get; set; }
    /// <summary>Scheduled/show date yyyy-MM-dd.</summary>
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    /// <summary>Deadline date yyyy-MM-dd. Null = no due date.</summary>
    public string? DueDate { get; set; }
    public bool Done { get; set; }
    public bool Pinned { get; set; }
}
