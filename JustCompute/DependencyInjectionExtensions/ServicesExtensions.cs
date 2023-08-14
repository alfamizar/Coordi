#if IOS
using Environment = JustCompute.Platforms.iOS.UI.Environment;
#elif ANDROID
using Environment = JustCompute.Platforms.Android.UI.Environment;
#elif WINDOWS
using Environment = JustCompute.Platforms.Windows.UI.Environment;
#elif MACCATALYST
using Environment = JustCompute.Platforms.MacCatalyst.UI.Environment;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using Environment = System.Object;
#endif
using Compute.Core.Domain.Services;
using Compute.Core.Helpers.UI;
using Compute.Core.Services;
using JustCompute.Presentation.Dialogs;
using JustCompute.Presentation.Helpers;
using JustCompute.Services;

namespace JustCompute.DependencyInjectionExtensions;

public static class ServicesExtensions
{
    public static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<ThemeHandler>();
        builder.Services.AddSingleton<ILocationManager, Services.LocationService.LocationManager>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IDevicePermissionsService<PermissionStatus>, DevicePermissionsService>();
        builder.Services.AddSingleton<IMoonService, MoonService>();
        builder.Services.AddSingleton<ISunService, SunService>();
        builder.Services.AddLocalization();
        builder.Services.AddTransient<IEnvironment, Environment>();

        return builder;
    }
}