using JustCompute.Presentation.Popups.ViewModels;
using JustCompute.Presentation.ViewModels;

namespace JustCompute.DependencyInjectionExtensions;

public static class ViewModelsExtensions
{
    public static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
    {
        //builder.Services.AddTransient(typeof(SunViewModel<>)); how to register generic
        builder.Services.AddSingleton<SunViewModel>();
        builder.Services.AddSingleton<MoonViewModel>();
        builder.Services.AddSingleton<CoordinatesConverterViewModel>();
        builder.Services.AddSingleton<MoonEclipsesViewModel>();
        builder.Services.AddSingleton<SunEclipsesViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<LocationsViewModel>();
        builder.Services.AddTransient<InputLocationViewModel>();
        builder.Services.AddSingleton<TimeTravelViewModel>();
        builder.Services.AddSingleton<SearchByCityViewModel>();
        builder.Services.AddSingleton<SavedLocationsViewModel>();
        builder.Services.AddSingleton<DistanceViewModel>();

        return builder;
    }
}