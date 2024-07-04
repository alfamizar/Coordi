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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        static SQLiteAsyncConnection Database;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static readonly AsyncLazy<WorldCitiesDatabase> Instance = new(() =>
        {
            var instance = new WorldCitiesDatabase();
            return instance;
        });

        private WorldCitiesDatabase()
        {
            Database = new SQLiteAsyncConnection(RepositoryConstants.DatabasePath, RepositoryConstants.Flags);
        }

        // optimized, not tested
        /*public async Task<WorldCityTable> GetTheNearestCityAsync(double currentLat, double currentLng)
        {
            // Define the bounding box radius (in degrees) - adjust as needed
            double radius = 1.0; // This is just a start; you may need to expand this iteratively

            // Calculate the bounding box around the current location
            double minLat = currentLat - radius;
            double maxLat = currentLat + radius;
            double minLng = currentLng - radius;
            double maxLng = currentLng + radius;

            // Ensure longitude wraps correctly
            if (minLng < -180) minLng += 360;
            if (maxLng > 180) maxLng -= 360;

            WorldCityTable nearestCity = null;
            double minDistance = double.MaxValue;

            // Query cities within the bounding box
            var cities = await Database.Table<WorldCityTable>()
                .Where(x => x.Lat >= minLat && x.Lat <= maxLat && x.Lng >= minLng && x.Lng <= maxLng)
                .ToListAsync();

            foreach (var city in cities)
            {
                // Calculate the distance using the Haversine formula
                double distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, currentLat, currentLng);

                // Update the nearest city if the current one is closer
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestCity = city;
                }
            }

            // If no cities found in the initial radius, expand the radius
            while (nearestCity == null)
            {
                radius += 1.0; // Expand radius iteratively
                minLat = currentLat - radius;
                maxLat = currentLat + radius;
                minLng = currentLng - radius;
                maxLng = currentLng + radius;

                if (minLng < -180) minLng += 360;
                if (maxLng > 180) maxLng -= 360;

                cities = await Database.Table<WorldCityTable>()
                    .Where(x => x.Lat >= minLat && x.Lat <= maxLat && x.Lng >= minLng && x.Lng <= maxLng)
                    .ToListAsync();

                foreach (var city in cities)
                {
                    double distance = DistanceUtils.GetDistanceOnSphereByHaversineFormula(city.Lat, city.Lng, currentLat, currentLng);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestCity = city;
                    }
                }
            }

            return nearestCity;
        }*/

        public async Task<WorldCityTable> GetTheNearestCityAsync(double currentLat, double currentLng)
        {
            int batchSize = 100;
            int offset = 0;
            WorldCityTable nearestCity = new();
            double minDistance = double.MaxValue;

            while (true)
            {
                var cities = await Database.Table<WorldCityTable>()
                    .OrderBy(x => x.Id)
                    .Skip(offset)
                    .Take(batchSize)
                    .ToListAsync();

                if (cities.Count == 0)
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
                currentLat, currentLat, currentLng, currentLng, cos_lat_2
                );

            return cities.First();
        }

        public static Task<List<WorldCityTable>> GetItemsAsync() => Database.Table<WorldCityTable>().ToListAsync();

        public Task<List<WorldCityTable>> GetCitiesInCountryAsync(string name)
        {
            // SQL queries are also possible xD
            return Database.QueryAsync<WorldCityTable>(@"SELECT * FROM worldcities WHERE Country = ?", name);
        }

        public async Task<IEnumerable<WorldCityTable>> FilterByCity(string name)
        {
            return await Database
                .QueryAsync<WorldCityTable>(
                @"SELECT * FROM worldcities WHERE CityAscii LIKE @name OR Country LIKE @name ORDER BY CityAscii ASC", 
                name + "%"
                );
        }
    }
}