using JustCompute.Shared.Abstractions.UI;
using JustCompute.Shared.Theming;

namespace JustCompute.Shared.Helpers;

/// <summary>
/// Applies the chosen theme.
///
/// The app offers more themes than the two the platform knows about, so the palette cannot be
/// carried by <c>AppThemeBinding</c> alone. Instead each palette is written into the app's
/// resource dictionary under the <c>Theme*</c> keys, which the XAML reaches through
/// <c>DynamicResource</c> — so a change reaches the live visual tree instead of needing the pages
/// rebuilt. <c>UserAppTheme</c> is still set from the palette's light/dark side, which keeps the
/// neutral greys and the light/dark icon variants in step.
/// </summary>
public class ThemeHandler(IEnvironment environment)
{
    private readonly IEnvironment _environment = environment;
    private bool _followingDevice;

    public void SetTheme()
    {
        var app = Application.Current;
        if (app is null) return;

        AppThemeId chosen = Settings.ThemeId;
        ThemePalette palette = AppThemes.For(chosen, app.RequestedTheme);

        foreach (var (key, color) in palette.ResourceEntries())
        {
            app.Resources[key] = color;
        }

        // Set after the resources: this raises the theme-changed cascade, and the bindings should
        // find the new palette already in place when it does.
        app.UserAppTheme = palette.IsDark ? AppTheme.Dark : AppTheme.Light;

        _environment.SetNavigationBarColor(palette.NavigationBar.ColorMauiToSystem());

        // The status bar is painted by the shell, which now reads the same slot — nudge it so a
        // theme change reaches the bar and not just the page.
        AppShell.SetStatusBarTheme();

        FollowDeviceThemeWhile(chosen == AppThemeId.System, app);
    }

    /// <summary>
    /// "System default" has to keep tracking the device after it is chosen — the user may flip
    /// their phone to dark hours later, with the app still running.
    /// </summary>
    private void FollowDeviceThemeWhile(bool follow, Application app)
    {
        if (follow == _followingDevice) return;

        if (follow)
        {
            app.RequestedThemeChanged += OnDeviceThemeChanged;
        }
        else
        {
            app.RequestedThemeChanged -= OnDeviceThemeChanged;
        }

        _followingDevice = follow;
    }

    private void OnDeviceThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (Settings.ThemeId != AppThemeId.System) return;

        MainThread.BeginInvokeOnMainThread(SetTheme);
    }
}
