using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Windows.Devices.Geolocation;

namespace DeskLite.Services;

public sealed class LocationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private const string VisitorWeatherUrl = "https://lt20czhfj3.hzh.sealos.run/macos_web_fangke";

    static LocationService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Floatly/2.0");
    }

    /// <summary>Prefer Windows location, then a server-side visitor weather locator, then public IP locators.</summary>
    public async Task<DetectedLocation?> DetectAsync(CancellationToken ct = default)
    {
        var device = await TryDetectByWindowsAsync(ct);
        if (device is not null)
        {
            return device;
        }

        var visitor = await DetectByVisitorWeatherAsync(ct);
        if (visitor is not null)
        {
            return visitor;
        }

        var ip = await DetectByIpAsync(ct);
        return ip is null ? null : ip with { IpFallbackWarning = true };
    }

    /// <summary>Try Windows Geolocation API only, but still enrich the city name through reverse-geocoding fallbacks.</summary>
    public Task<DetectedLocation?> TryDetectByWindowsAsync(CancellationToken ct = default) =>
        TryDetectByDeviceAsync(ct);

    public async Task<DetectedLocation?> DetectByIpAsync(CancellationToken ct = default)
    {
        var ipApi = await DetectByIpApiAsync(ct);
        if (ipApi is not null)
        {
            return ipApi;
        }

        return await DetectByIpWhoAsync(ct);
    }

    private static async Task<DetectedLocation?> DetectByVisitorWeatherAsync(CancellationToken ct)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                source = "floatly_desktop",
                page = "settings"
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = $"{VisitorWeatherUrl}?timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            using var response = await Http.PostAsync(url, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean() ||
                !root.TryGetProperty("weather", out var weather))
            {
                return null;
            }

            var city = GetFirstNonEmpty(weather, "city");
            if (!HasUsableCityName(city))
            {
                return null;
            }

            var district = GetFirstNonEmpty(weather, "district");
            var province = GetFirstNonEmpty(weather, "province");
            var region = HasUsableCityName(district) && !SamePlace(city, district)
                ? district
                : province;
            var geo = await GeocodeCityAsync(city!, province, ct);
            if (geo is null)
            {
                return null;
            }

            return new DetectedLocation(city!.Trim(), region?.Trim(), geo.Latitude, geo.Longitude, "visitor", true);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<DetectedLocation?> DetectByIpApiAsync(CancellationToken ct)
    {
        try
        {
            var url = "http://ip-api.com/json/?lang=zh-CN&fields=status,message,country,regionName,city,lat,lon";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("status", out var status) ||
                !string.Equals(status.GetString(), "success", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var city = GetFirstNonEmpty(root, "city");
            if (!HasUsableCityName(city) || !TryReadCoordinate(root, "lat", "lon", out var lat, out var lon))
            {
                return null;
            }

            var region = GetFirstNonEmpty(root, "regionName", "country");
            return new DetectedLocation(city!.Trim(), region?.Trim(), lat, lon, "ip");
        }
        catch
        {
            return null;
        }
    }

    private static async Task<DetectedLocation?> DetectByIpWhoAsync(CancellationToken ct)
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

            var city = GetFirstNonEmpty(root, "city");
            if (!HasUsableCityName(city) || !TryReadCoordinate(root, "latitude", "longitude", out var lat, out var lon))
            {
                return null;
            }

            var region = GetFirstNonEmpty(root, "region", "country");
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
                maximumAge: TimeSpan.FromMinutes(5),
                timeout: TimeSpan.FromSeconds(15));

            var lat = position.Coordinate.Latitude;
            var lon = position.Coordinate.Longitude;
            if (!ValidCoordinate(lat, lon))
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
        var nominatim = await ReverseGeocodeByNominatimAsync(lat, lon, "device", ct);
        if (HasUsableCityName(nominatim?.City))
        {
            return nominatim;
        }

        var openMeteo = await ReverseGeocodeByOpenMeteoAsync(lat, lon, "device", ct);
        if (HasUsableCityName(openMeteo?.City))
        {
            return openMeteo;
        }

        var visitor = await DetectByVisitorWeatherAsync(ct);
        if (visitor is not null)
        {
            return visitor with { Source = "hybrid", IpFallbackWarning = true };
        }

        return new DetectedLocation(FormatCoordinateCity(lat, lon), null, lat, lon, "device");
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
                $"https://geocoding-api.open-meteo.com/v1/reverse?latitude={lat.ToString("F4", CultureInfo.InvariantCulture)}" +
                $"&longitude={lon.ToString("F4", CultureInfo.InvariantCulture)}&language=zh";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            var first = results[0];
            var name = GetFirstNonEmpty(first, "name", "city", "locality", "admin3", "admin2", "admin1");
            var region = GetFirstNonEmpty(first, "admin1", "admin2", "country");
            return HasUsableCityName(name)
                ? new DetectedLocation(name!.Trim(), region?.Trim(), lat, lon, source)
                : null;
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

    private static async Task<GeoPoint?> GeocodeCityAsync(string city, string? region, CancellationToken ct)
    {
        foreach (var query in BuildCityQueries(city, region))
        {
            var openMeteo = await GeocodeCityByOpenMeteoAsync(query, ct);
            if (openMeteo is not null)
            {
                return openMeteo;
            }

            var nominatim = await GeocodeCityByNominatimAsync(query, ct);
            if (nominatim is not null)
            {
                return nominatim;
            }
        }

        return null;
    }

    private static async Task<GeoPoint?> GeocodeCityByOpenMeteoAsync(string query, CancellationToken ct)
    {
        try
        {
            var url =
                $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=5&language=zh&format=json";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            var first = results.EnumerateArray().FirstOrDefault(r =>
                string.Equals(GetFirstNonEmpty(r, "country_code"), "CN", StringComparison.OrdinalIgnoreCase));
            if (first.ValueKind == JsonValueKind.Undefined)
            {
                first = results[0];
            }

            return TryReadCoordinate(first, "latitude", "longitude", out var lat, out var lon)
                ? new GeoPoint(lat, lon)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<GeoPoint?> GeocodeCityByNominatimAsync(string query, CancellationToken ct)
    {
        try
        {
            var url =
                $"https://nominatim.openstreetmap.org/search?format=jsonv2&q={Uri.EscapeDataString(query)}" +
                "&limit=1&accept-language=zh-CN";
            var json = await Http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return null;
            }

            var first = root[0];
            return TryReadCoordinate(first, "lat", "lon", out var lat, out var lon)
                ? new GeoPoint(lat, lon)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> BuildCityQueries(string city, string? region)
    {
        var trimmedCity = city.Trim();
        var simpleCity = SimplifyChinesePlaceName(trimmedCity);
        var trimmedRegion = region?.Trim();

        yield return trimmedCity;
        if (!string.Equals(simpleCity, trimmedCity, StringComparison.Ordinal))
        {
            yield return simpleCity;
        }

        if (!string.IsNullOrWhiteSpace(trimmedRegion))
        {
            yield return $"{trimmedCity}, {trimmedRegion}";
            if (!string.Equals(simpleCity, trimmedCity, StringComparison.Ordinal))
            {
                yield return $"{simpleCity}, {trimmedRegion}";
            }
        }
    }

    private static string SimplifyChinesePlaceName(string value)
    {
        var result = value.Trim();
        foreach (var suffix in new[] { "特别行政区", "自治州", "地区", "盟", "市", "区", "县" })
        {
            if (result.EndsWith(suffix, StringComparison.Ordinal) && result.Length > suffix.Length)
            {
                return result[..^suffix.Length];
            }
        }

        return result;
    }

    private static string? GetFirstNonEmpty(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            var text = value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static bool TryReadCoordinate(JsonElement element, string latName, string lonName, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;
        if (!element.TryGetProperty(latName, out var latEl) || !element.TryGetProperty(lonName, out var lonEl))
        {
            return false;
        }

        lat = ReadDouble(latEl);
        lon = ReadDouble(lonEl);
        return ValidCoordinate(lat, lon);
    }

    private static double ReadDouble(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : double.NaN;

    private static bool ValidCoordinate(double lat, double lon) =>
        !double.IsNaN(lat) &&
        !double.IsNaN(lon) &&
        lat is >= -90 and <= 90 &&
        lon is >= -180 and <= 180;

    private static bool SamePlace(string? left, string? right)
    {
        var a = SimplifyChinesePlaceName(left ?? string.Empty);
        var b = SimplifyChinesePlaceName(right ?? string.Empty);
        return !string.IsNullOrWhiteSpace(a) &&
               string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatCoordinateCity(double lat, double lon) =>
        $"{lat.ToString("F2", CultureInfo.InvariantCulture)}°, {lon.ToString("F2", CultureInfo.InvariantCulture)}°";

    public static bool HasUsableCityName(string? city) =>
        !string.IsNullOrWhiteSpace(city) && !LooksLikeCoordinateLabel(city);

    public static bool LooksLikeCoordinateLabel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Replace("°", string.Empty).Trim();
        var parts = normalized.Split([',', '，'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
               double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) &&
               ValidCoordinate(lat, lon);
    }

    public sealed record DetectedLocation(
        string City,
        string? Region,
        double Latitude,
        double Longitude,
        string Source = "ip",
        bool IpFallbackWarning = false);

    private sealed record GeoPoint(double Latitude, double Longitude);

    public static string DescribeSource(string? source) => source switch
    {
        "device" => "Windows 定位",
        "hybrid" => "Windows 定位 + 服务端兜底",
        "visitor" => "服务端天气定位",
        "ip" => "IP 定位（备用）",
        "manual" => "手动",
        _ => "自动定位"
    };
}
