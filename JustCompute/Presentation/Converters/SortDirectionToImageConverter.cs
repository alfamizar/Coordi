using Compute.Core.Common.Sort;
using JustCompute.Presentation.Models;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class SortDirectionToImageConverter : IValueConverter
    {
        public ImageSource? AscendingImage { get; set; }
        public ImageSource? DescendingImage { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SelectableSortingCriterion sortingCriterion || !sortingCriterion.IsSelected)
            {
                return null;
            }

            return sortingCriterion.SortingCriterion.Direction switch
            {
                SortDirection.Ascending => AscendingImage,
                SortDirection.Descending => DescendingImage,
                _ => AscendingImage,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
