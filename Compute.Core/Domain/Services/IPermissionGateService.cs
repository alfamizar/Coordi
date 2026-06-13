namespace Compute.Core.Domain.Services
{
    public interface IPermissionGateService
    {
        bool? LastKnownLocationPermissionGranted { get; }

        Task<bool> RefreshLocationPermissionState();

        event EventHandler<bool> LocationPermissionStateChanged;
    }
}
