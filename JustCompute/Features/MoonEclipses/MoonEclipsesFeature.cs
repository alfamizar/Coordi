namespace JustCompute.Features.MoonEclipses;

public static class MoonEclipsesFeature
{
    public static MauiAppBuilder AddMoonEclipsesFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MoonEclipsesPage>();
        builder.Services.AddSingleton<MoonEclipsesViewModel>();
        return builder;
    }
}
