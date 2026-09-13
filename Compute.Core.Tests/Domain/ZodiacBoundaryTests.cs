using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.AstroSign;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Extensions;

namespace Compute.Core.Tests.Domain
{
    public class ZodiacBoundaryTests
    {
        [Theory]
        [InlineData(11, 20, AstroZodiacSign.Scorpio)]
        [InlineData(11, 21, AstroZodiacSign.Scorpio)]      // last day of Scorpio
        [InlineData(11, 22, AstroZodiacSign.Sagittarius)]  // first day of Sagittarius
        [InlineData(12, 21, AstroZodiacSign.Sagittarius)]
        [InlineData(12, 22, AstroZodiacSign.Capricorn)]
        public void SagittariusBeginsTheDayAfterScorpioEnds(int month, int day, AstroZodiacSign expected) =>
            Assert.Equal(expected, new DateTime(2026, month, day, 12, 0, 0).CalculateZodiacSign());

        [Fact]
        public void EveryDayOfTheYearResolvesToExactlyOneSign()
        {
            for (var d = new DateTime(2026, 1, 1); d.Year == 2026; d = d.AddDays(1))
            {
                Assert.NotEqual(AstroZodiacSign.None, d.CalculateZodiacSign());
            }
        }

        [Fact]
        public void GlyphFor_HandlesEverySignAndNone()
        {
            Assert.Equal(string.Empty, BaseCelestialBodyCycle.GlyphFor(AstroZodiacSign.None));

            foreach (AstroZodiacSign sign in Enum.GetValues<AstroZodiacSign>())
            {
                if (sign == AstroZodiacSign.None) continue;
                Assert.False(string.IsNullOrEmpty(BaseCelestialBodyCycle.GlyphFor(sign)));
            }
        }

        [Fact]
        public void SettingANoneSign_DoesNotThrow()
        {
            // A default-constructed cycle holds None; the glyph lookup used to index -1.
            var cycle = new MoonCycle { MoonInZodiacSign = AstroZodiacSign.None };
            Assert.Equal(string.Empty, cycle.MoonInZodiacSignUnicodeIcon);
        }
    }
}
