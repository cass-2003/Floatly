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

        var yi = FormatList(lunar.DayYi);
        var ji = FormatList(lunar.DayJi);

        var meta = $"五行 {lunar.DayNaYin} · 冲煞 {lunar.DayChongDesc}煞{lunar.DaySha} · 值神 {lunar.DayTianShen} · 建除 {lunar.ZhiXing}日";

        var gods = $"吉神宜趋 {FormatList(lunar.DayJiShen)} · 凶神宜忌 {FormatList(lunar.DayXiongSha)}";
        var tai = $"今日胎神 {lunar.DayPositionTai}";

        var pengZu = $"彭祖百忌 {lunar.PengZuGan} {lunar.PengZuZhi} · 二十八宿 {lunar.Xiu}{lunar.XiuLuck}";

        var deity = $"喜神{lunar.DayPositionXiDesc} · 财神{lunar.DayPositionCaiDesc} · 福神{lunar.GetDayPositionFuDesc(2)}";

        var timeLuck = BuildTimeLuckLine(lunar);
        var currentTime = includeCurrentTime && date.Date == DateTime.Today
            ? BuildCurrentTimeLine(lunar, DateTime.Now)
            : null;

        return new HuangLiDayInfo(
            headline,
            yi,
            ji,
            meta,
            gods,
            tai,
            pengZu,
            deity,
            timeLuck,
            currentTime);
    }

    private static string BuildTimeLuckLine(global::Lunar.Lunar lunar)
    {
        var parts = lunar.Times.Select(t => $"{t.Zhi}{MapLuckShort(t.TianShenLuck)}");
        return "时辰吉凶 " + string.Join(" ", parts);
    }

    private static string? BuildCurrentTimeLine(global::Lunar.Lunar lunar, DateTime now)
    {
        var time = LunarTime.FromYmdHms(
            lunar.Year,
            lunar.Month,
            lunar.Day,
            now.Hour,
            now.Minute,
            now.Second);

        return $"当前时辰 {time.Zhi}时 {time.GanZhi} {time.MinHm}-{time.MaxHm} · 冲{time.ChongDesc} · {MapLuckShort(time.TianShenLuck)}";
    }

    private static string MapLuckShort(string? luck) =>
        luck switch
        {
            "吉" => "吉",
            "凶" => "凶",
            _ => "平"
        };

    private static string FormatList(IList<string> items)
    {
        if (items.Count == 0)
        {
            return "无";
        }

        var text = string.Join(" ", items);
        return text == "无" ? text : text;
    }
}

public sealed record HuangLiDayInfo(
    string Headline,
    string Yi,
    string Ji,
    string Meta,
    string Gods,
    string TaiShen,
    string PengZu,
    string Deity,
    string TimeLuck,
    string? CurrentTime);
