namespace JustCompute.Features.SkyChart;

public static class SkyChartFeature
{
    public static MauiAppBuilder AddSkyChartFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SkyChartPage>();
        builder.Services.AddSingleton<SkyChartViewModel>();
        return builder;
    }
}
