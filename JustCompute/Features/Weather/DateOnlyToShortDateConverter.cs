using System.Globalization;

namespace JustCompute.Features.Weather;

public sealed class DateOnlyToShortDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateOnly date ? date.ToString("d MMM", culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
