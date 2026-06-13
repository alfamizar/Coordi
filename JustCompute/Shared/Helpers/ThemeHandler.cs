using Compute.Core.UI;

namespace JustCompute.Shared.Helpers;

public class ThemeHandler(IEnvironment environment)
{
    private readonly IEnvironment _environment = environment;

    public void SetTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        var preference = Settings.Theme;
        app.UserAppTheme = preference;

        AppTheme resolved = preference == AppTheme.Unspecified
            ? app.RequestedTheme
            : preference;

        switch (resolved)
        {
            case AppTheme.Light:
                SetNavigationBarColor("LightNavigationBarColor");
                break;
            case AppTheme.Dark:
            default:
                SetNavigationBarColor("DarkNavigationBarColor");
                break;
        }
    }

    private void SetNavigationBarColor(string? color)
    {
        if (color == null)
        {
            _environment.ResetNavigationBarColor();
            return;
        }

        if (Application.Current?.Resources.TryGetValue(color, out var navigationBarColor) == true)
        {
            var mauiColor = (Color)navigationBarColor;
            _environment.SetNavigationBarColor(mauiColor.ColorMauiToSystem());
        }
    }
}
