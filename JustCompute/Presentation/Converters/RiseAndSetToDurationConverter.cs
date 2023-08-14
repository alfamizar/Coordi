using Compute.Core.Domain.Entities.Models.CelestialBody;
using Compute.Core.Extensions;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class RiseAndSetToDurationConverter : IMultiValueConverter
    {
        public CelestialBody CelestialBody { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var RiseTime = (DateTime?)values[0];
            var SetTime = (DateTime?)values[1];

            if (SetTime != null && RiseTime != null)
            {
                return (SetTime - RiseTime).Value.Duration().StripMilliseconds();
            }

            return TimeSpan.Zero;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
