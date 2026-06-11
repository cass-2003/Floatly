namespace DeskLite.Services;

public enum OffWorkState
{
    Weekend,
    BeforeWork,
    Working,
    AfterWork,
    Overtime
}

public readonly record struct OffWorkInfo(
    string Title,
    string MainText,
    string? Detail,
    double? DayProgressPercent,
    OffWorkState State);

public static class OffWorkService
{
    public static OffWorkInfo GetInfo(
        DateTime now,
        string? workStartTime,
        string? workEndTime,
        bool weekdaysOnly,
        bool showSeconds)
    {
        if (weekdaysOnly && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new OffWorkInfo("周末", "周末愉快 🎉", "好好休息", null, OffWorkState.Weekend);
        }

        if (!TryParseTime(workStartTime, out var start) || !TryParseTime(workEndTime, out var end))
        {
            return new OffWorkInfo("下班倒计时", "请设置上下班时间", null, null, OffWorkState.BeforeWork);
        }

        var todayStart = now.Date.Add(start.ToTimeSpan());
        var todayEnd = now.Date.Add(end.ToTimeSpan());
        if (todayEnd <= todayStart)
        {
            todayEnd = todayEnd.AddDays(1);
        }

        if (now < todayStart)
        {
            var remaining = todayStart - now;
            return new OffWorkInfo(
                "上班倒计时",
                FormatRemaining(remaining, showSeconds),
                $"上班时间 {start:HH\\:mm}",
                null,
                OffWorkState.BeforeWork);
        }

        if (now < todayEnd)
        {
            var remaining = todayEnd - now;
            var total = todayEnd - todayStart;
            var elapsed = now - todayStart;
            var progress = total.TotalSeconds > 0
                ? Math.Clamp(elapsed.TotalSeconds / total.TotalSeconds * 100, 0, 100)
                : 0;
            return new OffWorkInfo(
                "下班倒计时",
                FormatRemaining(remaining, showSeconds),
                $"距离下班还有 {FormatFriendly(remaining)}",
                progress,
                OffWorkState.Working);
        }

        var overtime = now - todayEnd;
        if (overtime.TotalMinutes < 1)
        {
            return new OffWorkInfo("下班啦", "已下班 🎉", "辛苦了，好好休息", 100, OffWorkState.AfterWork);
        }

        return new OffWorkInfo(
            "加班中",
            $"+{FormatRemaining(overtime, showSeconds)}",
            $"已超出下班时间 {FormatFriendly(overtime)}",
            100,
            OffWorkState.Overtime);
    }

    public static bool TryParseTime(string? text, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return TimeOnly.TryParse(text.Trim(), out time);
    }

    private static string FormatRemaining(TimeSpan span, bool showSeconds)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        if (showSeconds)
        {
            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}"
                : $"{span.Minutes:D2}:{span.Seconds:D2}";
        }

        return FormatFriendly(span);
    }

    private static string FormatFriendly(TimeSpan span)
    {
        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours}小时{span.Minutes}分";
        }

        if (span.TotalMinutes >= 1)
        {
            return $"{(int)span.TotalMinutes}分钟";
        }

        return $"{Math.Max(1, span.Seconds)}秒";
    }
}
