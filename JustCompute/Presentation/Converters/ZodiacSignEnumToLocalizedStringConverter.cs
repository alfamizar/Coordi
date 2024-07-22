using Compute.Core.Domain.Entities.Models;
using CoordinateSharp;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class ZodiacSignEnumToLocalizedStringConverter : IValueConverter
    {
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            AstrologicalSignType? AstrologicalSignType = (AstrologicalSignType?)value;
            return $"{BaseCelestialBodyCycle.ZodiacSigns.ElementAt((int?)value - 1 ?? 0)} {GetLocalizedStringFromSignEnum((AstrologicalSignType))}";
        }

        private static string? GetLocalizedStringFromSignEnum(object? sign)
        {
            if (sign is AstrologicalSignType moonInZodiacSign)
            {
                switch (moonInZodiacSign)
                {
                    case AstrologicalSignType.Aries:
                        {
                            return _localizer.GetString("AriesLabel");
                        }
                    case AstrologicalSignType.Taurus:
                        {
                            return _localizer.GetString("TaurusLabel");
                        }
                    case AstrologicalSignType.Gemini:
                        {
                            return _localizer.GetString("GeminiLabel");
                        }
                    case AstrologicalSignType.Cancer:
                        {
                            return _localizer.GetString("CancerLabel");
                        }
                    case AstrologicalSignType.Leo:
                        {
                            return _localizer.GetString("LeoLabel");
                        }
                    case AstrologicalSignType.Virgo:
                        {
                            return _localizer.GetString("VirgoLabel");
                        }
                    case AstrologicalSignType.Libra:
                        {
                            return _localizer.GetString("LibraLabel");
                        }
                    case AstrologicalSignType.Scorpio:
                        {
                            return _localizer.GetString("ScorpioLabel");
                        }
                    case AstrologicalSignType.Sagittarius:
                        {
                            return _localizer.GetString("SagittariusLabel");
                        }
                    case AstrologicalSignType.Capricorn:
                        {
                            return _localizer.GetString("CapricornLabel");
                        }
                    case AstrologicalSignType.Aquarius:
                        {
                            return _localizer.GetString("AquariusLabel");
                        }
                    case AstrologicalSignType.Pisces:
                        {
                            return _localizer.GetString("PiscesLabel");
                        }
                }
                return null;
            }
            else
            {
                return $"Expected AstrologicalSignType type for {sign?.ToString()}";
            }
        }

        public object? ConvertBack(object? value, Type targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
