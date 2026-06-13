namespace Compute.Core.Domain.Entities.Models.Weather;

public sealed record WeatherForecast(string TemperatureUnit, IReadOnlyList<DailyForecast> Days);
