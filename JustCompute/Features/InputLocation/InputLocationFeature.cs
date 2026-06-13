namespace JustCompute.Features.InputLocation;

public static class InputLocationFeature
{
    public static MauiAppBuilder AddInputLocationFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<InputLocationPage>();
        builder.Services.AddTransient<InputLocationViewModel>();
        return builder;
    }
}
