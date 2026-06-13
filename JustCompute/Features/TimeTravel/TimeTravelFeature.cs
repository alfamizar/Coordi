namespace JustCompute.Features.TimeTravel;

public static class TimeTravelFeature
{
    public static MauiAppBuilder AddTimeTravelFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<TimeTravelPage>();
        builder.Services.AddSingleton<TimeTravelViewModel>();
        return builder;
    }
}
