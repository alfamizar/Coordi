using JustCompute.Presentation.ViewModels;

namespace JustCompute.DependencyInjectionExtensions;

public static class ViewModelsExtensions
{
    public static MauiAppBuilder ConfigureViewModels(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SunViewModel>();
        builder.Services.AddSingleton<MoonViewModel>();
        builder.Services.AddSingleton<CoordinatesConverterViewModel>();
        builder.Services.AddSingleton<MoonEclipsesViewModel>();
        builder.Services.AddSingleton<SunEclipsesViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<LocationsViewModel>();
        builder.Services.AddTransient<AddLocationViewModel>();
        builder.Services.AddSingleton<TimeTravelViewModel>();

        return builder;
    }
}