using Compute.Core.Extensions;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class EmptyStringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value as string;
            return text.IsSet();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
