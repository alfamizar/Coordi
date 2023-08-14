using Compute.Core.Helpers.UI;

namespace JustCompute.Presentation.Helpers;

public class ThemeHandler
{
    private readonly IEnvironment _environment;

    public ThemeHandler(IEnvironment environment)
    {
        _environment = environment;
    }

    public void SetTheme()
    {   
        // todo: prepare light theme color palette and allow to set light theme
        //Application.Current.UserAppTheme = currentSystemTheme;
        Application.Current.UserAppTheme = AppTheme.Dark; // delete when light theme will be implemented
        AppTheme currentSystemTheme = Application.Current.RequestedTheme;
        switch (currentSystemTheme)
        {
            case AppTheme.Light:
                SetStatusBarColor("LightStatusBarColor");
                SetNavigationBarColor(null);
                break;
            case AppTheme.Dark:
                SetStatusBarColor("DarkStatusBarColor");
                SetNavigationBarColor("DarkNavigationBarColor");
                break;
        }

        // todo: support theme switching and saving choosen theme
        /*switch (Settings.Theme)
        {
            default:
            case AppTheme.Light:
                Application.Current.UserAppTheme = AppTheme.Light;
                SetStatusBarColor("LightStatusBarColor");
                break;
            case AppTheme.Dark:
                Application.Current.UserAppTheme = AppTheme.Dark;
                SetStatusBarColor("DarkStatusBarColor");
                break;
        }*/
    }

    private void SetStatusBarColor(string color)
    {
        if (Application.Current.Resources.TryGetValue(color, out var statusBarColor))
        {
            var mauiColor = (Color)statusBarColor;
            _environment?.SetStatusBarColor(mauiColor.ColorMauiToSystem(), false);
        }
    }

    private void SetNavigationBarColor(string color)
    {
        if (color == null)
        {
            _environment?.ResetNavigationBarColor();
            return;
        }

        if (Application.Current.Resources.TryGetValue(color, out var navigationBarColor))
        {
            var mauiColor = (Color)navigationBarColor;
            _environment?.SetNavigationBarColor(mauiColor.ColorMauiToSystem());
        }
    }
}
