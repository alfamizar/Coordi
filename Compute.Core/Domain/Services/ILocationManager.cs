using Compute.Core.Domain.Entities.Models;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Domain.Errors;
using DotNext;
using System.Reflection;

namespace Compute.Core.Domain.Services
{
    public interface ILocationManager
    {
        public const int DeviceLocationId = -1;

        public Location? DeviceLocation { get; }

        public Location? SelectedLocation { get; set; }

        public bool IsGettingDeviceLocation { get; }

        public TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; }

        Task<Result<Location, FaultCode>> GetDeviceLocation();

        Task<List<Location>> GetSavedLocations();

        Task SaveLocation(Location location);

        Task UpdateLocation(Location location);

        Task DeleteLocation(Location location);

        Task<List<Location>> GetLocationsByCity(string searchParam);

        public event EventHandler<EventArgs> DeviceLocationChanged;

        public void OnStartListeningDeciveLocation();

        public void OnStopListeningDeciveLocation();
    }
}