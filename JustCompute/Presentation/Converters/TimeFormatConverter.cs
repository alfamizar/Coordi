using JustCompute.Presentation.Helpers;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class TimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var is24HourTimeFormat = Settings.Is24HourTimeFormat;

                if (is24HourTimeFormat)
                {
                    return dateTime.ToString("H:mm:ss");
                }
                else
                {
                    return dateTime.ToString("h:mm:ss tt");
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
