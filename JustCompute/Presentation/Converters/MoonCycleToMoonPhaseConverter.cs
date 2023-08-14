using Compute.Core.Domain.Entities.Models.Moon;
using CoordinateSharp;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;
using static Compute.Core.Domain.Entities.Models.BaseCelestialBodyCycle;

namespace JustCompute.Presentation.Converters
{
    public class MoonCycleToMoonPhaseConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            MoonCycle moonCycle = (MoonCycle)value;
            MoonPhase moonPhase = moonCycle.PhaseName;

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

            return $"{moonCycle.MoonPhaseUnicodeIcon} {localizedMoonPhaseName}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
