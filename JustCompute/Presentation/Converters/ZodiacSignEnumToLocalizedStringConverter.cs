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
                return moonInZodiacSign switch
                {
                    AstrologicalSignType.Aries => _localizer.GetString("AriesLabel"),
                    AstrologicalSignType.Taurus => _localizer.GetString("TaurusLabel"),
                    AstrologicalSignType.Gemini => _localizer.GetString("GeminiLabel"),
                    AstrologicalSignType.Cancer => _localizer.GetString("CancerLabel"),
                    AstrologicalSignType.Leo => _localizer.GetString("LeoLabel"),
                    AstrologicalSignType.Virgo => _localizer.GetString("VirgoLabel"),
                    AstrologicalSignType.Libra => _localizer.GetString("LibraLabel"),
                    AstrologicalSignType.Scorpio => _localizer.GetString("ScorpioLabel"),
                    AstrologicalSignType.Sagittarius => _localizer.GetString("SagittariusLabel"),
                    AstrologicalSignType.Capricorn => _localizer.GetString("CapricornLabel"),
                    AstrologicalSignType.Aquarius => _localizer.GetString("AquariusLabel"),
                    AstrologicalSignType.Pisces => _localizer.GetString("PiscesLabel"),
                    _ => null
                };
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
