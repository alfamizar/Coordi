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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            MoonPhase moonPhase = (MoonPhase)value;

            switch (moonPhase)
            {
                case MoonPhase.NewMoon:
                    {
                        return _localizer.GetString("NewMoonLabel");
                    }
                case MoonPhase.WaxingCrescent:
                    {
                        return _localizer.GetString("WaxingCrescentLabel");
                    }
                case MoonPhase.FirstQuarter:
                    {
                        return _localizer.GetString("FirstQuarterLabel");
                    }
                case MoonPhase.WaxingGibbous:
                    {
                        return _localizer.GetString("WaxingGibbousLabel");
                    }
                case MoonPhase.FullMoon:
                    {
                        return _localizer.GetString("FullMoonLabel");
                    }
                case MoonPhase.WaningGibbous:
                    {
                        return _localizer.GetString("WaningGibbousLabel");
                    }
                case MoonPhase.LastQuarter:
                    {
                        return _localizer.GetString("LastQuarterLabel");
                    }
                case MoonPhase.WaningCrescent:
                    {
                        return _localizer.GetString("WaningCrescentLabel");
                    }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
