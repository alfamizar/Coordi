using Compute.Core.Services;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Services
{
    public class MoonServiceTests
    {
        [Fact]
        public async Task GetMoonCyclesAsync_ShouldReturnListOfMoonCycles()
        {
            // Arrange
            double latitude = 40.7128;
            double longitude = -74.0060;
            int timeZoneOffset = -4;
            DateTime currentDate = new(2023, 3, 21);
            int expectedNumberOfDays = DateTime.IsLeapYear(currentDate.Year) ? 366 : 365;
            MoonService moonService = new();

            // Act
            var result = await moonService.GetMoonCyclesAsync(latitude, longitude, currentDate, timeZoneOffset);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<MoonCycle>>(result);
            Assert.Equal(expectedNumberOfDays, result.Count);

            // Additional assertions can be added to check the values of specific MoonCycle objects in the result list.
            // For example, you can verify the GeoDate, EarthHemisphere, RiseTime, SetTime, etc.
            var firstMoonCycle = result.First();
            Assert.Equal(currentDate, firstMoonCycle.GeoDate);
            Assert.Equal(BaseCelestialBodyCycle.Hemisphere.Northern, firstMoonCycle.EarthHemisphere);
            Assert.NotNull(firstMoonCycle.RiseTime);
            Assert.NotNull(firstMoonCycle.SetTime);
        }
    }
}