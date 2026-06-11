using System.Globalization;
using Lunar;

namespace DeskLite.Services;

/// <summary>
/// 黄历详情（宜忌、神煞、时辰等），基于 lunar-csharp 本地计算。
/// </summary>
public static class HuangLiService
{
    public static HuangLiDayInfo Get(DateTime date, bool includeCurrentTime = false)
    {
        var solar = Solar.FromDate(date.Date);
        var lunar = solar.Lunar;

        var weekNo = ISOWeek.GetWeekOfYear(date);
        var weekName = solar.Week == 0 ? "周日" : $"周{solar.WeekInChinese}";
        var headline = $"{lunar.YearInGanZhi}年 {lunar.MonthInGanZhi}月 {lunar.DayInGanZhi}日 · 属{lunar.YearShengXiao} · {weekName} · 第{weekNo}周";

        string? currentHourZhi = null;
        string? currentTimeSummary = null;
        if (includeCurrentTime && date.Date == DateTime.Today)
        {
            var now = DateTime.Now;
            var time = LunarTime.FromYmdHms(lunar.Year, lunar.Month, lunar.Day, now.Hour, now.Minute, now.Second);
            currentHourZhi = time.Zhi;
            currentTimeSummary = $"当前时辰 {time.Zhi}时 {time.GanZhi} {time.MinHm}-{time.MaxHm} · 冲{time.ChongDesc} · {MapLuckShort(time.TianShenLuck)}";
        }

        var timeSlots = lunar.Times
            .Select(t => new HuangLiTimeSlot(
                t.Zhi,
                MapLuckShort(t.TianShenLuck),
                currentHourZhi is not null && t.Zhi == currentHourZhi))
            .ToList();

        return new HuangLiDayInfo(
            headline,
            ToItems(lunar.DayYi),
            ToItems(lunar.DayJi),
            lunar.DayNaYin,
            $"{lunar.DayChongDesc}煞{lunar.DaySha}",
            lunar.DayTianShen,
            $"{lunar.ZhiXing}日",
            ToItems(lunar.DayJiShen),
            ToItems(lunar.DayXiongSha),
            lunar.DayPositionTai,
            $"{lunar.PengZuGan} · {lunar.PengZuZhi}",
            $"{lunar.Xiu}{lunar.XiuLuck}",
            lunar.DayPositionXiDesc,
            lunar.DayPositionCaiDesc,
            lunar.GetDayPositionFuDesc(2),
            timeSlots,
            currentTimeSummary);
    }

    private static IReadOnlyList<string> ToItems(IList<string> items)
    {
        if (items.Count == 0 || (items.Count == 1 && items[0] == "无"))
        {
            return ["无"];
        }

        return items.Where(i => i != "无").ToList();
    }

    private static string MapLuckShort(string? luck) =>
        luck switch
        {
            "吉" => "吉",
            "凶" => "凶",
            _ => "平"
        };
}

public sealed record HuangLiTimeSlot(string Zhi, string Luck, bool IsCurrent);

public sealed record HuangLiDayInfo(
    string Headline,
    IReadOnlyList<string> YiItems,
    IReadOnlyList<string> JiItems,
    string WuXing,
    string ChongSha,
    string ZhiShen,
    string JianChu,
    IReadOnlyList<string> JiShen,
    IReadOnlyList<string> XiongShen,
    string TaiShen,
    string PengZu,
    string Xiu,
    string XiShen,
    string CaiShen,
    string FuShen,
    IReadOnlyList<HuangLiTimeSlot> TimeSlots,
    string? CurrentTimeSummary);
