using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace Compute.Core.Domain.Services
{
    /// <summary>A one-shot fix for where the device is.</summary>
    public interface IDeviceLocationProvider
    {
        /// <summary>
        /// The most recent fix — or, until one arrives, the position kept from the last launch —
        /// or null before either.
        /// </summary>
        Location? DeviceLocation { get; }

        bool IsGettingDeviceLocation { get; }

        /// <summary>
        /// Completes when the fix in flight settles, successfully or not; already complete when
        /// none is. A read-only view of the request — callers wait on it, they cannot finish it.
        /// </summary>
        Task WaitForPendingFixAsync();

        /// <summary>A fresh fix, made the device's position and announced.</summary>
        Task<Result<Location, FaultCode>> GetDeviceGeoLocation();

        /// <summary>
        /// A fresh fix, without making it the device's position: nobody is told. For a caller that
        /// has to decide first whether it is taken — the first fix of a launch can put the device in
        /// another town than the one on screen, and that is asked about before anything moves.
        /// </summary>
        Task<Result<Location, FaultCode>> FetchDeviceGeoLocationAsync();

        /// <summary>Makes <paramref name="position"/> the device's position and announces it.</summary>
        void PublishDeviceLocation(Location position);

        /// <summary>Raised whenever <see cref="DeviceLocation"/> changes to a different object.</summary>
        event EventHandler<DeviceLocationChangedEventArgs> DeviceLocationChanged;
    }

    /// <param name="Previous">The object that stood for the device's position until now, if any.</param>
    /// <param name="Current">The object that stands for it from now on.</param>
    public sealed class DeviceLocationChangedEventArgs(Location? previous, Location current) : EventArgs
    {
        public Location? Previous { get; } = previous;

        public Location Current { get; } = current;
    }
}
