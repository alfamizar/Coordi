using Compute.Core.Domain.Services;
using Compute.Core.Navigation;
using Compute.Core.UI;

namespace JustCompute.Shared.ViewModels;

public sealed class ViewModelServices(
    IDialogService dialogService,
    IGPSLocationService gpsLocationService,
    ILocationService locationService,
    INavigationService navigationService)
{
    public IDialogService DialogService { get; } = dialogService;

    public IGPSLocationService GpsLocationService { get; } = gpsLocationService;

    public ILocationService LocationService { get; } = locationService;

    public INavigationService NavigationService { get; } = navigationService;
}
