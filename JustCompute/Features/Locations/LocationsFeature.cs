namespace JustCompute.Features.Locations;

public static class LocationsFeature
{
    public static MauiAppBuilder AddLocationsFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<LocationsPage>();
        builder.Services.AddSingleton<LocationsViewModel>();
        return builder;
    }
}
