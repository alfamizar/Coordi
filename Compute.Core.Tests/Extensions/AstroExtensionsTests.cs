using Compute.Core.Extensions;
using CoordinateSharp;

namespace Compute.Core.Tests.Extensions
{
    public class AstroExtensionsTests
    {
        [Theory]
        [InlineData(1, 10, AstrologicalSignType.Capricorn)]
        [InlineData(1, 25, AstrologicalSignType.Aquarius)]
        [InlineData(2, 25, AstrologicalSignType.Pisces)]
        [InlineData(3, 25, AstrologicalSignType.Aries)]
        [InlineData(4, 25, AstrologicalSignType.Taurus)]
        [InlineData(5, 25, AstrologicalSignType.Gemini)]
        [InlineData(6, 25, AstrologicalSignType.Cancer)]
        [InlineData(7, 25, AstrologicalSignType.Leo)]
        [InlineData(8, 25, AstrologicalSignType.Virgo)]
        [InlineData(9, 25, AstrologicalSignType.Libra)]
        [InlineData(10, 25, AstrologicalSignType.Scorpio)]
        [InlineData(11, 25, AstrologicalSignType.Sagittarius)]
        [InlineData(12, 25, AstrologicalSignType.Capricorn)]
        public void CalculateZodiacSign_ReturnsExpectedSign_ForRepresentativeDates(int month, int day, AstrologicalSignType expected)
        {
            var date = new DateTime(2025, month, day);

            Assert.Equal(expected, date.CalculateZodiacSign());
        }

        // Pins the full zodiac calendar at the sign-change boundaries. The Scorpio range in
        // particular must be Oct 23 – Nov 21: if the sign checks are ever reordered, these
        // assertions catch a range that silently overlaps a neighbour.
        [Theory]
        [InlineData(1, 19, AstrologicalSignType.Capricorn)]
        [InlineData(1, 20, AstrologicalSignType.Aquarius)]
        [InlineData(9, 23, AstrologicalSignType.Libra)]
        [InlineData(10, 22, AstrologicalSignType.Libra)]
        [InlineData(10, 23, AstrologicalSignType.Scorpio)]
        [InlineData(11, 21, AstrologicalSignType.Scorpio)]
        [InlineData(11, 22, AstrologicalSignType.Sagittarius)]
        [InlineData(12, 21, AstrologicalSignType.Sagittarius)]
        [InlineData(12, 22, AstrologicalSignType.Capricorn)]
        public void CalculateZodiacSign_RespectsSignBoundaries(int month, int day, AstrologicalSignType expected)
        {
            var date = new DateTime(2025, month, day, 0, 0, 0);

            Assert.Equal(expected, date.CalculateZodiacSign());
        }
    }
}
