using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Domain.Errors;
using Compute.Core.Common.Results;

namespace Compute.Core.Domain.Services
{
    public interface IGPSLocationService
    {
        Location? DeviceLocation { get; set; }

        Location? SelectedLocation { get; set; }

        /// <summary>
        /// True while the app has nowhere real to compute from: the user has not chosen a place
        /// and the device's own fix has not arrived either, so every screen is running on the
        /// invented fallback. Drives the onboarding cards, which have nothing to ask for once a
        /// fix has been adopted.
        /// </summary>
        bool ShouldPromptForLocation { get; }

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
