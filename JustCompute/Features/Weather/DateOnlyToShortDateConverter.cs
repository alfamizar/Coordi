using System.Globalization;
using JustCompute.Shared.Helpers;

namespace JustCompute.Features.Weather;

public sealed class DateOnlyToShortDateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            DateOnly date => ShortDateFormat.DayAndMonth(date, culture),
            // The Today banner holds its date as a DateTime; the label wanted is the same one.
            DateTime dateTime => ShortDateFormat.DayAndMonth(DateOnly.FromDateTime(dateTime), culture),
            _ => string.Empty,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
