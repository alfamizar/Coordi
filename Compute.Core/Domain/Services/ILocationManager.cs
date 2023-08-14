using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Errors;
using DotNext;

namespace Compute.Core.Domain.Services
{
    public interface ILocationManager
    {
        public Location CurrentLocation { get; set; }

        public TaskCompletionSource<bool> GettingLocationFinished { get; }

        Task<Result<Location, FaultCode>> ObtainCurrentLocation();

        Task<List<Location>> GetSavedLocations();

        Task SaveLocation(Location location); 

        Task DeleteLocation(Location location);
    }
}