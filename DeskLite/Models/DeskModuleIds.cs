namespace DeskLite.Models;

public static class DeskModuleIds
{
    public const string HuangLi = "huangli";
    public const string Weather = "weather";
    public const string YearProgress = "yearprogress";
    public const string Countdown = "countdown";
    public const string DailyQuote = "dailyquote";
    public const string Scratch = "scratch";
    public const string Todos = "todos";

    public static readonly string[] DefaultOrder =
    [
        HuangLi,
        YearProgress,
        Weather,
        Countdown,
        DailyQuote,
        Scratch,
        Todos
    ];

    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>
        {
            [HuangLi] = "黄历详情",
            [Weather] = "天气",
            [YearProgress] = "年进度",
            [Countdown] = "倒数日",
            [DailyQuote] = "每日一句",
            [Scratch] = "速记便签",
            [Todos] = "今日待办"
        };

    public static List<string> Normalize(IEnumerable<string>? order)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (order != null)
        {
            foreach (var id in order)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var key = id.Trim().ToLowerInvariant();
                if (DisplayNames.ContainsKey(key) && seen.Add(key))
                {
                    result.Add(key);
                }
            }
        }

        foreach (var id in DefaultOrder)
        {
            if (!seen.Contains(id))
            {
                result.Add(id);
            }
        }

        return result;
    }
}
