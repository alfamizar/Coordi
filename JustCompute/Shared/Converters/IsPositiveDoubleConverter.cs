using System.Globalization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// True for a value above zero. Eclipse magnitudes use zero for "this phase does not occur" —
    /// a penumbral lunar eclipse has no umbral magnitude — the same way the contact times use
    /// <see langword="default"/> dates, so the rows that report them hide on the same principle.
    /// </summary>
    public class IsPositiveDoubleConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double number && number > 0.0;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
