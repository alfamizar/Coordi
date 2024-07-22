using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class IsDateTimeSetConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ((DateTime?)value)?.Ticks != 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
