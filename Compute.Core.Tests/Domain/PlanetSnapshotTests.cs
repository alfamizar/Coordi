using Compute.Astro;
using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// The planets list assembles a day's crossing and a present position out of the engine. The
    /// arithmetic is validated in Compute.Astro's own tests; what these pin is the assembly —
    /// which day's crossing is returned, that the two "now" figures agree with the crossing they
    /// belong to, and that the iteration for a body which does not hold still actually converges.
    /// </summary>
    public class PlanetSnapshotTests
    {
        private const double ParisLat = 48.8566;
        private const double ParisLon = 2.3522;

        private static IReadOnlyList<PlanetSnapshot> Paris(DateTime utcNow, double offsetHours = 2.0) =>
            PlanetSnapshot.AllFor(ParisLat, ParisLon, utcNow, utcNow.AddHours(offsetHours).Date, offsetHours);

        [Fact]
        public void AllSevenArePresent_AndEarthIsNot()
        {
            var planets = Paris(new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc));

            Assert.Equal(7, planets.Count);
            Assert.DoesNotContain(Planet.Earth, planets.Select(p => p.Planet));

            // Ordered outward from the Sun, so the list reads as the solar system and not as a
            // ranking that would change under the reader between one evening and the next.
            Assert.Equal(
                [Planet.Mercury, Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Uranus, Planet.Neptune],
                planets.Select(p => p.Planet));
        }

        [Fact]
        public void TheCrossingReturnedBelongsToTheLocalDayAsked()
        {
            var utcNow = new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc);
            const double offset = 2.0;

            foreach (var planet in Paris(utcNow, offset))
            {
                // Local noon of the day asked for, with the transit nearest it: half a day either
                // way is the whole of that day and nothing of the next.
                var localNoon = utcNow.AddHours(offset).Date.AddHours(12);

                Assert.True(
                    Math.Abs((planet.Transit - localNoon).TotalHours) <= 12.0,
                    $"{planet.Planet} transits at {planet.Transit:u}, not within the day of {localNoon:u}");
            }
        }

        [Fact]
        public void RiseAndSetBracketTheTransit()
        {
            foreach (var planet in Paris(new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc)))
            {
                if (planet.Rise is null || planet.Set is null) continue;

                Assert.True(planet.Rise < planet.Transit, $"{planet.Planet} rises after its transit");
                Assert.True(planet.Transit < planet.Set, $"{planet.Planet} sets before its transit");
            }
        }

        [Fact]
        public void NothingIsBothCircumpolarAndNeverRising()
        {
            // The two flags answer the same question in opposite directions, and a planet in the
            // ecliptic seen from Paris is neither: it is the pair being coherent that matters.
            foreach (var planet in Paris(new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc)))
            {
                Assert.False(planet.Circumpolar && planet.NeverRises);

                if (planet.Circumpolar || planet.NeverRises)
                {
                    Assert.Null(planet.Rise);
                    Assert.Null(planet.Set);
                }
                else
                {
                    Assert.NotNull(planet.Rise);
                    Assert.NotNull(planet.Set);
                }
            }
        }

        [Fact]
        public void TheTransitIsTheHighestTheDayGets()
        {
            var utcNow = new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc);
            const double offset = 2.0;

            foreach (var planet in Paris(utcNow, offset))
            {
                // Sampled from the snapshot's own transit time, in UTC, against the engine.
                var transitUtc = planet.Transit.AddHours(-offset);

                for (int minutes = -240; minutes <= 240; minutes += 20)
                {
                    var jd = AstroTime.JulianDay(DateTime.SpecifyKind(transitUtc.AddMinutes(minutes), DateTimeKind.Utc));
                    var eq = Planets.GeocentricEquatorial(planet.Planet, jd);
                    var alt = HorizontalCoordinates
                        .OfEquatorial(jd, eq.RightAscensionDeg, eq.DeclinationDeg, ParisLat, ParisLon).AltitudeDeg;

                    // A tenth of a degree of slack: the reported altitude is the one the fixed
                    // solution gives at the transit, and the planet has moved a little since.
                    Assert.True(
                        alt <= planet.TransitAltitudeDeg + 0.1,
                        $"{planet.Planet} stands {alt:F3}° at {minutes} min off transit, above the reported {planet.TransitAltitudeDeg:F3}°");
                }
            }
        }

        [Fact]
        public void NowIsWhereTheEngineSaysItIs()
        {
            var utcNow = new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc);
            var jd = AstroTime.JulianDay(utcNow);

            foreach (var planet in Paris(utcNow))
            {
                var eq = Planets.GeocentricEquatorial(planet.Planet, jd);
                var expected = HorizontalCoordinates
                    .OfEquatorial(jd, eq.RightAscensionDeg, eq.DeclinationDeg, ParisLat, ParisLon);

                Assert.Equal(expected.AltitudeDeg, planet.AltitudeDeg, 9);
                Assert.Equal(expected.AzimuthDeg, planet.AzimuthDeg, 9);
                Assert.Equal(planet.AltitudeDeg > 0.0, planet.IsUp);
            }
        }

        /// <summary>
        /// Mercury moves furthest against the stars in a day, so it is the worst case for solving
        /// a moving body as though it were fixed. Re-solving at the first answer has to leave the
        /// hour angle at the reported transit near zero — that is what "transit" means.
        /// </summary>
        [Fact]
        public void TheIterationConverges_EvenForMercury()
        {
            const double offset = 2.0;

            for (int day = 1; day <= 28; day += 3)
            {
                var utcNow = new DateTime(2026, 4, day, 12, 0, 0, DateTimeKind.Utc);
                var mercury = Paris(utcNow, offset).First(p => p.Planet == Planet.Mercury);

                var transitUtc = DateTime.SpecifyKind(mercury.Transit.AddHours(-offset), DateTimeKind.Utc);
                var jd = AstroTime.JulianDay(transitUtc);
                var eq = Planets.GeocentricEquatorial(Planet.Mercury, jd);
                var hourAngle = HorizontalCoordinates.HourAngleDeg(jd, eq.RightAscensionDeg, ParisLon);

                // Four arcseconds of hour angle is under a third of a second of time.
                Assert.InRange(hourAngle, -0.001, 0.001);
            }
        }

        [Fact]
        public void DistanceIsTheEnginesDistance()
        {
            var utcNow = new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc);
            var jd = AstroTime.JulianDay(utcNow);

            foreach (var planet in Paris(utcNow))
            {
                Assert.Equal(Planets.GeocentricDistanceAu(planet.Planet, jd), planet.DistanceAu, 9);
            }
        }

        [Fact]
        public void APlanetOverTheNorthPoleIsCircumpolarOrDown_NeverBoth()
        {
            // The one latitude where the flags actually fire. Whichever way each planet falls, the
            // rise and set must be absent and the pair must stay coherent.
            var planets = PlanetSnapshot.AllFor(
                89.9, 0.0, new DateTime(2026, 12, 21, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 21), 0.0);

            foreach (var planet in planets)
            {
                Assert.True(planet.Circumpolar || planet.NeverRises, $"{planet.Planet} neither rises nor stays");
                Assert.Null(planet.Rise);
                Assert.Null(planet.Set);
            }
        }
    }
}
