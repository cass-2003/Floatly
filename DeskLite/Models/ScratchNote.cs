namespace DeskLite.Models;

public sealed class ScratchNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = ScratchNoteColors.Default;
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public static class ScratchNoteColors
{
    public const string Default = "default";
    public const string Yellow = "yellow";
    public const string Green = "green";
    public const string Blue = "blue";
    public const string Pink = "pink";

    public static readonly string[] All = [Default, Yellow, Green, Blue, Pink];
}
