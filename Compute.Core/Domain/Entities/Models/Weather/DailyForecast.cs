namespace Compute.Core.Domain.Entities.Models.Weather;

public sealed record DailyForecast(
    DateOnly Date,
    WeatherCondition Condition,
    double MinTemperature,
    double MaxTemperature,
    double? CurrentTemperature,
    /// <summary>Mean cloud cover for the day, 0-100, or null when the source omits it.</summary>
    int? CloudCoverPercent = null);
