using Compute.Core.Repository;
using Compute.Core.Utils;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Persistence.Repository.Models;
using SQLite;

namespace JustCompute.Persistence.Repository
{
    public class WorldCitiesRepository : IWorldCitiesRepository<WorldCityTable>
    {
        private readonly SQLiteAsyncConnection _database;

        public WorldCitiesRepository()
        {
            _database = new SQLiteAsyncConnection(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
        }

        public async Task<WorldCityTable> GetTheNearestCityAsync(double currentLat, double currentLng)
        {
            return await GetTheNearestCityByQueryAsync(currentLat, currentLng).ConfigureAwait(false);
        }

        public async Task<WorldCityTable> GetTheNearestCityByQueryAsync(double currentLat, double currentLng)
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

        public Task<List<WorldCityTable>> GetItemsAsync() => _database.Table<WorldCityTable>().ToListAsync();

        public Task<List<WorldCityTable>> GetCitiesInCountryAsync(string name)
        {
            return _database.QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities WHERE Country = ? ORDER BY CityAscii ASC",
                name);
        }

        public async Task<IEnumerable<WorldCityTable>> FilterByCity(string name)
        {
            var searchPattern = $"%{name}%";

            return await _database
                .QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities
                  WHERE CityAscii LIKE ? OR Country LIKE ?
                  ORDER BY CityAscii ASC",
                searchPattern,
                searchPattern
                ).ConfigureAwait(false);
        }
    }
}
