using System.Globalization;
using Compute.Astro;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// A latitude as degrees, minutes and seconds, e.g. <c>N 51° 30' 26.64"</c>.
    ///
    /// Formatting used to be a property on the Location entity itself, which put a display
    /// decision into the domain model. It is the view's business how a number is shown.
    /// </summary>
    public class LatitudeToDmsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double latitude ? GeoFormat.FormatDms(latitude, GeoFormat.Axis.Latitude) : string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>A longitude as degrees, minutes and seconds, e.g. <c>W 0° 7' 40.08"</c>.</summary>
    public class LongitudeToDmsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is double longitude ? GeoFormat.FormatDms(longitude, GeoFormat.Axis.Longitude) : string.Empty;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
