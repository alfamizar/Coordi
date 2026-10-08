using System.Globalization;
using JustCompute.Shared.Design;
using JustCompute.Shared.Helpers;
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

    // On the type scale like every other screen: Micro is the step made for a column this narrow,
    // a seventh of the card; the icon and today's temperature take the Display and Body steps the
    // rest of the app reads at.
    private readonly Label _dayOfWeek = Line("FontSizeMicro", FontAttributes.Bold);

    private readonly Label _date = Line("FontSizeMicro");

    private readonly Label _icon = Line("FontSizeDisplay");

    private readonly Label _currentTemp = Line("FontSizeBody", FontAttributes.Bold);

    private readonly Label _range = Line("FontSizeMicro");

    public WeatherDayCell()
    {
        Content = new VerticalStackLayout
        {
            Spacing = DesignTokens.Spacing(Space.Micro),
            // Nothing at the sides: a date like "10 Oct" needs all of the column's seventh.
            Padding = new Thickness(0, DesignTokens.Spacing(Space.Small)),
            HorizontalOptions = LayoutOptions.Fill,
            Children = { _dayOfWeek, _date, _icon, _currentTemp, _range },
        };
    }

    public DailyForecast? Day
    {
        get => (DailyForecast?)GetValue(DayProperty);
        set => SetValue(DayProperty, value);
    }

    private static Label Line(string fontSizeKey, FontAttributes attributes = FontAttributes.None)
    {
        var label = new Label
        {
            FontSize = DesignTokens.Size(fontSizeKey),
            FontAttributes = attributes,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        // The strip is painted in the secondary colour, so its text is the colour made to go on
        // that: the page's text colour was white on Midnight's amber, at 1.9:1.
        label.SetDynamicResource(Label.TextColorProperty, "ThemeOnSecondary");
        return label;
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
        _date.Text = ShortDateFormat.DayAndMonth(day.Date, culture);
        _icon.Text = WeatherConditionToIconConverter.IconFor(day.Condition);
        _currentTemp.Text = day.CurrentTemperature is { } t
            ? $"{Math.Round(t):0}°"
            : string.Empty;
        _range.Text = $"{Math.Round(day.MinTemperature):0}° / {Math.Round(day.MaxTemperature):0}°";
    }
}
