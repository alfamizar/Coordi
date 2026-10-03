using JustCompute.Shared.Helpers;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Domain.Entities.Models;
using System.Globalization;
using static Compute.Core.Domain.Entities.Models.BaseCelestialBodyCycle;
using Compute.Core.Domain.ReadModels;

namespace JustCompute.Shared.Converters
{
    public class CoordinateToMoonPhaseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return null;

            if (value is not CelestialSnapshot snapshot) return null;

            Hemisphere hemisphere = snapshot.Latitude > 0 ? Hemisphere.Northern : Hemisphere.Southern;
            MoonPhase moonPhase = snapshot.MoonPhase;

            string localizedMoonPhaseName = moonPhase switch
            {
                MoonPhase.NewMoon => Strings.Get("NewMoonLabel"),
                MoonPhase.WaxingCrescent => Strings.Get("WaxingCrescentLabel"),
                MoonPhase.FirstQuarter => Strings.Get("FirstQuarterLabel"),
                MoonPhase.WaxingGibbous => Strings.Get("WaxingGibbousLabel"),
                MoonPhase.FullMoon => Strings.Get("FullMoonLabel"),
                MoonPhase.WaningGibbous => Strings.Get("WaningGibbousLabel"),
                MoonPhase.LastQuarter => Strings.Get("LastQuarterLabel"),
                MoonPhase.WaningCrescent => Strings.Get("WaningCrescentLabel"),
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
