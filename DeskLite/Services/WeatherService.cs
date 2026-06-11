using System.IO;
using System.Net.Http;
using System.Text.Json;
using DeskLite.Models;

namespace DeskLite.Services;

public sealed class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string CachePath =>
        Path.Combine(AppConstants.AppDataDir, "weather-cache.json");

    public WeatherCache? LoadCache()
    {
        try
        {
            if (!File.Exists(CachePath))
            {
                return null;
            }

            var json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<WeatherCache>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<WeatherFetchResult?> FetchAsync(
        string city,
        double? lat,
        double? lon,
        string? region = null,
        string? locationSource = null,
        CancellationToken ct = default)
    {
        try
        {
            GeoResult? geo = null;
            if (lat is null || lon is null)
            {
                geo = await GeocodeAsync(city, ct);
                if (geo is null)
                {
                    var cached = LoadCache();
                    return cached is null ? null : new WeatherFetchResult(cached, 0, 0);
                }

                lat = geo.Latitude;
                lon = geo.Longitude;
                city = geo.Name;
                region ??= geo.Region;
            }

            var url =
                $"https://api.open-meteo.com/v1/forecast?latitude={lat.Value:F4}&longitude={lon.Value:F4}" +
                "&current=temperature_2m,apparent_temperature,weather_code,is_day" +
                "&daily=temperature_2m_max,temperature_2m_min,weather_code,sunrise,sunset" +
                "&timezone=auto&forecast_days=2";

            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var current = root.GetProperty("current");
            var code = current.GetProperty("weather_code").GetInt32();
            var temp = (int)Math.Round(current.GetProperty("temperature_2m").GetDouble());
            var feels = current.TryGetProperty("apparent_temperature", out var feelsEl)
                ? (int)Math.Round(feelsEl.GetDouble())
                : temp;
            var isDay = !current.TryGetProperty("is_day", out var isDayEl) || isDayEl.GetInt32() == 1;

            var daily = root.GetProperty("daily");
            var max = (int)Math.Round(daily.GetProperty("temperature_2m_max")[0].GetDouble());
            var min = (int)Math.Round(daily.GetProperty("temperature_2m_min")[0].GetDouble());
            var sunrise = FormatSunTime(daily.GetProperty("sunrise")[0].GetString());
            var sunset = FormatSunTime(daily.GetProperty("sunset")[0].GetString());

            int? tomorrowMin = null;
            int? tomorrowMax = null;
            int? tomorrowCode = null;
            string? tomorrowDesc = null;
            string? tomorrowIconSlug = null;

            if (daily.GetProperty("temperature_2m_max").GetArrayLength() > 1)
            {
                var tCode = daily.GetProperty("weather_code")[1].GetInt32();
                tomorrowCode = tCode;
                tomorrowMin = (int)Math.Round(daily.GetProperty("temperature_2m_min")[1].GetDouble());
                tomorrowMax = (int)Math.Round(daily.GetProperty("temperature_2m_max")[1].GetDouble());
                tomorrowDesc = DescribeWeather(tCode);
                tomorrowIconSlug = WeatherIconMapper.SlugForCode(tCode, isDay: true);
            }

            var cache = new WeatherCache
            {
                City = city,
                Region = region,
                LocationSource = locationSource ?? "manual",
                Temperature = temp,
                TempMin = min,
                TempMax = max,
                FeelsLike = feels,
                WeatherCode = code,
                IsDay = isDay,
                Description = DescribeWeather(code),
                IconSlug = WeatherIconMapper.SlugForCode(code, isDay),
                Sunrise = sunrise,
                Sunset = sunset,
                TomorrowMin = tomorrowMin,
                TomorrowMax = tomorrowMax,
                TomorrowWeatherCode = tomorrowCode,
                TomorrowDescription = tomorrowDesc,
                TomorrowIconSlug = tomorrowIconSlug,
                UpdatedAt = DateTime.Now
            };

            SaveCache(cache);
            return new WeatherFetchResult(cache, lat.Value, lon.Value);
        }
        catch
        {
            var cached = LoadCache();
            return cached is null ? null : new WeatherFetchResult(cached, 0, 0);
        }
    }

    public sealed record WeatherFetchResult(WeatherCache Cache, double Latitude, double Longitude);

    private static string? FormatSunTime(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso) || !DateTime.TryParse(iso, out var dt))
        {
            return null;
        }

        return dt.ToString("HH:mm");
    }

    private static void SaveCache(WeatherCache cache)
    {
        var dir = Path.GetDirectoryName(CachePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CachePath, json);
    }

    private static async Task<GeoResult?> GeocodeAsync(string city, CancellationToken ct)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=zh&format=json";
        var json = await Http.GetStringAsync(url, ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
        {
            return null;
        }

        var first = results[0];
        var name = first.GetProperty("name").GetString() ?? city;
        var region = first.TryGetProperty("admin1", out var admin) ? admin.GetString() : null;
        return new GeoResult(
            first.GetProperty("latitude").GetDouble(),
            first.GetProperty("longitude").GetDouble(),
            name,
            region);
    }

    private static string DescribeWeather(int code) => code switch
    {
        0 => "晴",
        1 or 2 or 3 => "多云",
        45 or 48 => "雾",
        51 or 53 or 55 => "毛毛雨",
        61 or 63 or 65 => "雨",
        71 or 73 or 75 => "雪",
        80 or 81 or 82 => "阵雨",
        95 or 96 or 99 => "雷雨",
        _ => "阴"
    };

    private sealed record GeoResult(double Latitude, double Longitude, string Name, string? Region);
}
