using Compute.Core.Extensions;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class EmptyStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string valueString && valueString.IsSet() ? value : "Feature is not supported";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
