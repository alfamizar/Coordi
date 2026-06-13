using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Compute.Core.Domain.Entities.Models.Weather;

namespace Compute.Core.Domain.Services.Weather;

public sealed class WeatherService(HttpClient httpClient) : IWeatherService
{
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public async Task<WeatherForecast?> GetDailyForecastAsync(
        double latitude,
        double longitude,
        int days,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            return null;
        }

        var url = BuildUrl(latitude, longitude, days);

        OpenMeteoResponse? payload;
        try
        {
            payload = await httpClient
                .GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }

        return BuildForecast(payload);
    }

    private static string BuildUrl(double latitude, double longitude, int days)
    {
        var lat = latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("0.######", CultureInfo.InvariantCulture);

        return $"{BaseUrl}?latitude={lat}&longitude={lon}"
             + "&current=temperature_2m,weather_code"
             + "&daily=weather_code,temperature_2m_max,temperature_2m_min"
             + $"&forecast_days={days}"
             + "&timezone=auto";
    }

    private static WeatherForecast? BuildForecast(OpenMeteoResponse? payload)
    {
        var daily = payload?.Daily;
        if (daily?.Time is null || daily.WeatherCode is null
            || daily.MaxTemperature is null || daily.MinTemperature is null)
        {
            return null;
        }

        var count = new[]
        {
            daily.Time.Count,
            daily.WeatherCode.Count,
            daily.MaxTemperature.Count,
            daily.MinTemperature.Count
        }.Min();

        if (count == 0)
        {
            return null;
        }

        var today = payload!.Current?.Time is { } currentTime
            ? DateOnly.FromDateTime(currentTime)
            : (DateOnly?)null;

        var unit = payload.DailyUnits?.MaxTemperature ?? "°C";
        var entries = new List<DailyForecast>(count);

        for (var i = 0; i < count; i++)
        {
            if (!DateOnly.TryParse(daily.Time[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            var currentTemp = today is { } t && t == date ? payload.Current?.Temperature : null;

            entries.Add(new DailyForecast(
                date,
                WmoWeatherCodeMapper.Map(daily.WeatherCode[i]),
                daily.MinTemperature[i],
                daily.MaxTemperature[i],
                currentTemp));
        }

        return entries.Count == 0 ? null : new WeatherForecast(unit, entries);
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")] public CurrentBlock? Current { get; init; }
        [JsonPropertyName("daily")] public DailyBlock? Daily { get; init; }
        [JsonPropertyName("daily_units")] public DailyUnitsBlock? DailyUnits { get; init; }
    }

    private sealed class CurrentBlock
    {
        [JsonPropertyName("time")] public DateTime? Time { get; init; }
        [JsonPropertyName("temperature_2m")] public double? Temperature { get; init; }
    }

    private sealed class DailyBlock
    {
        [JsonPropertyName("time")] public IReadOnlyList<string>? Time { get; init; }
        [JsonPropertyName("weather_code")] public IReadOnlyList<int>? WeatherCode { get; init; }
        [JsonPropertyName("temperature_2m_max")] public IReadOnlyList<double>? MaxTemperature { get; init; }
        [JsonPropertyName("temperature_2m_min")] public IReadOnlyList<double>? MinTemperature { get; init; }
    }

    private sealed class DailyUnitsBlock
    {
        [JsonPropertyName("temperature_2m_max")] public string? MaxTemperature { get; init; }
    }
}
