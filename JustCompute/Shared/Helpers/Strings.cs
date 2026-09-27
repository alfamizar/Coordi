using System.Globalization;
using JustCompute.Resources.Strings;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// Localized text for code that XAML constructs itself: value converters and markup
    /// extensions, which cannot be handed an IStringLocalizer through a constructor.
    ///
    /// They used to fetch one from the service container, which made every converter depend on
    /// the container existing before XAML first touched it — and one resolved it in a static
    /// initializer, which throws if the type is loaded early. The strings are static resources,
    /// not a service, so this reads them where they live. AddLocalization builds its resource
    /// manager from this same base name, so the text is identical to what view models get.
    /// </summary>
    public static class Strings
    {
        /// <summary>The string for <paramref name="key"/> in the current UI culture, or the key itself when there is none.</summary>
        public static string Get(string key) =>
            AppStringsRes.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
