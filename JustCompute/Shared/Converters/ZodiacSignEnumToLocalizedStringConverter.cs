using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.AstroSign;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Shared.Converters
{
    public class ZodiacSignEnumToLocalizedStringConverter : IValueConverter
    {
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not AstroZodiacSign zodiacSign || zodiacSign == AstroZodiacSign.None)
            {
                // Nothing to show beats a glyph for a sign we do not have. The old arithmetic
                // read ElementAt(-1) for None and threw, and for null silently showed Aries.
                return string.Empty;
            }

            return $"{BaseCelestialBodyCycle.GlyphFor(zodiacSign)} {GetLocalizedStringFromSignEnum(zodiacSign)}";
        }

        private static string? GetLocalizedStringFromSignEnum(object? sign)
        {
            if (sign is AstroZodiacSign moonInZodiacSign)
            {
                string? resourceKey = moonInZodiacSign switch
                {
                    AstroZodiacSign.Aries => "AriesLabel",
                    AstroZodiacSign.Taurus => "TaurusLabel",
                    AstroZodiacSign.Gemini => "GeminiLabel",
                    AstroZodiacSign.Cancer => "CancerLabel",
                    AstroZodiacSign.Leo => "LeoLabel",
                    AstroZodiacSign.Virgo => "VirgoLabel",
                    AstroZodiacSign.Libra => "LibraLabel",
                    AstroZodiacSign.Scorpio => "ScorpioLabel",
                    AstroZodiacSign.Sagittarius => "SagittariusLabel",
                    AstroZodiacSign.Capricorn => "CapricornLabel",
                    AstroZodiacSign.Aquarius => "AquariusLabel",
                    AstroZodiacSign.Pisces => "PiscesLabel",
                    _ => null
                };

                return resourceKey == null ? null : _localizer.GetString(resourceKey).Value;
            }
            else
            {
                return $"Expected AstroZodiacSign type for {sign?.ToString()}";
            }
        }

        public object? ConvertBack(object? value, Type targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
