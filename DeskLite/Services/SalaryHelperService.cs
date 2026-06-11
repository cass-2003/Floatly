using System.Globalization;
using DeskLite.Models;

namespace DeskLite.Services;

public readonly record struct SalaryInfo(
    string Title,
    string AmountText,
    string? Subtitle,
    string PerSecondText,
    string HourlyText,
    string WorkDurationText,
    bool IsWorking);

public static class SalaryHelperService
{
    public static decimal PerSecondRate(decimal monthlySalary, int workDaysPerMonth, double workHoursPerDay)
    {
        if (monthlySalary <= 0 || workDaysPerMonth <= 0 || workHoursPerDay <= 0)
        {
            return 0;
        }

        var secondsPerMonth = workDaysPerMonth * (decimal)workHoursPerDay * 3600m;
        return monthlySalary / secondsPerMonth;
    }

    public static decimal PerHourRate(decimal monthlySalary, int workDaysPerMonth, double workHoursPerDay)
    {
        if (monthlySalary <= 0 || workDaysPerMonth <= 0 || workHoursPerDay <= 0)
        {
            return 0;
        }

        return monthlySalary / workDaysPerMonth / (decimal)workHoursPerDay;
    }

    public static SalaryInfo GetInfo(DateTime now, AppSettings settings)
    {
        if (settings.MonthlySalary <= 0)
        {
            return new SalaryInfo(
                "摸鱼小助手",
                "请设置月薪",
                "在设置中填写月薪后显示实时收入",
                "每秒 +¥0.0000",
                "时薪  ¥0.00",
                "今日工作  00:00:00",
                false);
        }

        if (settings.OffWorkWeekdaysOnly && now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new SalaryInfo(
                "休息中",
                "周末",
                "周末不计入工作日收入",
                "每秒 +¥0.0000",
                "时薪  ¥0.00",
                "今日工作  00:00:00",
                false);
        }

        if (!OffWorkService.TryParseTime(settings.WorkStartTime, out var start) ||
            !OffWorkService.TryParseTime(settings.WorkEndTime, out var end))
        {
            return new SalaryInfo(
                "摸鱼小助手",
                "请设置上下班时间",
                null,
                "每秒 +¥0.0000",
                "时薪  ¥0.00",
                "今日工作  00:00:00",
                false);
        }

        var rate = PerSecondRate(settings.MonthlySalary, settings.WorkDaysPerMonth, settings.WorkHoursPerDay);
        var hourly = PerHourRate(settings.MonthlySalary, settings.WorkDaysPerMonth, settings.WorkHoursPerDay);
        var perSecondText = $"每秒 +{FormatYuan(rate, 4)}";
        var hourlyText = $"时薪  {FormatYuan(hourly, 2)}";

        var todayStart = now.Date.Add(start.ToTimeSpan());
        var todayEnd = now.Date.Add(end.ToTimeSpan());
        if (todayEnd <= todayStart)
        {
            todayEnd = todayEnd.AddDays(1);
        }

        var fullDaySeconds = (todayEnd - todayStart).TotalSeconds;
        var fullDayAmount = (decimal)fullDaySeconds * rate;

        if (now < todayStart)
        {
            return new SalaryInfo(
                "今日已赚薪水",
                FormatYuan(0, 2),
                $"今日预计 {FormatYuan(fullDayAmount, 2)}",
                perSecondText,
                hourlyText,
                "今日工作  00:00:00",
                false);
        }

        if (now >= todayEnd)
        {
            return new SalaryInfo(
                "今日已赚薪水",
                FormatYuan(fullDayAmount, 2),
                "下班啦，今日收入已结算",
                perSecondText,
                hourlyText,
                $"今日工作  {FormatDuration(todayEnd - todayStart)}",
                false);
        }

        var workedSeconds = (now - todayStart).TotalSeconds;
        var earned = (decimal)workedSeconds * rate;
        return new SalaryInfo(
            "今日已赚薪水",
            FormatYuan(earned, 2),
            null,
            perSecondText,
            hourlyText,
            $"今日工作  {FormatDuration(now - todayStart)}",
            true);
    }

    private static string FormatYuan(decimal amount, int decimals) =>
        $"¥{amount.ToString($"F{decimals}", CultureInfo.InvariantCulture)}";

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }
}
