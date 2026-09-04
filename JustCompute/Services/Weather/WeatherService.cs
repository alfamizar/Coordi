using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Compute.Core.Domain.Entities.Models.Weather;
using Compute.Core.Domain.Services.Weather;

namespace JustCompute.Services.Weather;

/// <summary>
/// Open-Meteo backed forecast. Lives in the app rather than in Compute.Core: talking HTTP to a
/// named third-party endpoint is infrastructure, and the domain library should not have to know
/// that a forecast arrives over a network at all. Compute.Core keeps the contract
/// (<see cref="IWeatherService"/>) and the shapes it returns.
/// </summary>
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
        catch (OperationCanceledException)
        {
            // A superseded or abandoned request is not a failure — let the caller tell the two
            // apart instead of reporting "weather unavailable" for a load nobody is waiting on.
            throw;
        }
        catch (Exception)
        {
            return null;
        }

        return BuildForecast(payload, days);
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

    private static WeatherForecast? BuildForecast(OpenMeteoResponse? payload, int days)
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

        // All of the requested days, or none of them.
        //
        // The strip that draws this is a fixed row of columns reading Days[0] through Days[6]; a
        // short forecast — a truncated payload, or a date that failed to parse — would send those
        // bindings past the end of the list. Reporting the forecast as unavailable is honest and
        // already has a retry; rendering a partial one is not something this layout can do.
        return entries.Count < days ? null : new WeatherForecast(unit, entries);
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
