namespace JustCompute.Features.Distance;

public static class DistanceFeature
{
    public static MauiAppBuilder AddDistanceFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<DistancePage>();
        builder.Services.AddSingleton<DistanceViewModel>();
        return builder;
    }
}
