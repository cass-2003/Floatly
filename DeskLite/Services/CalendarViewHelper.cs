namespace DeskLite.Services;

public enum CalendarViewMode
{
    Week,
    Month
}

public static class CalendarViewHelper
{
    public static CalendarViewMode ParseMode(string? value) =>
        string.Equals(value, "month", StringComparison.OrdinalIgnoreCase) ? CalendarViewMode.Month : CalendarViewMode.Week;

    public static string ToSettingValue(CalendarViewMode mode) =>
        mode == CalendarViewMode.Month ? "month" : "week";

    public static DateTime StartOfWeek(DateTime date) =>
        date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7);

    public static IEnumerable<DateTime> GetWeekDays(DateTime anchor) =>
        Enumerable.Range(0, 7).Select(i => StartOfWeek(anchor).AddDays(i));

    public static IEnumerable<DateTime> GetMonthGridDays(DateTime anchor)
    {
        var first = new DateTime(anchor.Year, anchor.Month, 1);
        var start = StartOfWeek(first);
        var last = first.AddMonths(1).AddDays(-1);
        var end = StartOfWeek(last).AddDays(6);
        var days = (end - start).Days + 1;
        for (var i = 0; i < days; i++)
        {
            yield return start.AddDays(i);
        }
    }

    public static bool IsSameMonth(DateTime day, DateTime anchor) =>
        day.Year == anchor.Year && day.Month == anchor.Month;

    public static string GetTitle(CalendarViewMode mode, DateTime anchor, DateTime? preview)
    {
        if (preview is not null && preview.Value.Date != DateTime.Today)
        {
            return $"查看 {preview.Value:yyyy/M/d}";
        }

        return mode switch
        {
            CalendarViewMode.Month => $"{anchor:yyyy年M月}",
            _ => $"{StartOfWeek(anchor):M/d} - {StartOfWeek(anchor).AddDays(6):M/d}"
        };
    }
}
