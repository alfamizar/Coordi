using Compute.Core.Domain.Services;

namespace JustCompute.Services
{
    public class PermissionGateService : IPermissionGateService
    {
        public bool? LastKnownLocationPermissionGranted { get; private set; }

        public event EventHandler<bool>? LocationPermissionStateChanged;

        public async Task<bool> RefreshLocationPermissionState()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            var isGranted = status == PermissionStatus.Granted;

            if (LastKnownLocationPermissionGranted != isGranted)
            {
                LastKnownLocationPermissionGranted = isGranted;
                LocationPermissionStateChanged?.Invoke(this, isGranted);
            }

            return isGranted;
        }
    }
}
