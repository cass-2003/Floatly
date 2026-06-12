namespace DeskLite.Services;

/// <summary>Maps Open-Meteo WMO weather codes to Meteocons icon slugs (fill or line).</summary>
public static class WeatherIconMapper
{
    public static string SlugForCode(int code, bool isDay) => code switch
    {
        0 => isDay ? "clear-day" : "clear-night",
        1 => isDay ? "partly-cloudy-day" : "partly-cloudy-night",
        2 => isDay ? "partly-cloudy-day" : "partly-cloudy-night",
        3 => "overcast",
        45 or 48 => isDay ? "fog-day" : "fog-night",
        51 or 53 or 55 => "drizzle",
        56 or 57 => "sleet",
        61 or 63 or 65 => "rain",
        66 or 67 => "sleet",
        71 or 73 or 75 or 77 => "snow",
        80 or 81 or 82 => isDay ? "partly-cloudy-day-rain" : "partly-cloudy-night-rain",
        85 or 86 => "snow",
        95 => isDay ? "thunderstorms-day" : "thunderstorms-night",
        96 or 99 => isDay ? "thunderstorms-day-rain" : "thunderstorms-night-rain",
        _ => isDay ? "cloudy" : "cloudy"
    };

    public static bool InferIsDay(DateTime now, string? sunrise, string? sunset)
    {
        if (!TimeSpan.TryParse(sunrise, out var rise) || !TimeSpan.TryParse(sunset, out var set))
        {
            return now.Hour is >= 6 and < 18;
        }

        var t = now.TimeOfDay;
        return t >= rise && t < set;
    }
}
