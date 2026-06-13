namespace JustCompute.Features.SavedLocations;

public static class SavedLocationsFeature
{
    public static MauiAppBuilder AddSavedLocationsFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<SavedLocationsPage>();
        builder.Services.AddTransient<SavedLocationsViewModel>();
        return builder;
    }
}
