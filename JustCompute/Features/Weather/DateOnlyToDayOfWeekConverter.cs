using System.Globalization;

namespace JustCompute.Features.Weather;

public sealed class DateOnlyToDayOfWeekConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateOnly date
            ? culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek)
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
