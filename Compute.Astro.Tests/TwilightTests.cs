using Compute.Astro;

namespace Compute.Astro.Tests
{
    /// <summary>
    /// Twilight boundaries, checked against the astral 3.2 reference implementation and against
    /// the cases where the answer is that there is no answer.
    /// </summary>
    public class TwilightTests
    {
        private const double London = 51.5074;
        private const double LondonLon = -0.1278;

        /// <summary>The hour-angle method evaluates the Sun once at noon, so events far from noon
        /// carry a little error. Three minutes is well inside what a twilight row can show.</summary>
        private const double ToleranceMinutes = 3.0;

        private static double MinutesOf(int hour, int minute, int second) =>
            hour * 60.0 + minute + second / 60.0;

        [Fact]
        public void London_AtTheEquinox_MatchesTheReference()
        {
            var t = SunRiseSet.Twilight(2024, 3, 20, London, LondonLon);

            // astral 3.2, London, 2024-03-20 UTC.
            Assert.InRange(t.FirstLightUtcMinutes!.Value, MinutesOf(4, 8, 36) - ToleranceMinutes, MinutesOf(4, 8, 36) + ToleranceMinutes);
            Assert.InRange(t.NauticalDawnUtcMinutes!.Value, MinutesOf(4, 49, 29) - ToleranceMinutes, MinutesOf(4, 49, 29) + ToleranceMinutes);
            Assert.InRange(t.CivilDawnUtcMinutes!.Value, MinutesOf(5, 28, 39) - ToleranceMinutes, MinutesOf(5, 28, 39) + ToleranceMinutes);
            Assert.InRange(t.CivilDuskUtcMinutes!.Value, MinutesOf(18, 48, 4) - ToleranceMinutes, MinutesOf(18, 48, 4) + ToleranceMinutes);
            Assert.InRange(t.NauticalDuskUtcMinutes!.Value, MinutesOf(19, 27, 23) - ToleranceMinutes, MinutesOf(19, 27, 23) + ToleranceMinutes);
            Assert.InRange(t.LastLightUtcMinutes!.Value, MinutesOf(20, 8, 31) - ToleranceMinutes, MinutesOf(20, 8, 31) + ToleranceMinutes);
        }

        [Fact]
        public void TheStagesRunInOrderAcrossTheDay()
        {
            var t = SunRiseSet.Twilight(2024, 3, 20, London, LondonLon);
            var sun = SunRiseSet.Events(2024, 3, 20, London, LondonLon);

            Assert.True(t.FirstLightUtcMinutes < t.NauticalDawnUtcMinutes);
            Assert.True(t.NauticalDawnUtcMinutes < t.CivilDawnUtcMinutes);
            Assert.True(t.CivilDawnUtcMinutes < sun.SunriseUtcMinutes);
            Assert.True(sun.SunsetUtcMinutes < t.CivilDuskUtcMinutes);
            Assert.True(t.CivilDuskUtcMinutes < t.NauticalDuskUtcMinutes);
            Assert.True(t.NauticalDuskUtcMinutes < t.LastLightUtcMinutes);
        }

        [Fact]
        public void LondonInMidsummer_HasNoAstronomicalTwilight()
        {
            // At the solstice the Sun's declination is +23.44°, so at lower culmination it reaches
            // only 23.44 + 51.51 - 90 = -15.05° over London. It therefore crosses -12° but never
            // -18°: nautical twilight happens, astronomical twilight cannot.
            var t = SunRiseSet.Twilight(2024, 6, 21, London, LondonLon);

            Assert.Null(t.FirstLightUtcMinutes);
            Assert.Null(t.LastLightUtcMinutes);
            Assert.NotNull(t.NauticalDawnUtcMinutes);
            Assert.NotNull(t.NauticalDuskUtcMinutes);
            Assert.NotNull(t.CivilDawnUtcMinutes);
            Assert.NotNull(t.CivilDuskUtcMinutes);
        }

        [Fact]
        public void UnderTheMidnightSun_NoStageOccursAtAll()
        {
            // Tromsø in midsummer: the Sun never sets, so it never reaches any twilight depression.
            var t = SunRiseSet.Twilight(2024, 6, 21, 69.6492, 18.9553);

            Assert.Null(t.FirstLightUtcMinutes);
            Assert.Null(t.NauticalDawnUtcMinutes);
            Assert.Null(t.CivilDawnUtcMinutes);
            Assert.Null(t.CivilDuskUtcMinutes);
            Assert.Null(t.NauticalDuskUtcMinutes);
            Assert.Null(t.LastLightUtcMinutes);
        }

        [Fact]
        public void InPolarNight_TheDarkerStagesStillHappen()
        {
            // Tromsø at midwinter: the Sun never rises, but it does climb far enough to bring
            // civil twilight around midday — the few hours of blue the town actually gets.
            var t = SunRiseSet.Twilight(2024, 12, 21, 69.6492, 18.9553);

            Assert.NotNull(t.CivilDawnUtcMinutes);
            Assert.NotNull(t.CivilDuskUtcMinutes);
            Assert.True(t.CivilDawnUtcMinutes < t.CivilDuskUtcMinutes);
        }
    }
}
