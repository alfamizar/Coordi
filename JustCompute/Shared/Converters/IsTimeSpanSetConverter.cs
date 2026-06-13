using System.Globalization;

namespace JustCompute.Shared.Converters
{
    public class IsTimeSpanSetConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ((TimeSpan?)value)?.Ticks != 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
