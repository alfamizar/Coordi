using Compute.Core.Domain.Entities.Models.Moon;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class MoonNameEnumToStringConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MoonName moonName)
            {
                switch (moonName)
                {
                    case MoonName.Blue:
                        {
                            return _localizer.GetString("BlueMoonLabel");
                        }
                    case MoonName.Wolf:
                        {
                            return _localizer.GetString("WolfMoonLabel");
                        }
                    case MoonName.Snow:
                        {
                            return _localizer.GetString("SnowMoonLabel");
                        }
                    case MoonName.Worm:
                        {
                            return _localizer.GetString("WormMoonLabel");
                        }
                    case MoonName.Pink:
                        {
                            return _localizer.GetString("PinkMoonLabel");
                        }
                    case MoonName.Strawberry:
                        {
                            return _localizer.GetString("StrawberryMoonLabel");
                        }
                    case MoonName.Buck:
                        {
                            return _localizer.GetString("BuckMoonLabel");
                        }
                    case MoonName.Sturgeon:
                        {
                            return _localizer.GetString("SturgeonMoonLabel");
                        }
                    case MoonName.Corn:
                        {
                            return _localizer.GetString("CornMoonLabel");
                        }
                    case MoonName.Hunters:
                        {
                            return _localizer.GetString("HuntersMoonLabel");
                        }
                    case MoonName.Beaver:
                        {
                            return _localizer.GetString("BeaverMoonLabel");
                        }
                    case MoonName.Cold:
                        {
                            return _localizer.GetString("ColdMoonLabel");
                        }
                }
                return null;
            }
            else
            {
                return $"Expected MoonPhaseEnum type for {value?.ToString()}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
