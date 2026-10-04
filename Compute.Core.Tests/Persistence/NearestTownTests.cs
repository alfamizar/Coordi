using Compute.Core.Utils;
using JustCompute.Persistence.Repository;
using JustCompute.Persistence.Repository.Models;
using SQLite;

namespace Compute.Core.Tests.Persistence
{
    /// <summary>
    /// The nearest-town lookup searches a band of latitude first and the whole catalogue only
    /// when nothing in the band is close enough. Checked against the shipped catalogue itself:
    /// whatever it answers must be the true nearest of all 42,905 towns, by the haversine.
    /// </summary>
    public sealed class NearestTownTests : IDisposable
    {
        private readonly string _directory = Directory.CreateTempSubdirectory("coordi-nearest-").FullName;
        private readonly WorldCitiesRepository _repository;
        private readonly List<WorldCityTable> _all;

        public NearestTownTests()
        {
            var paths = DatabasePaths.InDirectory(_directory);
            File.Copy(ShippedCatalogue(), paths.Catalogue);

            using (var db = new SQLiteConnection(paths.Catalogue))
            {
                _all = db.Query<WorldCityTable>("SELECT * FROM worldcities");
            }

            _repository = new WorldCitiesRepository(new AppDatabaseConnection(paths));
        }

        public void Dispose()
        {
            SQLiteAsyncConnection.ResetPool();
            try { Directory.Delete(_directory, recursive: true); } catch (IOException) { }
        }

        public static TheoryData<double, double> HardPlaces() => new()
        {
            { 50.0647, 19.9450 },   // Kraków: dense, the band answers
            { 0.0, -150.0 },        // mid-Pacific: nothing within a degree, the full scan answers
            { 89.9, 0.0 },          // the pole
            { -89.9, 45.0 },
            { 65.0, 179.9 },        // either side of the date line
            { 65.0, -179.9 },
            { -33.86, 151.21 },     // Sydney
            { 78.22, 15.65 },       // Svalbard
            { 50.0647 + 0.99, 19.9450 },   // just inside the band's edge
        };

        [Theory]
        [MemberData(nameof(HardPlaces))]
        public async Task TheAnswerIsTheTrueNearest_InHardPlaces(double latitude, double longitude) =>
            await AssertTrueNearest(latitude, longitude);

        [Fact]
        public async Task TheAnswerIsTheTrueNearest_AllOverTheGlobe()
        {
            var random = new Random(20261004);
            for (var i = 0; i < 400; i++)
            {
                // Uniform on the sphere, not in latitude: the poles would otherwise be oversampled.
                var latitude = Math.Asin(2 * random.NextDouble() - 1) * 180 / Math.PI;
                var longitude = random.NextDouble() * 360 - 180;
                await AssertTrueNearest(latitude, longitude);
            }
        }

        private async Task AssertTrueNearest(double latitude, double longitude)
        {
            var answer = await _repository.GetNearestCityAsync(latitude, longitude);

            var best = _all.Min(c => Meters(c, latitude, longitude));
            var nearest = _all
                .Where(c => Meters(c, latitude, longitude) <= best + 1.0)
                .Select(c => (c.CityAscii, c.Country))
                .ToList();

            Assert.True(
                nearest.Contains((answer.CityName, answer.CountryName)),
                $"({latitude:F4}, {longitude:F4}): answered {answer.CityName}, {answer.CountryName}; " +
                $"nearest is {string.Join(" / ", nearest)} at {best / 1000:F1} km");
        }

        private static double Meters(WorldCityTable city, double latitude, double longitude) =>
            DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, latitude, longitude);

        private static string ShippedCatalogue()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "JustCompute", "Database", "geo_world.db");
                if (File.Exists(candidate)) return candidate;
            }

            throw new FileNotFoundException("The shipped catalogue, JustCompute/Database/geo_world.db, was not found above the test output.");
        }
    }
}
