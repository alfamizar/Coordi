namespace JustCompute.Features.Moon;

public static class MoonFeature
{
    public static MauiAppBuilder AddMoonFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MoonPage>();
        builder.Services.AddSingleton<MoonViewModel>();
        return builder;
    }
}
