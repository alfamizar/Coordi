using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.AstroSign;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class ZodiacSignEnumToLocalizedStringConverter : IValueConverter
    {
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BaseCelestialBodyCycle.ZodiacSigns.ElementAt((int)value - 1) + " " + GetLocalizedStringFromSignEnum((AstroZodiacSign)value);
        }

        private static string GetLocalizedStringFromSignEnum(object sign)
        {
            if (sign is AstroZodiacSign moonInZodiacSign)
            {
                switch (moonInZodiacSign)
                {
                    case AstroZodiacSign.Aries:
                        {
                            return _localizer.GetString("AriesLabel");
                        }
                    case AstroZodiacSign.Taurus:
                        {
                            return _localizer.GetString("TaurusLabel");
                        }
                    case AstroZodiacSign.Gemini:
                        {
                            return _localizer.GetString("GeminiLabel");
                        }
                    case AstroZodiacSign.Cancer:
                        {
                            return _localizer.GetString("CancerLabel");
                        }
                    case AstroZodiacSign.Leo:
                        {
                            return _localizer.GetString("LeoLabel");
                        }
                    case AstroZodiacSign.Virgo:
                        {
                            return _localizer.GetString("VirgoLabel");
                        }
                    case AstroZodiacSign.Libra:
                        {
                            return _localizer.GetString("LibraLabel");
                        }
                    case AstroZodiacSign.Scorpio:
                        {
                            return _localizer.GetString("ScorpioLabel");
                        }
                    case AstroZodiacSign.Sagittarius:
                        {
                            return _localizer.GetString("SagittariusLabel");
                        }
                    case AstroZodiacSign.Capricorn:
                        {
                            return _localizer.GetString("CapricornLabel");
                        }
                    case AstroZodiacSign.Aquarius:
                        {
                            return _localizer.GetString("AquariusLabel");
                        }
                    case AstroZodiacSign.Pisces:
                        {
                            return _localizer.GetString("PiscesLabel");
                        }
                }
                return null;
            }
            else
            {
                return $"Expected AstroZodiacSign type for {sign?.ToString()}";
            }
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
