using Compute.Core.Domain.Entities.Models.Weather;

namespace Compute.Core.Domain.Services.Weather;

// Maps WMO 4677 numeric codes (as returned by Open-Meteo) to WeatherCondition buckets.
internal static class WmoWeatherCodeMapper
{
    public static WeatherCondition Map(int wmoCode) => wmoCode switch
    {
        0 => WeatherCondition.Clear,
        1 or 2 => WeatherCondition.PartlyCloudy,
        3 => WeatherCondition.Cloudy,
        45 or 48 => WeatherCondition.Fog,
        51 or 53 or 55 or 56 or 57 => WeatherCondition.Drizzle,
        61 or 63 or 65 or 66 or 67 => WeatherCondition.Rain,
        71 or 73 or 75 or 77 => WeatherCondition.Snow,
        80 or 81 or 82 or 85 or 86 => WeatherCondition.Showers,
        95 or 96 or 99 => WeatherCondition.Thunderstorm,
        _ => WeatherCondition.Unknown,
    };
}
