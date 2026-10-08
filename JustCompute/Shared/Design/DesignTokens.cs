namespace JustCompute.Shared.Design
{
    /// <summary>
    /// The sizes of Styles.xaml, for views built in code. Read from the app's resources rather
    /// than repeated here, so the stylesheet stays the one place the numbers are written; a key
    /// it does not define throws, where a dynamic resource would silently leave the default.
    /// </summary>
    public static class DesignTokens
    {
        /// <summary>A step of the spacing scale; <see cref="Space.Zero"/> and
        /// <see cref="Space.Unset"/> are nothing.</summary>
        public static double Spacing(Space step) =>
            step is Space.Unset or Space.Zero ? 0 : Size("Spacing" + step);

        /// <summary>Any size the stylesheet defines, by key: <c>Size("FontSizeMicro")</c>.</summary>
        public static double Size(string key) =>
            Application.Current?.Resources.TryGetValue(key, out var value) == true && value is double size
                ? size
                : throw new InvalidOperationException($"Styles.xaml defines no x:Double called {key}.");
    }
}
