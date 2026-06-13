namespace JustCompute.Features.SunEclipses;

public static class SunEclipsesFeature
{
    public static MauiAppBuilder AddSunEclipsesFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SunEclipsesPage>();
        builder.Services.AddSingleton<SunEclipsesViewModel>();
        return builder;
    }
}
