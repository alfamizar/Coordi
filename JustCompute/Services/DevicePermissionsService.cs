using Compute.Core.Helpers;
using Compute.Core.Services;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;

namespace JustCompute.Services
{
    public class DevicePermissionsService : IDevicePermissionsService<PermissionStatus>
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IDialogService _dialogService;
        private bool _isObtainingPermsInProgress;
        private TaskCompletionSource<PermissionStatus> _tcs;

        public DevicePermissionsService(IStringLocalizer<AppStringsRes> localizer, IDialogService dialogService)
        {
            _localizer = localizer;
            _dialogService = dialogService;
        }

        public async Task<PermissionStatus> CheckPermissionAndRequestIfNeeded(
            Permission permission,
            bool shouldShowRationaleIfNeeded = true)
        {
            if (_isObtainingPermsInProgress)
            {
                _tcs ??= new TaskCompletionSource<PermissionStatus>();
                return await _tcs.Task;
            }

            _isObtainingPermsInProgress = true;

            PermissionStatus status = permission switch
            {
                Permission.CurrentLocation => await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>(),
                _ => throw new ArgumentException("Unknown permission", nameof(permission))
            };

            if (status == PermissionStatus.Granted)
            {
                _isObtainingPermsInProgress = false;
                return status;
            }

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // todo
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                _isObtainingPermsInProgress = false;
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>() && shouldShowRationaleIfNeeded)
            {
                // Prompt the user with additional information as to why the permission is needed
                await _dialogService.DisplayAlert(
                    _localizer.GetString("LocationPermissionDialogTitle"),
                    _localizer.GetString("LocationPermissionDialogMessage"), 
                    _localizer.GetString("Ok"));
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            _isObtainingPermsInProgress = false;

            if (_tcs != null)
            {
                _tcs.TrySetResult(status);
                _tcs = null;
            }

            return status;
        }
    }
}
