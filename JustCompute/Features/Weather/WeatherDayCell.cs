using System.Globalization;
using Compute.Core.Domain.Entities.Models.Weather;

namespace JustCompute.Features.Weather;

public sealed class WeatherDayCell : ContentView
{
    public static readonly BindableProperty DayProperty = BindableProperty.Create(
        nameof(Day),
        typeof(DailyForecast),
        typeof(WeatherDayCell),
        defaultValue: null,
        propertyChanged: static (b, _, _) => ((WeatherDayCell)b).Refresh());

    private readonly Label _dayOfWeek = new()
    {
        FontSize = 11,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    private readonly Label _date = new()
    {
        FontSize = 10,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    private readonly Label _icon = new()
    {
        FontSize = 20,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    private readonly Label _currentTemp = new()
    {
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    private readonly Label _range = new()
    {
        FontSize = 11,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    public WeatherDayCell()
    {
        Content = new VerticalStackLayout
        {
            Spacing = 2,
            Padding = new Thickness(2, 6),
            HorizontalOptions = LayoutOptions.Fill,
            Children = { _dayOfWeek, _date, _icon, _currentTemp, _range },
        };
    }

    public DailyForecast? Day
    {
        get => (DailyForecast?)GetValue(DayProperty);
        set => SetValue(DayProperty, value);
    }

    private void Refresh()
    {
        var day = Day;

        if (day is null)
        {
            _dayOfWeek.Text = string.Empty;
            _date.Text = string.Empty;
            _icon.Text = string.Empty;
            _currentTemp.Text = string.Empty;
            _range.Text = string.Empty;
            return;
        }

        var culture = CultureInfo.CurrentCulture;

        _dayOfWeek.Text = culture.DateTimeFormat.GetAbbreviatedDayName(day.Date.DayOfWeek);
        _date.Text = day.Date.ToString("d MMM", culture);
        _icon.Text = WeatherConditionToIconConverter.IconFor(day.Condition);
        _currentTemp.Text = day.CurrentTemperature is { } t
            ? $"{Math.Round(t):0}°"
            : string.Empty;
        _range.Text = $"{Math.Round(day.MinTemperature):0}° / {Math.Round(day.MaxTemperature):0}°";
    }
}
