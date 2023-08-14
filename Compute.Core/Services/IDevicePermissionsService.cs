using Compute.Core.Helpers;

namespace Compute.Core.Services
{
    public interface IDevicePermissionsService<T>
    {
        Task<T> CheckPermissionAndRequestIfNeeded(Permission permission, bool shouldShowRationaleIfNeeded = true);
    }
}
