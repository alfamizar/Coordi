using Compute.Core.Domain.Entities.Models;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace Compute.Core.Domain.Services
{
    public interface ILocationService
    {
        Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1);

        Task<City> GetTheClosestCityToCoordinates(double latitude, double longitude);

        Task<List<Location>> GetSavedLocations();

        Task SaveLocation(Location location);

        Task UpdateLocation(Location location);

        Task DeleteLocation(Location location);

        Task<List<Location>> SearchLocations(string searchParam);
    }
}