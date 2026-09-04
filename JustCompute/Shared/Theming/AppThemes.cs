namespace JustCompute.Shared.Theming
{
    /// <summary>The palettes behind <see cref="AppThemeId"/>.</summary>
    public static class AppThemes
    {
        /// <summary>
        /// The app's original light palette. Kept faithfully, including a primary that only
        /// reaches 2.6:1 against the white text on it — changing that is a design decision, not
        /// something to slip into a restore.
        /// </summary>
        public static readonly ThemePalette Ocean = new(
            IsDark: false,
            Primary: Color.FromArgb("#00A9FF"),
            OnPrimary: Colors.White,
            PrimaryVariant: Color.FromArgb("#0086CC"),
            Secondary: Color.FromArgb("#89CFF3"),
            OnSecondary: Colors.Black,
            Background: Colors.White,
            OnBackground: Color.FromArgb("#212121"),
            Surface: Color.FromArgb("#CDF5FD"),
            OnSurface: Color.FromArgb("#212121"),
            Outline: Color.FromArgb("#89CFF3"),
            Error: Color.FromArgb("#B00020"),
            OnError: Colors.White,
            HeaderBackground: Color.FromArgb("#00A9FF"),
            MutedSurface: Color.FromArgb("#89CFF3"),
            AccentStrong: Color.FromArgb("#0086CC"));

        /// <summary>
        /// Rose pink over mint. The primary is deeper than a pastel would be on purpose: buttons
        /// put white text on it at 14pt, which needs 4.5:1, and this clears it at 4.60.
        /// </summary>
        public static readonly ThemePalette Blossom = new(
            IsDark: false,
            Primary: Color.FromArgb("#DB2777"),
            OnPrimary: Colors.White,
            PrimaryVariant: Color.FromArgb("#A81B5C"),
            Secondary: Color.FromArgb("#A5D6A7"),
            OnSecondary: Color.FromArgb("#14321A"),
            Background: Color.FromArgb("#FFF7FB"),
            OnBackground: Color.FromArgb("#212121"),
            Surface: Color.FromArgb("#E4F7E2"),
            OnSurface: Color.FromArgb("#212121"),
            Outline: Color.FromArgb("#8FCB92"),
            Error: Color.FromArgb("#B00020"),
            OnError: Colors.White,
            HeaderBackground: Color.FromArgb("#DB2777"),
            MutedSurface: Color.FromArgb("#A5D6A7"),
            AccentStrong: Color.FromArgb("#A81B5C"));

        /// <summary>The app's original dark palette: crimson and amber on black.</summary>
        public static readonly ThemePalette Midnight = new(
            IsDark: true,
            Primary: Color.FromArgb("#FF1E56"),
            OnPrimary: Colors.White,
            PrimaryVariant: Color.FromArgb("#CC1845"),
            Secondary: Color.FromArgb("#FFAC41"),
            OnSecondary: Colors.Black,
            Background: Colors.Black,
            OnBackground: Colors.White,
            Surface: Color.FromArgb("#323232"),
            OnSurface: Colors.White,
            Outline: Color.FromArgb("#4A4A4A"),
            Error: Color.FromArgb("#CF6679"),
            OnError: Colors.Black,
            HeaderBackground: Color.FromArgb("#323232"),
            MutedSurface: Color.FromArgb("#404040"),
            AccentStrong: Color.FromArgb("#FF1E56"));

        /// <summary>
        /// Orange on near-black, following Penombre's dark orange scheme (#FF9500 over the
        /// #121316/#1C1C1E surface stack).
        ///
        /// Its on-primary is near-black rather than white: white on this orange is 2.1:1, which
        /// fails outright, while the dark text reads at 8.9:1. The chrome stays a dark surface —
        /// an orange app bar would drown the accent it is supposed to be.
        /// </summary>
        public static readonly ThemePalette Ember = new(
            IsDark: true,
            Primary: Color.FromArgb("#FF9500"),
            OnPrimary: Color.FromArgb("#1C1C1E"),
            PrimaryVariant: Color.FromArgb("#FF8800"),
            Secondary: Color.FromArgb("#2C2C2E"),
            OnSecondary: Colors.White,
            Background: Color.FromArgb("#121316"),
            OnBackground: Colors.White,
            Surface: Color.FromArgb("#1C1C1E"),
            OnSurface: Colors.White,
            Outline: Color.FromArgb("#2C2C2E"),
            Error: Color.FromArgb("#FF3B30"),
            OnError: Colors.White,
            HeaderBackground: Color.FromArgb("#1C1C1E"),
            MutedSurface: Color.FromArgb("#2C2C2E"),
            AccentStrong: Color.FromArgb("#FF8800"));

        /// <summary>
        /// The palette for a choice. <see cref="AppThemeId.System"/> resolves against the device's
        /// current setting, which is why it needs it passed in.
        /// </summary>
        public static ThemePalette For(AppThemeId id, AppTheme deviceTheme) => id switch
        {
            AppThemeId.Ocean => Ocean,
            AppThemeId.Blossom => Blossom,
            AppThemeId.Midnight => Midnight,
            AppThemeId.Ember => Ember,
            _ => deviceTheme == AppTheme.Dark ? Midnight : Ocean,
        };

        /// <summary>The resource keys the XAML binds to, paired with this palette's colours.</summary>
        public static IEnumerable<KeyValuePair<string, Color>> ResourceEntries(this ThemePalette p)
        {
            yield return new("ThemePrimary", p.Primary);
            yield return new("ThemeOnPrimary", p.OnPrimary);
            yield return new("ThemePrimaryVariant", p.PrimaryVariant);
            yield return new("ThemeSecondary", p.Secondary);
            yield return new("ThemeOnSecondary", p.OnSecondary);
            yield return new("ThemeBackground", p.Background);
            yield return new("ThemeOnBackground", p.OnBackground);
            yield return new("ThemeSurface", p.Surface);
            yield return new("ThemeOnSurface", p.OnSurface);
            yield return new("ThemeOutline", p.Outline);
            yield return new("ThemeError", p.Error);
            yield return new("ThemeOnError", p.OnError);
            yield return new("ThemeHeaderBackground", p.HeaderBackground);
            yield return new("ThemeMutedSurface", p.MutedSurface);
            yield return new("ThemeAccentStrong", p.AccentStrong);
        }
    }
}
