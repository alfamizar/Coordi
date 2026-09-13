namespace JustCompute.Features.Planets;

public static class PlanetsFeature
{
    public static MauiAppBuilder AddPlanetsFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<PlanetsPage>();
        builder.Services.AddSingleton<PlanetsViewModel>();
        return builder;
    }
}
