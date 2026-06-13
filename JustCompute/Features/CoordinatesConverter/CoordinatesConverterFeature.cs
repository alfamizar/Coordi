namespace JustCompute.Features.CoordinatesConverter;

public static class CoordinatesConverterFeature
{
    public static MauiAppBuilder AddCoordinatesConverterFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<CoordinatesConverterPage>();
        builder.Services.AddSingleton<CoordinatesConverterViewModel>();
        return builder;
    }
}
