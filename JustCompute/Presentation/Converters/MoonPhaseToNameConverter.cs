using Compute.Core.Domain.Entities.Models.Moon;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class MoonPhaseToNameConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MoonPhase moonPhase)
            {
                return moonPhase switch
                {
                    MoonPhase.NewMoon => _localizer.GetString("NewMoonLabel"),
                    MoonPhase.WaxingCrescent => _localizer.GetString("WaxingCrescentLabel"),
                    MoonPhase.FirstQuarter => _localizer.GetString("FirstQuarterLabel"),
                    MoonPhase.WaxingGibbous => _localizer.GetString("WaxingGibbousLabel"),
                    MoonPhase.FullMoon => _localizer.GetString("FullMoonLabel"),
                    MoonPhase.WaningGibbous => _localizer.GetString("WaningGibbousLabel"),
                    MoonPhase.LastQuarter => _localizer.GetString("LastQuarterLabel"),
                    MoonPhase.WaningCrescent => _localizer.GetString("WaningCrescentLabel"),
                    _ => null
                };
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
