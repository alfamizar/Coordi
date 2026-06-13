using Compute.Core.Common.Device;
using Compute.Core.UI;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;

namespace JustCompute.Services
{
    public class DevicePermissionsService(IStringLocalizer<AppStringsRes> localizer, IDialogService dialogService) : IDevicePermissionsService<PermissionStatus>
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = localizer;
        private readonly IDialogService _dialogService = dialogService;
        private readonly SemaphoreSlim _permissionSemaphore = new(1, 1);

        public async Task<PermissionStatus> CheckPermissionAndRequestIfNeeded(
            Permission permission,
            bool shouldShowRationaleIfNeeded = true)
        {
            await _permissionSemaphore.WaitAsync();

            try
            {
                PermissionStatus status = permission switch
                {
                    Permission.DeviceLocation => await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>(),
                    _ => throw new ArgumentException("Unknown permission", nameof(permission))
                };

                if (status == PermissionStatus.Granted)
                {
                    return status;
                }

                if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // On iOS once a permission has been denied it may not be requested again from the application.
                    return status;
                }

                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>() && shouldShowRationaleIfNeeded)
                {
                    await _dialogService.DisplayAlert(
                        _localizer.GetString("LocationPermissionDialogTitle"),
                        _localizer.GetString("LocationPermissionDialogMessage"),
                        _localizer.GetString("Ok"));
                }

                return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            finally
            {
                _permissionSemaphore.Release();
            }
        }
    }
}
