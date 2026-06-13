using System.Globalization;

namespace JustCompute.Shared.Converters
{
    public class IsIntEqualZeroConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (int?)value == 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
