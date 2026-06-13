namespace JustCompute.Features.Settings;

public static class SettingsFeature
{
    public static MauiAppBuilder AddSettingsFeature(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        return builder;
    }
}
