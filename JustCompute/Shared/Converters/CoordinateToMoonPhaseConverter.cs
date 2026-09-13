using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;
using static Compute.Core.Domain.Entities.Models.BaseCelestialBodyCycle;

namespace JustCompute.Shared.Converters
{
    public class CoordinateToMoonPhaseConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;

            if (value is not CelestialSnapshot snapshot) return null;

            Hemisphere hemisphere = snapshot.Latitude > 0 ? Hemisphere.Northern : Hemisphere.Southern;
            MoonPhase moonPhase = snapshot.MoonPhase;

            string localizedMoonPhaseName = moonPhase switch
            {
                MoonPhase.NewMoon => _localizer.GetString("NewMoonLabel"),
                MoonPhase.WaxingCrescent => _localizer.GetString("WaxingCrescentLabel"),
                MoonPhase.FirstQuarter => _localizer.GetString("FirstQuarterLabel"),
                MoonPhase.WaxingGibbous => _localizer.GetString("WaxingGibbousLabel"),
                MoonPhase.FullMoon => _localizer.GetString("FullMoonLabel"),
                MoonPhase.WaningGibbous => _localizer.GetString("WaningGibbousLabel"),
                MoonPhase.LastQuarter => _localizer.GetString("LastQuarterLabel"),
                MoonPhase.WaningCrescent => _localizer.GetString("WaningCrescentLabel"),
                _ => throw new Exception($"MoonPhase {nameof(moonPhase)} does not exist!"),
            };

            if (hemisphere == Hemisphere.Northern)
            {
                return $"{MoonCycle.NorthernHemisphere.ElementAt((int)moonPhase)} {localizedMoonPhaseName}";
            }
            else
            {
                return $"{MoonCycle.SouthernHemisphere.ElementAt((int)moonPhase)} {localizedMoonPhaseName}";
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
