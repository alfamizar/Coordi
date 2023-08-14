using System.Collections;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class IsCollectionNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable collection)
            {
                return collection is null || !collection.GetEnumerator().MoveNext();
            }
            else
            {
                throw new ArgumentException($"Expected IEnumerable value for {value?.ToString()}", nameof(value));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
