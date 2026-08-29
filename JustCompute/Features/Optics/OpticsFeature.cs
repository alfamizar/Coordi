namespace JustCompute.Features.Optics;

public static class OpticsFeature
{
    public static MauiAppBuilder AddOpticsFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<OpticsPage>();
        builder.Services.AddSingleton<OpticsViewModel>();
        return builder;
    }
}
