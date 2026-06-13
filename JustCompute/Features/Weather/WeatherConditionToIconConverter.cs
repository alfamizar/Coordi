using System.Globalization;
using Compute.Core.Domain.Entities.Models.Weather;

namespace JustCompute.Features.Weather;

public sealed class WeatherConditionToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is WeatherCondition condition ? IconFor(condition) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    public static string IconFor(WeatherCondition? condition) => condition switch
    {
        WeatherCondition.Clear => "☀️",
        WeatherCondition.PartlyCloudy => "⛅",
        WeatherCondition.Cloudy => "☁️",
        WeatherCondition.Fog => "\U0001F32B️",
        WeatherCondition.Drizzle => "\U0001F326️",
        WeatherCondition.Rain => "\U0001F327️",
        WeatherCondition.Snow => "\U0001F328️",
        WeatherCondition.Showers => "\U0001F327️",
        WeatherCondition.Thunderstorm => "⛈️",
        _ => string.Empty,
    };
}
