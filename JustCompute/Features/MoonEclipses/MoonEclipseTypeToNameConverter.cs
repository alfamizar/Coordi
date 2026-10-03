using JustCompute.Shared.Helpers;
using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Eclipses;
using System.Globalization;

namespace JustCompute.Features.MoonEclipses
{
    public class MoonEclipseTypeToNameConverter : IValueConverter
    {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // The whole row is bound, not just the type: an eclipse that misses this location is
            // still listed, and says so, rather than quietly showing a type nobody here will see.
            if (value is LunarEclipseInfo info)
            {
                if (!info.IsVisible)
                {
                    return Strings.Get("NotVisibleFromHereLabel");
                }

                value = info.Type;
            }

            if (value is LunarEclipseType lunarEclipseType)
            {
                switch (lunarEclipseType)
                {
                    case LunarEclipseType.Total:
                        {
                            return Strings.Get("TotalEclipseLabel");
                        }
                    case LunarEclipseType.Penumbral:
                        {
                            return Strings.Get("PenumbralEclipseLabel");
                        }
                    case LunarEclipseType.Partial:
                        {
                            return Strings.Get("PartialEclipseLabel");
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
