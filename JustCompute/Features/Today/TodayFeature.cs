namespace JustCompute.Features.Today;

public static class TodayFeature
{
    public static MauiAppBuilder AddTodayFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<TodayPage>();
        builder.Services.AddSingleton<TodayViewModel>();
        return builder;
    }
}
