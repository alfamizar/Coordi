namespace JustCompute.Features.Sun;

public static class SunFeature
{
    public static MauiAppBuilder AddSunFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SunPage>();
        builder.Services.AddSingleton<SunViewModel>();
        return builder;
    }
}
