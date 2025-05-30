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

        Task<Result<bool, FaultCode>> StartListeningForDeviceGeoLocation<T, U>(
            EventHandler<T> locationChangedCallback, EventHandler<U> listeningFailedCallback) where T : class where U : class;

        Result<bool, FaultCode> StoptListeningForDeviceLocation<T, U>(
            EventHandler<T> locationChangedCallback, EventHandler<U> listeningFailedCallback) where T : class where U : class;
    }
}