using Compute.Core.Domain.Entities.Models.Weather;

namespace Compute.Core.Domain.Services.Weather;

public interface IWeatherService
{
    Task<WeatherForecast?> GetDailyForecastAsync(
        double latitude,
        double longitude,
        int days,
        CancellationToken cancellationToken = default);
}
