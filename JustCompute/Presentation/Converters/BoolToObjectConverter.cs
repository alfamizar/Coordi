using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class BoolToObjectConverter<T> : IValueConverter
    {
        public T? TrueObject { set; get; }

        public T? FalseObject { set; get; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool valueBool)
            {
                return valueBool switch
                {
                    true => TrueObject,
                    false => FalseObject
                };
            }
            else
            {
                throw new ArgumentException($"Expected boolean value for {value?.ToString()}", nameof(value));
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
