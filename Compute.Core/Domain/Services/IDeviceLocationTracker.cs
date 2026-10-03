using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;

namespace Compute.Core.Domain.Services
{
    /// <summary>A continuous stream of fixes, for recording a trip.</summary>
    public interface IDeviceLocationTracker
    {
        event EventHandler<DeviceLocationUpdate> DeviceLocationUpdated;

        event EventHandler<DeviceLocationListeningFailure> DeviceLocationListeningFailed;

        Task<Result<bool, FaultCode>> StartListeningForDeviceGeoLocation(bool backgroundCapable = false);

        Result<bool, FaultCode> StopListeningForDeviceLocation();
    }
}
