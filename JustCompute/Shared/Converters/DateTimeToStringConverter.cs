using System.Globalization;

namespace JustCompute.Shared.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ((DateTime?)value).ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
