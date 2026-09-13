namespace JustCompute.Features.SunEclipses;

public static class SunEclipsesFeature
{
    public static MauiAppBuilder AddSunEclipsesFeature(this MauiAppBuilder builder)
    {
        // Transient, unlike the view model it binds to. These two are the only pages
        // realised as sibling ShellContents under one FlyoutItem, so switching tabs
        // re-realises them while ResetActiveTabToRoot removes pages underneath. Handing
        // Shell the same Page instance each time leaves an earlier, disconnected handler
        // owning the views on screen — they animate and lay out but never see another
        // property change. The view model stays a singleton, so nothing is recomputed.
        builder.Services.AddTransient<SunEclipsesPage>();
        builder.Services.AddSingleton<SunEclipsesViewModel>();
        return builder;
    }
}
