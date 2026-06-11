using DeskLite.Models;

namespace DeskLite.Services;

public sealed class TodoReminderService
{
    private readonly HashSet<string> _notifiedToday = [];
    private string _lastDate = DateTime.Today.ToString("yyyy-MM-dd");

    public IEnumerable<TodoItem> CheckDue(IReadOnlyList<TodoItem> todos, DateTime now)
    {
        var today = now.ToString("yyyy-MM-dd");
        if (_lastDate != today)
        {
            _notifiedToday.Clear();
            _lastDate = today;
        }

        var current = now.ToString("HH:mm");
        foreach (var todo in todos)
        {
            if (todo.Done || string.IsNullOrWhiteSpace(todo.Time) || TodoStore.GetReminderDate(todo) != today)
            {
                continue;
            }

            if (todo.Time != current || _notifiedToday.Contains(todo.Id))
            {
                continue;
            }

            _notifiedToday.Add(todo.Id);
            yield return todo;
        }
    }
}
