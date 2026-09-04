using System.Globalization;
using JustCompute.Shared.Helpers;

namespace JustCompute.Features.Weather;

public sealed class DateOnlyToShortDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateOnly date ? ShortDateFormat.DayAndMonth(date, culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
