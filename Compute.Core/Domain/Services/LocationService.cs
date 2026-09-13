using Compute.Core.Domain.Entities.Models;
using Compute.Core.Repository;

namespace Compute.Core.Domain.Services
{
    /// <summary>
    /// Places, as the app reasons about them.
    ///
    /// This lived in the MAUI project because it had to speak SQL and name persistence row types
    /// to get its work done. Now that the repositories return domain models, it has nothing
    /// platform- or storage-shaped left in it, so it belongs here with the rest of the domain.
    /// </summary>
    public class LocationService(
        ISavedLocationsRepository savedLocations,
        IWorldCitiesRepository worldCities) : ILocationService
    {
        /// <summary>
        /// How many cities a search will return. The catalogue holds tens of thousands; a browse
        /// list only needs enough to scroll, and typing narrows it long before the cap matters.
        /// </summary>
        public const int MaxSearchResults = 200;

        private readonly ISavedLocationsRepository _savedLocations = savedLocations;
        private readonly IWorldCitiesRepository _worldCities = worldCities;

        public async Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1)
        {
            City city = await GetTheClosestCityToCoordinates(latitude, longitude);

            return new Location
            {
                Id = locationId,
                Latitude = latitude,
                Longitude = longitude,
                City = city,
                // The Locations list labels every row by Name, so leaving it unset showed the
                // device's own position as a nameless pair of coordinates.
                Name = city.CityName,
            };
        }

        public Task<City> GetTheClosestCityToCoordinates(double latitude, double longitude) =>
            _worldCities.GetNearestCityAsync(latitude, longitude);

        public Task<List<Location>> GetSavedLocations() => _savedLocations.GetAllAsync();

        public Task SaveLocation(Location location) => _savedLocations.AddAsync(location);

        public Task UpdateLocation(Location location) => _savedLocations.UpdateAsync(location);

        public Task DeleteLocation(Location location) => _savedLocations.DeleteAsync(location);

        public Task<List<Location>> SearchLocations(string searchParam) =>
            _worldCities.SearchAsync(searchParam, MaxSearchResults);
    }
}
