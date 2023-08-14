using CoordinateSharp;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class SunEclipseTypeToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolarEclipseType solarEclipseType)
            {
                switch (solarEclipseType)
                {
                    case SolarEclipseType.Total:
                        {
                            return _localizer.GetString("TotalEclipseLabel");
                        }
                    case SolarEclipseType.Annular:
                        {
                            return _localizer.GetString("AnnularEclipseLabel");
                        }
                    case SolarEclipseType.Partial:
                        {
                            return _localizer.GetString("PartialEclipseLabel");
                        }
                }
                return null;
            }
            else
            {
                return $"Expected SolarEclipseTypeEnum type for {value?.ToString()}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
