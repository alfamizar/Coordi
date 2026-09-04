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

        private async Task<WorldCityTable> NearestRowAsync(double currentLat, double currentLng)
        {
            if (double.IsNaN(currentLat) || double.IsNaN(currentLng))
            {
                return new WorldCityTable();
            }

            var cosLatitudeSquared = Math.Pow(Math.Cos(currentLat * Math.PI / 180), 2);

            var cities = await _database.QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities
                  ORDER BY (((Lat - ?) * (Lat - ?)) +
                    (MIN(ABS(Lng - ?), 360 - ABS(Lng - ?)) * MIN(ABS(Lng - ?), 360 - ABS(Lng - ?)) * ?)) ASC
                  LIMIT 50",
                currentLat, currentLat,
                currentLng, currentLng,
                currentLng, currentLng,
                cosLatitudeSquared
                );

            return cities
                .OrderBy(city => DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, currentLat, currentLng))
                .FirstOrDefault() ?? new WorldCityTable();
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
