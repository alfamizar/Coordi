#if IOS
using Environment = JustCompute.Platforms.iOS.UI.Environment;
#elif ANDROID
using Environment = JustCompute.Platforms.Android.UI.Environment;
#else
using Environment = System.Object;
#endif
using JustCompute.Shared.Helpers;
using JustCompute.Services;
using Compute.Core.Navigation;
using JustCompute.Navigation;
using Compute.Core.UI;
using Compute.Core.Domain.Services.Moon;
using Compute.Core.Domain.Services.Sun;
using Compute.Core.Domain.Services.Weather;
using Compute.Core.Domain.Services;
using Compute.Core.Common.Device;
using JustCompute.Services.LocationService;
using JustCompute.Shared.Popups;
using Compute.Core.Common.Messaging;
using Compute.Core.Repository;
using JustCompute.Persistence.Repository;
using JustCompute.Persistence.Repository.Models;
using JustCompute.Shared.ViewModels;

namespace JustCompute.DependencyInjectionExtensions;

public static class ServicesExtensions
{
    public static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<ThemeHandler>();
        builder.Services.AddSingleton<IGPSLocationService, GPSLocationService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IToastService, ToastService>();
        builder.Services.AddSingleton<IDevicePermissionsService<PermissionStatus>, DevicePermissionsService>();
        builder.Services.AddSingleton<IPermissionGateService, PermissionGateService>();
        builder.Services.AddSingleton<IMoonService, MoonService>();
        builder.Services.AddSingleton<ISunService, SunService>();
        builder.Services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
        builder.Services.AddSingleton<IWeatherService, WeatherService>();
        builder.Services.AddLocalization();
        builder.Services.AddTransient<IEnvironment, Environment>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IMessagingService, MessagingService>();
        builder.Services.AddSingleton<ViewModelServices>();
        builder.Services.AddSingleton<ILocationsRepository, SavedLocationsRepository>();
        builder.Services.AddSingleton<IWorldCitiesRepository<WorldCityTable>, WorldCitiesRepository>();

        return builder;
    }
}
