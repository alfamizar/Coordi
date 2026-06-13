using Compute.Core.Domain.Entities.Models;
using CoordinateSharp;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Features.TimeTravel
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
                string? resourceKey = moonInZodiacSign switch
                {
                    AstrologicalSignType.Aries => "AriesLabel",
                    AstrologicalSignType.Taurus => "TaurusLabel",
                    AstrologicalSignType.Gemini => "GeminiLabel",
                    AstrologicalSignType.Cancer => "CancerLabel",
                    AstrologicalSignType.Leo => "LeoLabel",
                    AstrologicalSignType.Virgo => "VirgoLabel",
                    AstrologicalSignType.Libra => "LibraLabel",
                    AstrologicalSignType.Scorpio => "ScorpioLabel",
                    AstrologicalSignType.Sagittarius => "SagittariusLabel",
                    AstrologicalSignType.Capricorn => "CapricornLabel",
                    AstrologicalSignType.Aquarius => "AquariusLabel",
                    AstrologicalSignType.Pisces => "PiscesLabel",
                    _ => null
                };

                return resourceKey == null ? null : _localizer.GetString(resourceKey).Value;
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
