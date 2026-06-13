using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Domain.Errors;
using DotNext;

namespace Compute.Core.Domain.Services
{
    public interface IGPSLocationService
    {
        Location? DeviceLocation { get; set; }

        Location? SelectedLocation { get; set; }

        bool IsGettingDeviceLocation { get; }

        TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; }

        Task<Result<Location, FaultCode>> GetDeviceGeoLocation();

        event EventHandler<EventArgs> DeviceLocationChanged;

        event EventHandler<DeviceLocationUpdate> DeviceLocationUpdated;

        event EventHandler<DeviceLocationListeningFailure> DeviceLocationListeningFailed;

        Task<Result<bool, FaultCode>> StartListeningForDeviceGeoLocation(bool backgroundCapable = false);

        Result<bool, FaultCode> StopListeningForDeviceLocation();

        Task RestorePersistedSelectedLocation();
    }
}
