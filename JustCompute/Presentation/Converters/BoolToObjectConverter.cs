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
                return valueBool ? TrueObject : FalseObject;
            }

            throw new ArgumentException($"Expected boolean value but received {value?.GetType().Name ?? "null"}", nameof(value));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
