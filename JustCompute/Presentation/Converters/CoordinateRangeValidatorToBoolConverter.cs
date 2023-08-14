using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class CoordinateRangeValidatorToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                // Check if the value is within a valid range
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
