using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace Compute.Core.Domain.Services
{
    /// <summary>A one-shot fix for where the device is.</summary>
    public interface IDeviceLocationProvider
    {
        /// <summary>The most recent fix, or null before one has arrived.</summary>
        Location? DeviceLocation { get; }

        bool IsGettingDeviceLocation { get; }

        /// <summary>
        /// Completes when the fix in flight settles, successfully or not; already complete when
        /// none is. A read-only view of the request — callers wait on it, they cannot finish it.
        /// </summary>
        Task WaitForPendingFixAsync();

        Task<Result<Location, FaultCode>> GetDeviceGeoLocation();

        /// <summary>
        /// Makes <paramref name="listInstance"/> the object that stands for the device's position.
        ///
        /// The Locations screen moves each fresh fix onto the row it already shows, because the
        /// list is bound to that row and replacing it would lose the reader's place. This is how
        /// it says so. Only the device's own row qualifies: anything else is rejected.
        /// </summary>
        void KeepListInstance(Location listInstance);

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
