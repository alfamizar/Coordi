using System.Globalization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// An eclipse magnitude, to three decimals in the reader's own number format.
    ///
    /// Magnitude is the covered fraction of the body's <em>diameter</em>, not of its area, and it
    /// is conventionally written as a decimal (0.987) rather than a percentage — printing "99%"
    /// here would quietly claim the obscuration, which is a different and always smaller number.
    /// </summary>
    public class MagnitudeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double magnitude ? magnitude.ToString("0.000", CultureInfo.CurrentCulture) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
