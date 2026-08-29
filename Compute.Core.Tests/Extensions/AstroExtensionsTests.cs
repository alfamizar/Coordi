using Compute.Core.Extensions;
using Compute.Core.Domain.Entities.Models.AstroSign;

namespace Compute.Core.Tests.Extensions
{
    public class AstroExtensionsTests
    {
        [Theory]
        [InlineData(1, 10, AstroZodiacSign.Capricorn)]
        [InlineData(1, 25, AstroZodiacSign.Aquarius)]
        [InlineData(2, 25, AstroZodiacSign.Pisces)]
        [InlineData(3, 25, AstroZodiacSign.Aries)]
        [InlineData(4, 25, AstroZodiacSign.Taurus)]
        [InlineData(5, 25, AstroZodiacSign.Gemini)]
        [InlineData(6, 25, AstroZodiacSign.Cancer)]
        [InlineData(7, 25, AstroZodiacSign.Leo)]
        [InlineData(8, 25, AstroZodiacSign.Virgo)]
        [InlineData(9, 25, AstroZodiacSign.Libra)]
        [InlineData(10, 25, AstroZodiacSign.Scorpio)]
        [InlineData(11, 25, AstroZodiacSign.Sagittarius)]
        [InlineData(12, 25, AstroZodiacSign.Capricorn)]
        public void CalculateZodiacSign_ReturnsExpectedSign_ForRepresentativeDates(int month, int day, AstroZodiacSign expected)
        {
            var date = new DateTime(2025, month, day);

            Assert.Equal(expected, date.CalculateZodiacSign());
        }

        // Pins the full zodiac calendar at the sign-change boundaries. The Scorpio range in
        // particular must be Oct 23 – Nov 21: if the sign checks are ever reordered, these
        // assertions catch a range that silently overlaps a neighbour.
        [Theory]
        [InlineData(1, 19, AstroZodiacSign.Capricorn)]
        [InlineData(1, 20, AstroZodiacSign.Aquarius)]
        [InlineData(9, 23, AstroZodiacSign.Libra)]
        [InlineData(10, 22, AstroZodiacSign.Libra)]
        [InlineData(10, 23, AstroZodiacSign.Scorpio)]
        [InlineData(11, 21, AstroZodiacSign.Scorpio)]
        [InlineData(11, 22, AstroZodiacSign.Sagittarius)]
        [InlineData(12, 21, AstroZodiacSign.Sagittarius)]
        [InlineData(12, 22, AstroZodiacSign.Capricorn)]
        public void CalculateZodiacSign_RespectsSignBoundaries(int month, int day, AstroZodiacSign expected)
        {
            var date = new DateTime(2025, month, day, 0, 0, 0);

            Assert.Equal(expected, date.CalculateZodiacSign());
        }
    }
}
