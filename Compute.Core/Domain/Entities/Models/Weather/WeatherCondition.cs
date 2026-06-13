namespace Compute.Core.Domain.Entities.Models.Weather;

// Coarse buckets over the WMO 4677 weather-code space — the UI only needs the broad kind at a glance.
public enum WeatherCondition
{
    Unknown,
    Clear,
    PartlyCloudy,
    Cloudy,
    Fog,
    Drizzle,
    Rain,
    Snow,
    Showers,
    Thunderstorm
}
