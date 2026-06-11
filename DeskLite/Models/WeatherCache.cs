namespace DeskLite.Models;

public sealed class WeatherCache
{
    public string City { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? LocationSource { get; set; }
    public int Temperature { get; set; }
    public int TempMin { get; set; }
    public int TempMax { get; set; }
    public int? FeelsLike { get; set; }
    public int WeatherCode { get; set; }
    public bool IsDay { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public string IconSlug { get; set; } = "partly-cloudy-day";
    public string? Sunrise { get; set; }
    public string? Sunset { get; set; }
    public int? TomorrowMin { get; set; }
    public int? TomorrowMax { get; set; }
    public int? TomorrowWeatherCode { get; set; }
    public string? TomorrowDescription { get; set; }
    public string? TomorrowIconSlug { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Legacy emoji icon from older caches; ignored when IconSlug is set.</summary>
    public string? Icon { get; set; }

    /// <summary>Legacy emoji icon for tomorrow forecast.</summary>
    public string? TomorrowIcon { get; set; }
}
