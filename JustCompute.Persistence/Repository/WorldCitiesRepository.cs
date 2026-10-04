using Compute.Core.Domain.Entities.Models;
using Compute.Core.Repository;
using JustCompute.Persistence.Mapping;
using Compute.Core.Utils;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Persistence.Repository.Models;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    public class WorldCitiesRepository : IWorldCitiesRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public WorldCitiesRepository(AppDatabaseConnection connection)
        {
            _database = connection.Catalogue;
        }

        public async Task<City> GetNearestCityAsync(double latitude, double longitude)
        {
            WorldCityTable row = await NearestRowAsync(latitude, longitude).ConfigureAwait(false);
            return row.ToCity();
        }

        public async Task<List<Location>> SearchAsync(string term, int limit)
        {
            IEnumerable<WorldCityTable> rows = await FilterByCityAsync(term, limit).ConfigureAwait(false);
            return rows.ToDomainLocations();
        }

        /// <summary>
        /// Degrees of latitude either side searched first, about 111 km. Anything outside the band
        /// is at least that far away, so a best match inside it that is nearer than that is the
        /// nearest town anywhere.
        /// </summary>
        private const double BandDegrees = 1.0;

        /// <summary>One degree of latitude on the sphere the distances below are measured on.</summary>
        private const double MetersPerDegreeLatitude = 6371e3 * Math.PI / 180.0;

        /// <summary>
        /// Rows ordered by an equirectangular approximation of the distance, which is cheap enough
        /// for SQLite to rank; the true nearest of these is then picked by the haversine.
        /// </summary>
        private const string ApproximateDistance =
            @"(((Lat - ?) * (Lat - ?)) +
              (MIN(ABS(Lng - ?), 360 - ABS(Lng - ?)) * MIN(ABS(Lng - ?), 360 - ABS(Lng - ?)) * ?))";

        private bool _latitudeIndexReady;

        private async Task<WorldCityTable> NearestRowAsync(double currentLat, double currentLng)
        {
            if (double.IsNaN(currentLat) || double.IsNaN(currentLng))
            {
                return new WorldCityTable();
            }

            await EnsureLatitudeIndexAsync().ConfigureAwait(false);

            var cosLatitudeSquared = Math.Pow(Math.Cos(currentLat * Math.PI / 180), 2);
            object[] distanceArgs =
            [
                currentLat, currentLat,
                currentLng, currentLng,
                currentLng, currentLng,
                cosLatitudeSquared,
            ];

            // A band of latitude first, which the index finds without reading the whole table.
            // It used to be all 42,905 rows on every lookup — and every launch does one now, to
            // name the position it opens on.
            var band = await _database.QueryAsync<WorldCityTable>(
                $"SELECT * FROM worldcities WHERE Lat BETWEEN ? AND ? ORDER BY {ApproximateDistance} ASC LIMIT 50",
                [currentLat - BandDegrees, currentLat + BandDegrees, .. distanceArgs]).ConfigureAwait(false);

            var (nearest, meters) = Nearest(band, currentLat, currentLng);
            if (nearest is not null && meters <= BandDegrees * MetersPerDegreeLatitude)
            {
                return nearest;
            }

            // Nothing that close: open ocean, the far north. Something outside the band could be
            // nearer, so the whole table it is.
            var cities = await _database.QueryAsync<WorldCityTable>(
                $"SELECT * FROM worldcities ORDER BY {ApproximateDistance} ASC LIMIT 50",
                distanceArgs).ConfigureAwait(false);

            return Nearest(cities, currentLat, currentLng).City ?? new WorldCityTable();
        }

        private static (WorldCityTable? City, double Meters) Nearest(
            IEnumerable<WorldCityTable> cities, double latitude, double longitude) =>
            cities
                .Select(city => (City: (WorldCityTable?)city,
                    Meters: DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, latitude, longitude)))
                .OrderBy(candidate => candidate.Meters)
                .FirstOrDefault((null, double.PositiveInfinity));

        /// <summary>
        /// Built on first use rather than shipped in the file: the catalogue is replaced wholesale
        /// on every release, and building takes a moment once per version. A failure only means the
        /// band is found by scanning — slower, never wrong.
        /// </summary>
        private async Task EnsureLatitudeIndexAsync()
        {
            if (_latitudeIndexReady) return;

            try
            {
                await _database.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS idx_worldcities_lat ON worldcities(Lat)").ConfigureAwait(false);
            }
            catch (SQLiteException)
            {
            }

            _latitudeIndexReady = true;
        }

        private async Task<IEnumerable<WorldCityTable>> FilterByCityAsync(string name, int limit)
        {
            var searchPattern = $"%{name}%";

            // Capped, and ordered by population so the cap keeps the places a person is most
            // likely to be looking for. An empty term used to match all 42,905 rows, which the
            // search screen then mapped and sorted in full on every single visit.
            return await _database
                .QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities
                  WHERE CityAscii LIKE ? OR Country LIKE ?
                  ORDER BY Population DESC
                  LIMIT ?",
                searchPattern,
                searchPattern,
                limit
                ).ConfigureAwait(false);
        }
    }
}
