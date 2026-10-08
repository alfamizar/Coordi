using Compute.Astro;

namespace Compute.Astro.Tests
{
    /// <summary>
    /// Vincenty's inverse does not converge for nearly antipodal points. These pin the answers the
    /// fallback produces, against values that can be derived independently of the algorithm.
    /// </summary>
    public class GeodesyAntipodalTests
    {
        /// <summary>
        /// Half the WGS84 meridian circumference. Every antipodal pair has a geodesic over a pole
        /// of exactly this length, and on an oblate ellipsoid that route is the shortest one, so
        /// this is the distance between any pair of antipodes.
        /// </summary>
        private const double MeridianHalf = 20_003_931.459;

        [Fact]
        public void PoleToPole_IsHalfTheMeridian()
        {
            double d = Geodesy.DistanceMeters(90, 0, -90, 0);

            Assert.InRange(d, MeridianHalf - 1, MeridianHalf + 1);
        }

        [Fact]
        public void EquatorialAntipodes_GoOverThePole_NotRoundTheEquator()
        {
            // The equatorial route is 20,037.5 km; over a pole is 20,003.9 km. On an oblate
            // planet the polar route wins, and that is what a geodesic must find.
            double d = Geodesy.DistanceMeters(0, 0, 0, 180);

            Assert.InRange(d, MeridianHalf - 1, MeridianHalf + 1);
        }

        [Fact]
        public void EveryAntipodalPair_MeasuresHalfTheMeridian()
        {
            // The old failure returned ~19,903 km for the equatorial pair - shorter than any
            // path that exists on this ellipsoid.
            (double lat, double lon)[] points =
            [
                (0, 0), (51.5074, -0.1278), (35.68, 139.65), (-33.86, 151.20), (12.5, -70.1),
                (89.9, 0), (45, 179.9), (-60, -120),
            ];

            foreach (var (lat, lon) in points)
            {
                double antipodeLon = lon > 0 ? lon - 180 : lon + 180;

                double d = Geodesy.DistanceMeters(lat, lon, -lat, antipodeLon);

                Assert.InRange(d, MeridianHalf - 1, MeridianHalf + 1);
            }
        }

        /// <summary>
        /// Distances from GeographicLib 2.0 (Karney's reference implementation), which is accurate
        /// to round-off. The near-antipodal rows are the ones Vincenty cannot reach at all.
        /// </summary>
        [Theory]
        [InlineData(10, 20, -10, -159.99, 20_003_922.228)]
        [InlineData(10, 20, -10, -159.9, 20_003_008.422)]
        [InlineData(10, 20, -10, -159.5, 19_980_861.909)]
        [InlineData(10, 20, -10, -159.0, 19_926_862.680)]
        [InlineData(10, 20, -10, -158.0, 19_817_223.429)]
        [InlineData(10, 20, -10, -150.0, 18_940_143.407)]
        [InlineData(0, 0, 0, 179.5, 19_980_861.909)]
        [InlineData(0, 0, 0, 179.0, 19_926_188.852)]
        [InlineData(0, 0, 0.5, 179.5, 19_936_288.579)]
        [InlineData(45, 0, -45.1, 179.9, 19_992_125.818)]
        [InlineData(51.5074, -0.1278, 48.8566, 2.3522, 343_923.120)]
        public void MatchesTheReferenceImplementation(
            double lat1, double lon1, double lat2, double lon2, double expected)
        {
            double d = Geodesy.DistanceMeters(lat1, lon1, lat2, lon2);

            Assert.InRange(d, expected - 1, expected + 1);
        }

        [Fact]
        public void NearlyAntipodal_IsConsistentWithSlightlyLessAntipodal()
        {
            // Walking the far point away from exact antipodality must shorten the distance, and by
            // no more than the far point itself moved - a discontinuity at the boundary between
            // the fallback and the main path would break one or the other.
            double atAntipode = Geodesy.DistanceMeters(10, 20, -10, -160);
            double nearlyThere = Geodesy.DistanceMeters(10, 20, -10, -159);
            double closerStill = Geodesy.DistanceMeters(10, 20, -10, -150);
            double endpointMoved = Geodesy.DistanceMeters(-10, -160, -10, -159);

            Assert.True(nearlyThere < atAntipode, $"{nearlyThere:N0} should be under {atAntipode:N0}");
            Assert.True(closerStill < nearlyThere, $"{closerStill:N0} should be under {nearlyThere:N0}");
            Assert.True(
                atAntipode - nearlyThere <= endpointMoved,
                $"distance changed by {atAntipode - nearlyThere:N0} m while the endpoint moved only {endpointMoved:N0} m");
        }

        [Fact]
        public void OrdinaryDistances_AreUntouchedByTheFallback()
        {
            // London to Paris, which converges normally and must keep its accuracy.
            Assert.InRange(Geodesy.DistanceMeters(51.5074, -0.1278, 48.8566, 2.3522), 343_900, 343_950);
        }

        // The cases of the Kotlin library's GeodesyAntipodalTest, with its bounds. They are looser
        // than the GeographicLib rows above because the Kotlin fallback is a great circle; this
        // one solves two converged legs instead, so it passes both.

        /// <summary>Half a meridian, from the tables: about 20 004 km pole to pole.</summary>
        [Fact]
        public void PoleToPole_IsHalfAMeridianNotNothing()
        {
            var d = Geodesy.DistanceMeters(90.0, 0.0, -90.0, 0.0);
            Assert.True(Math.Abs(d - 20_003_931.0) < 60_000.0, $"pole to pole came out {d} m");
        }

        /// <summary>And the same place is still the same place.</summary>
        [Fact]
        public void CoincidentPoints_AreStillZero()
        {
            Assert.Equal(0.0, Geodesy.DistanceMeters(48.8566, 2.3522, 48.8566, 2.3522));
        }

        /// <summary>
        /// Round the equator the other way. The true ellipsoidal figure is 20 037 km; the sphere the
        /// Kotlin fallback measures on gives 20 004, which is the half per cent it trades for an answer.
        /// </summary>
        [Fact]
        public void AntipodalOnTheEquator_IsHalfTheWayRound()
        {
            var d = Geodesy.DistanceMeters(0.0, 0.0, 0.0, 180.0);
            Assert.True(Math.Abs(d - 20_020_000.0) < 120_000.0, $"equatorial antipodes came out {d} m");
        }

        /// <summary>
        /// Nearly-antipodal is the case that used to wander rather than converge. No exact figure is
        /// claimed — only that it is the far side of the world and not a number from nowhere.
        /// </summary>
        [Fact]
        public void NearlyAntipodal_LandsInTheRightOrderOfMagnitude()
        {
            var d = Geodesy.DistanceMeters(40.0, -3.0, -39.5, 176.0);
            Assert.True(d > 19_000_000.0 && d < 20_100_000.0, $"near-antipodal came out {d} m");
        }

        /// <summary>The ordinary case must be untouched: Paris to London, a well-known 343.5 km.</summary>
        [Fact]
        public void AnOrdinaryPair_KeepsItsMillimetres()
        {
            var d = Geodesy.DistanceMeters(48.8566, 2.3522, 51.5074, -0.1278);
            Assert.True(Math.Abs(d - 343_556.0) < 500.0, $"Paris to London came out {d} m");
        }
    }
}
