using Compute.Core.Domain.Services;
using JustCompute.Shared.Abstractions.Navigation;

namespace JustCompute.Shared.ViewModels;

/// <summary>
/// What BaseViewModel itself needs, and nothing more.
///
/// This used to carry five services into every view model whether it used them or not —
/// dialogs, the location store and the permission gate included. A screen that wants one of
/// those now asks for it in its own constructor, so what each depends on can be read off it.
/// </summary>
public sealed class ViewModelServices(
    INavigationService navigation,
    ILocationSelection selection,
    IDeviceLocationProvider device)
{
    public INavigationService Navigation { get; } = navigation;

    public ILocationSelection Selection { get; } = selection;

    public IDeviceLocationProvider Device { get; } = device;
}
