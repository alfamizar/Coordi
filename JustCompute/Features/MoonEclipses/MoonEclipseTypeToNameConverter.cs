using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Eclipses;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Features.MoonEclipses
{
    public class MoonEclipseTypeToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // The whole row is bound, not just the type: an eclipse that misses this location is
            // still listed, and says so, rather than quietly showing a type nobody here will see.
            if (value is LunarEclipseInfo info)
            {
                if (!info.IsVisible)
                {
                    return _localizer.GetString("NotVisibleFromHereLabel");
                }

                value = info.Type;
            }

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
