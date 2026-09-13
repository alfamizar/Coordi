using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Eclipses;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Features.SunEclipses
{
    public class SunEclipseTypeToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // The whole row is bound, not just the type: an eclipse that misses this location is
            // still listed, and says so, rather than quietly showing a type nobody here will see.
            if (value is SolarEclipseInfo info)
            {
                if (!info.IsVisible)
                {
                    return _localizer.GetString("NotVisibleFromHereLabel");
                }

                // LocalType, not Type. Type is the geocentric classification — what the eclipse is
                // for the planet, decided by the darkest shadow that touches anyone. A reader in
                // Berlin looking at an eclipse whose umbra crosses Spain is owed "Partial", which
                // is what they will see; labelling that row "Total" promises them a totality that
                // happens two thousand kilometres away.
                value = info.LocalType;
            }

            if (value is SolarEclipseLocalType localType)
            {
                return localType switch
                {
                    SolarEclipseLocalType.Total => _localizer.GetString("TotalEclipseLabel"),
                    SolarEclipseLocalType.Annular => _localizer.GetString("AnnularEclipseLabel"),
                    SolarEclipseLocalType.Partial => _localizer.GetString("PartialEclipseLabel"),
                    // Visible but classified as nothing is a contradiction; say the honest half.
                    _ => _localizer.GetString("NotVisibleFromHereLabel"),
                };
            }

            return $"Expected SolarEclipseLocalType for {value?.ToString()}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
