using System.Globalization;

namespace JustCompute.Shared.Converters
{
    /// <summary>
    /// Validates a typed latitude or longitude against its legal range. Lives here rather than in
    /// the Coordinates Converter feature it was written for: that feature was an unreachable stub
    /// and has been removed, but PointEntry still relies on this.
    /// </summary>
    public class CoordinateRangeValidatorToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString()?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                if (parameter != null && parameter.ToString() == "latitude")
                {
                    if (doubleValue >= -90 && doubleValue <= 90)
                    {
                        return true;
                    }
                }
                else if (parameter != null && parameter.ToString() == "longitude")
                {
                    if (doubleValue >= -180 && doubleValue <= 180)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
