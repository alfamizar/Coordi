using Compute.Core.Utils;

namespace Compute.Core.Tests.Utils
{
    public class DistanceUtilsTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(40.7128, -74.0060)]
        [InlineData(-33.8688, 151.2093)]
        public void Haversine_SamePoint_IsZero(double lat, double lon)
        {
            var distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(lat, lon, lat, lon);

            Assert.Equal(0, distance, 6);
        }

        [Fact]
        public void Haversine_OneDegreeAtEquator_IsAboutOneEleventhOfEarthCircumference()
        {
            // One degree of longitude at the equator on a 6371 km sphere ≈ 111.195 km.
            var distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(0, 0, 0, 1);

            Assert.True(Math.Abs(distance - 111_194.93) < 1.0, $"Expected ~111194.93 m, got {distance} m");
        }

        [Fact]
        public void Haversine_IsSymmetric()
        {
            var ab = DistanceUtils.GetDistanceOnSphereByHaversineFormula(51.5074, -0.1278, 48.8566, 2.3522);
            var ba = DistanceUtils.GetDistanceOnSphereByHaversineFormula(48.8566, 2.3522, 51.5074, -0.1278);

            Assert.Equal(ab, ba, 6);
        }

        [Fact]
        public void Haversine_LondonToParis_IsAboutPublishedGreatCircleDistance()
        {
            // Published great-circle distance London ↔ Paris is ≈ 343–344 km.
            var distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(51.5074, -0.1278, 48.8566, 2.3522);

            Assert.InRange(distance, 340_000, 345_000);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(90, Math.PI / 2)]
        [InlineData(180, Math.PI)]
        [InlineData(-180, -Math.PI)]
        public void ToRadians_ConvertsDegrees(double degrees, double expectedRadians)
        {
            Assert.Equal(expectedRadians, degrees.ToRadians(), 12);
        }
    }
}
