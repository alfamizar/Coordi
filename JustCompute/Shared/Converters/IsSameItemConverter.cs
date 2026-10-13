using System.Globalization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// True when the two values are the same object: a row's item and the item the page has
    /// selected. Compared by reference, the way a CollectionView compares its SelectedItem.
    /// </summary>
    public class IsSameItemConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values is [{ } item, var selected] && ReferenceEquals(item, selected);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
