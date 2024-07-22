using CoordinateSharp;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class MoonEclipseTypeToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LunarEclipseType lunarEclipseType)
            {
                switch (lunarEclipseType)
                {
                    case LunarEclipseType.Total:
                        {
                            return _localizer.GetString("TotalEclipseLabel");
                        }
                    case LunarEclipseType.Penumbral:
                        {
                            return _localizer.GetString("PenumbralEclipseLabel");
                        }
                    case LunarEclipseType.Partial:
                        {
                            return _localizer.GetString("PartialEclipseLabel");
                        }
                }
                return null;
            }
            else
            {
                return $"Expected LunarEclipseTypeEnum type for {value?.ToString()}";
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
