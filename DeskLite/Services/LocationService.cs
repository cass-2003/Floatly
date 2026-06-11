using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using Windows.Devices.Geolocation;

namespace DeskLite.Services;

public sealed class LocationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    static LocationService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Floatly/2.0");
    }

    /// <summary>Prefer Windows location coordinates, then enrich a city name through multiple fallbacks.</summary>
    public async Task<DetectedLocation?> DetectAsync(CancellationToken ct = default)
    {
        var device = await TryDetectByWindowsAsync(ct);
        if (device is not null)
        {
            return device;
        }

        var ip = await DetectByIpAsync(ct);
        return ip is null ? null : ip with { IpFallbackWarning = true };
    }

    /// <summary>Try Windows Geolocation API only, but still enrich the city name through multiple reverse-geocoding fallbacks.</summary>
    public Task<DetectedLocation?> TryDetectByWindowsAsync(CancellationToken ct = default) =>
        TryDetectByDeviceAsync(ct);

    public async Task<DetectedLocation?> DetectByIpAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await Http.GetStringAsync("https://ipwho.is/?lang=zh", ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean())
            {
                return null;
            }

            var city = root.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : null;
            if (!HasUsableCityName(city))
            {
                return null;
            }

            var region = root.TryGetProperty("region", out var regionEl) ? regionEl.GetString() : null;
            var lat = root.GetProperty("latitude").GetDouble();
            var lon = root.GetProperty("longitude").GetDouble();
            return new DetectedLocation(city!.Trim(), region?.Trim(), lat, lon, "ip");
        }
        catch
        {
            return null;
        }
    }

    private async Task<DetectedLocation?> TryDetectByDeviceAsync(CancellationToken ct)
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync();
            if (access != GeolocationAccessStatus.Allowed)
            {
                return null;
            }

            var geolocator = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.High,
                DesiredAccuracyInMeters = 500
            };

            var position = await geolocator.GetGeopositionAsync(
                maximumAge: TimeSpan.FromMinutes(15),
                timeout: TimeSpan.FromSeconds(15));

            var lat = position.Coordinate.Latitude;
            var lon = position.Coordinate.Longitude;
            if (lat is < -90 or > 90 || lon is < -180 or > 180)
            {
                return null;
            }

            return await ResolveDeviceLocationAsync(lat, lon, ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<DetectedLocation?> ResolveDeviceLocationAsync(double lat, double lon, CancellationToken ct)
    {
        var openMeteo = await ReverseGeocodeByOpenMeteoAsync(lat, lon, "device", ct);
        if (HasUsableCityName(openMeteo?.City))
        {
            return openMeteo;
        }

        var nominatim = await ReverseGeocodeByNominatimAsync(lat, lon, "device", ct);
        if (HasUsableCityName(nominatim?.City))
        {
            return nominatim;
        }

        var ip = await DetectByIpAsync(ct);
        if (ip is not null)
        {
            return new DetectedLocation(ip.City, ip.Region, lat, lon, "hybrid", IpFallbackWarning: true);
        }

        return openMeteo ?? nominatim ?? new DetectedLocation(FormatCoordinateCity(lat, lon), null, lat, lon, "device");
    }

    private static async Task<DetectedLocation?> ReverseGeocodeByOpenMeteoAsync(
        double lat,
        double lon,
        string source,
        CancellationToken ct)
    {
        try
        {
            var url =
                $"https://geocoding-api.open-meteo.com/v1/reverse?latitude={lat:F4}&longitude={lon:F4}&language=zh";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return new DetectedLocation(FormatCoordinateCity(lat, lon), null, lat, lon, source);
            }

            var first = results[0];
            var name = GetFirstNonEmpty(first, "name", "city", "locality", "admin3", "admin2", "admin1");
            var region = GetFirstNonEmpty(first, "admin1", "admin2", "country");
            if (!HasUsableCityName(name))
            {
                name = FormatCoordinateCity(lat, lon);
            }

            return new DetectedLocation(name!, region?.Trim(), lat, lon, source);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<DetectedLocation?> ReverseGeocodeByNominatimAsync(
        double lat,
        double lon,
        string source,
        CancellationToken ct)
    {
        try
        {
            var url =
                $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat.ToString("F6", CultureInfo.InvariantCulture)}" +
                $"&lon={lon.ToString("F6", CultureInfo.InvariantCulture)}&accept-language=zh-CN";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("address", out var address))
            {
                return null;
            }

            var name = GetFirstNonEmpty(address, "city", "town", "municipality", "county", "state_district", "village", "suburb");
            var region = GetFirstNonEmpty(address, "state", "province", "region", "county");

            if (!HasUsableCityName(name) && root.TryGetProperty("name", out var nameEl))
            {
                name = nameEl.GetString();
            }

            return HasUsableCityName(name)
                ? new DetectedLocation(name!.Trim(), region?.Trim(), lat, lon, source)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetFirstNonEmpty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string FormatCoordinateCity(double lat, double lon) =>
        $"{lat:F2}°, {lon:F2}°";

    public static bool HasUsableCityName(string? city) =>
        !string.IsNullOrWhiteSpace(city) && !LooksLikeCoordinateLabel(city);

    public static bool LooksLikeCoordinateLabel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Replace("°", string.Empty)
            .Replace("º", string.Empty)
            .Replace("掳", string.Empty)
            .Trim();
        var parts = normalized.Split([',', '，'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
               double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) &&
               lat is >= -90 and <= 90 &&
               lon is >= -180 and <= 180;
    }

    public sealed record DetectedLocation(
        string City,
        string? Region,
        double Latitude,
        double Longitude,
        string Source = "ip",
        bool IpFallbackWarning = false);

    public static string DescribeSource(string? source) => source switch
    {
        "device" => "Windows 定位",
        "hybrid" => "Windows 定位 + IP 城市兜底",
        "ip" => "IP 定位（备用）",
        "manual" => "手动",
        _ => "自动定位"
    };
}
