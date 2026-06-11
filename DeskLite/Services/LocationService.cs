using System.Net.Http;
using System.Text.Json;
using Windows.Devices.Geolocation;

namespace DeskLite.Services;

public sealed class LocationService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Windows device location first, then IP geolocation as fallback.</summary>
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

    /// <summary>Try Windows Geolocation API only (Wi‑Fi / GPS, not VPN IP).</summary>
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
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var region = root.TryGetProperty("region", out var regionEl) ? regionEl.GetString() : null;
            var lat = root.GetProperty("latitude").GetDouble();
            var lon = root.GetProperty("longitude").GetDouble();
            return new DetectedLocation(city.Trim(), region?.Trim(), lat, lon, "ip");
        }
        catch
        {
            return null;
        }
    }

    private static async Task<DetectedLocation?> TryDetectByDeviceAsync(CancellationToken ct)
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

            return await ReverseGeocodeAsync(lat, lon, "device", ct);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<DetectedLocation?> ReverseGeocodeAsync(
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
            var name = first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            var region = first.TryGetProperty("admin1", out var adminEl) ? adminEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = FormatCoordinateCity(lat, lon);
            }

            return new DetectedLocation(name.Trim(), region?.Trim(), lat, lon, source);
        }
        catch
        {
            return new DetectedLocation(FormatCoordinateCity(lat, lon), null, lat, lon, source);
        }
    }

    private static string FormatCoordinateCity(double lat, double lon) =>
        $"{lat:F2}°, {lon:F2}°";

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
        "ip" => "IP 定位（备用）",
        "manual" => "手动",
        _ => "自动定位"
    };
}
