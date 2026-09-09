using Compute.Core.Domain.Services;
using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Shared.Abstractions.UI;

namespace JustCompute.Shared.ViewModels;

public sealed class ViewModelServices(
    IDialogService dialogService,
    IGPSLocationService gpsLocationService,
    ILocationService locationService,
    INavigationService navigationService,
    IPermissionGateService permissionGate)
{
    public IDialogService DialogService { get; } = dialogService;

    public IGPSLocationService GpsLocationService { get; } = gpsLocationService;

    public ILocationService LocationService { get; } = locationService;

    public INavigationService NavigationService { get; } = navigationService;

    public IPermissionGateService PermissionGate { get; } = permissionGate;
}
