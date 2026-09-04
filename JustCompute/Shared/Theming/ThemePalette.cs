namespace JustCompute.Shared.Theming
{
    /// <summary>
    /// One complete set of colours. Applied into the app's resource dictionary under the
    /// <c>Theme*</c> keys, which the XAML reaches through <c>DynamicResource</c> so a change
    /// takes effect without rebuilding the page.
    /// </summary>
    /// <param name="IsDark">
    /// Which side of the light/dark divide this palette sits on. It drives
    /// <c>Application.UserAppTheme</c>, so the neutral greys and the light/dark icon variants —
    /// which are still plain <c>AppThemeBinding</c>s — follow the chosen theme rather than the
    /// device.
    /// </param>
    public sealed record ThemePalette(
        bool IsDark,
        Color Primary,
        Color OnPrimary,
        Color PrimaryVariant,
        Color Secondary,
        Color OnSecondary,
        Color Background,
        Color OnBackground,
        Color Surface,
        Color OnSurface,
        Color Outline,
        Color Error,
        Color OnError,
        // App bar and flyout header. Light themes tint it with the primary; dark ones keep it
        // a surface, so the chrome stays quiet against a dark page.
        Color HeaderBackground,
        // A filled band that must stay legible without being the primary — the weather strip,
        // a switch track.
        Color MutedSurface,
        // A heavier accent for pressed and selected states.
        Color AccentStrong)
    {
        public Color StatusBar => HeaderBackground;

        public Color NavigationBar => HeaderBackground;
    }
}
