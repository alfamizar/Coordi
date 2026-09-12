namespace JustCompute.Features.Converter;

public static class ConverterFeature
{
    public static MauiAppBuilder AddConverterFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<ConverterPage>();
        builder.Services.AddSingleton<ConverterViewModel>();
        return builder;
    }
}
