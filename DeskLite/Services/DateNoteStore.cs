using DeskLite.Models;

namespace DeskLite.Services;

public sealed class DateNoteStore
{
    private AppDataFile _data;

    public DateNoteStore()
    {
        _data = JsonStore.LoadData();
        _data.DateNotes ??= new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public static string FormatKey(DateTime date) => date.ToString("yyyy-MM-dd");

    public bool HasNote(DateTime date)
    {
        var key = FormatKey(date);
        return _data.DateNotes.TryGetValue(key, out var note) && !string.IsNullOrWhiteSpace(note);
    }

    public string? GetNote(DateTime date)
    {
        var key = FormatKey(date);
        return _data.DateNotes.TryGetValue(key, out var note) && !string.IsNullOrWhiteSpace(note)
            ? note
            : null;
    }

    public void SetNote(DateTime date, string? note)
    {
        var key = FormatKey(date);
        if (string.IsNullOrWhiteSpace(note))
        {
            _data.DateNotes.Remove(key);
        }
        else
        {
            _data.DateNotes[key] = note.Trim();
        }

        Save();
    }

    private void Save() => JsonStore.SaveData(_data);
}
