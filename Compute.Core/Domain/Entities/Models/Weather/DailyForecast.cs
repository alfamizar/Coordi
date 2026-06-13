namespace Compute.Core.Domain.Entities.Models.Weather;

public sealed record DailyForecast(
    DateOnly Date,
    WeatherCondition Condition,
    double MinTemperature,
    double MaxTemperature,
    double? CurrentTemperature);
