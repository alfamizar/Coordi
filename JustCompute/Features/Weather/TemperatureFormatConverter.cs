using System.Globalization;

namespace JustCompute.Features.Weather;

public sealed class TemperatureFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            double d => Format(d, culture),
            float f => Format(f, culture),
            int i => $"{i}°",
            _ => string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static string Format(double value, CultureInfo culture)
        => $"{Math.Round(value).ToString("0", culture)}°";
}
