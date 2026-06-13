using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services.Moon;

namespace Compute.Core.Tests.Services
{
    public class MoonServiceTests
    {
        [Fact]
        public async Task GetMoonCyclesAsync_ShouldReturnListOfMoonCycles()
        {
            double latitude = 40.7128;
            double longitude = -74.0060;
            DateTime currentDate = new(2023, 3, 21);
            int expectedNumberOfDays = (currentDate.AddYears(1) - currentDate).Days;
            MoonService moonService = new();

            var result = await moonService.GetMoonCyclesAsync(latitude, longitude, currentDate);

            Assert.NotNull(result);
            Assert.IsType<List<MoonCycle>>(result);
            Assert.Equal(expectedNumberOfDays, result.Count);

            var firstMoonCycle = result.First();
            Assert.Equal(currentDate, firstMoonCycle.GeoDate);
            Assert.Equal(BaseCelestialBodyCycle.Hemisphere.Northern, firstMoonCycle.EarthHemisphere);
            Assert.NotNull(firstMoonCycle.RiseTime);
            Assert.NotNull(firstMoonCycle.SetTime);
        }
    }
}
