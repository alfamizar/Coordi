using Compute.Core.Domain.Entities.Models.AstroSign;

namespace Compute.Core.Extensions
{
    public static class AstroExtensions
    {
        public static AstroZodiacSign CalculateZodiacSign(this DateTime date)
        {
            if (date >= new DateTime(date.Year, 1, 1) && date <= new DateTime(date.Year, 1, 19, 23, 59, 59))
            {
                return AstroZodiacSign.Capricorn;
            }
            if (date >= new DateTime(date.Year, 1, 20) && date <= new DateTime(date.Year, 2, 18, 23, 59, 59))
            {
                return AstroZodiacSign.Aquarius;
            }
            if (date >= new DateTime(date.Year, 2, 19) && date <= new DateTime(date.Year, 3, 20, 23, 59, 59))
            {
                return AstroZodiacSign.Pisces;
            }
            if (date >= new DateTime(date.Year, 3, 21) && date <= new DateTime(date.Year, 4, 19, 23, 59, 59))
            {
                return AstroZodiacSign.Aries;
            }
            if (date >= new DateTime(date.Year, 4, 20) && date <= new DateTime(date.Year, 5, 20, 23, 59, 59))
            {
                return AstroZodiacSign.Taurus;
            }
            if (date >= new DateTime(date.Year, 5, 21) && date <= new DateTime(date.Year, 6, 20, 23, 59, 59))
            {
                return AstroZodiacSign.Gemini;
            }
            if (date >= new DateTime(date.Year, 6, 21) && date <= new DateTime(date.Year, 7, 22, 23, 59, 59))
            {
                return AstroZodiacSign.Cancer;
            }
            if (date >= new DateTime(date.Year, 7, 23) && date <= new DateTime(date.Year, 8, 22, 23, 59, 59))
            {
                return AstroZodiacSign.Leo;
            }
            if (date >= new DateTime(date.Year, 8, 23) && date <= new DateTime(date.Year, 9, 22, 23, 59, 59))
            {
                return AstroZodiacSign.Virgo;
            }
            if (date >= new DateTime(date.Year, 9, 23) && date <= new DateTime(date.Year, 10, 22, 23, 59, 59))
            {
                return AstroZodiacSign.Libra;
            }
            if (date >= new DateTime(date.Year, 10, 23) && date <= new DateTime(date.Year, 11, 21, 23, 59, 59))
            {
                return AstroZodiacSign.Scorpio;
            }
            if (date >= new DateTime(date.Year, 11, 21) && date <= new DateTime(date.Year, 12, 21, 23, 59, 59))
            {
                return AstroZodiacSign.Sagittarius;
            }
            if (date >= new DateTime(date.Year, 12, 22) && date <= new DateTime(date.Year, 12, 31, 23, 59, 59))
            {
                return AstroZodiacSign.Capricorn;
            }

            return AstroZodiacSign.Capricorn;
        }
    }
}
