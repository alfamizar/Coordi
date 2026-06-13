namespace Compute.Core.Common.Device
{
    public interface IDevicePermissionsService<T>
    {
        Task<T> CheckPermissionAndRequestIfNeeded(Permission permission, bool shouldShowRationaleIfNeeded = true);
    }
}
