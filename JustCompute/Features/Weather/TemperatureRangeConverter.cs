using System.Globalization;

namespace JustCompute.Features.Weather;

public sealed class TemperatureRangeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
        {
            return string.Empty;
        }

        var min = ToTemperatureString(values[0], culture);
        var max = ToTemperatureString(values[1], culture);

        if (min.Length == 0 || max.Length == 0)
        {
            return string.Empty;
        }

        return $"{min} / {max}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static string ToTemperatureString(object? value, CultureInfo culture) => value switch
    {
        double d => $"{Math.Round(d).ToString("0", culture)}°",
        float f => $"{Math.Round(f).ToString("0", culture)}°",
        int i => $"{i}°",
        _ => string.Empty,
    };
}
