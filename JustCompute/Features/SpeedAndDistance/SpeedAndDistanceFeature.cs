namespace JustCompute.Features.SpeedAndDistance;

public static class SpeedAndDistanceFeature
{
    public static MauiAppBuilder AddSpeedAndDistanceFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SpeedAndDistancelPage>();
        builder.Services.AddSingleton<SpeedAndDistanceViewModel>();
        return builder;
    }
}
