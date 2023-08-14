using Compute.Core.Helpers;
using Compute.Core.Repository;
using Compute.Core.Utils;
using JustCompute.Persistance.Repository.Constants;
using JustCompute.Persistance.Repository.Models;
using SQLite;

namespace JustCompute.Persistance.Repository
{
    public class WorldCitiesDatabase : IWorldCitiesDatabase<WorldCityTable>
    {
        static SQLiteAsyncConnection Database;

        public static readonly AsyncLazy<WorldCitiesDatabase> Instance = new(() =>
        {
            var instance = new WorldCitiesDatabase();
            return instance;
        });

        public WorldCitiesDatabase()
        {
            Database = new SQLiteAsyncConnection(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
        }

        public async Task<WorldCityTable> GetTheNearestCityAsync(double currentLat, double currentLng)
        {
            int batchSize = 100; // Adjust the batch size according to your needs
            int offset = 0;
            WorldCityTable nearestCity = null;
            double minDistance = double.MaxValue;

            while (true)
            {
                var cities = await Database.Table<WorldCityTable>()
                    .OrderBy(x => x.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .ToListAsync();

                if (!cities.Any())
                {
                    break;
                }

                foreach (var city in cities)
                {
                    double distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, currentLat, currentLng);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestCity = city;
                    }
                }

                offset += batchSize;
            }

            return nearestCity;
        }

        public async Task<WorldCityTable> GetTheNearestCityByQueryAsync(double currentLat, double currentLng)
        {
            var cos_lat_2 = Math.Pow(Math.Cos(currentLat * Math.PI / 180), 2);

            var cities = await Database.QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities ORDER BY (((Lat - ?) * (Lat - ?)) + ((Lng - ?) * (Lng - ?)) * ?) ASC",
                currentLat, currentLat, currentLng, currentLng, cos_lat_2);

            return cities.FirstOrDefault();
        }

        public Task<List<WorldCityTable>> GetItemsAsync()
        {
            return Database.Table<WorldCityTable>().ToListAsync();
        }

        public Task<List<WorldCityTable>> GetCitiesInCountryAsync(string name)
        {
            // SQL queries are also possible xD
            return Database.QueryAsync<WorldCityTable>(@"SELECT * FROM worldcities WHERE Country = ?", name);
        }
    }
}