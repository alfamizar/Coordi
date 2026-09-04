using System.Globalization;

namespace Compute.Core.Tests.Localization
{
    /// <summary>
    /// Ties the Play Store listing to the app's own languages.
    ///
    /// A localized store listing that leads to an English app is a worse experience than no
    /// listing at all: the user is told, in their language, that the app speaks it. So every
    /// locale published under fastlane/metadata/android must resolve — through the normal
    /// culture fallback chain — to a satellite resx the app actually ships.
    ///
    /// English is the exception and needs no satellite: it is the base resource.
    /// </summary>
    public class StoreLocaleCoverageTests
    {
        private static readonly string RepoRoot = LocateRepoRoot();

        private static string LocateRepoRoot()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JustCompute.sln")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName
                ?? throw new DirectoryNotFoundException(
                    $"Could not locate repo root (JustCompute.sln) starting from {AppContext.BaseDirectory}");
        }

        private static string MetadataDir =>
            Path.Combine(RepoRoot, "fastlane", "metadata", "android");

        private static string StringsDir =>
            Path.Combine(RepoRoot, "JustCompute", "Resources", "Strings");

        /// <summary>Cultures with an AppStringsRes.&lt;culture&gt;.resx on disk.</summary>
        private static HashSet<string> ShippedCultures() =>
            Directory.GetFiles(StringsDir, "AppStringsRes.*.resx")
                .Select(f => Path.GetFileName(f)["AppStringsRes.".Length..^".resx".Length])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Walks a locale up its parents the way .NET resolves resources: zh-TW resolves to a
        /// zh-Hant satellite, es-419 to neutral es, and so on.
        /// </summary>
        private static string? ResolveSatellite(string locale, HashSet<string> shipped)
        {
            for (CultureInfo? c = new(locale); c is not null && !string.IsNullOrEmpty(c.Name); c = c.Parent)
            {
                if (shipped.Contains(c.Name))
                {
                    return c.Name;
                }
            }

            return null;
        }

        public static IEnumerable<object[]> StoreLocales()
        {
            foreach (string dir in Directory.GetDirectories(MetadataDir).OrderBy(d => d))
            {
                yield return new object[] { Path.GetFileName(dir) };
            }
        }

        [Fact]
        public void StoreListing_HasLocales()
        {
            Assert.True(Directory.Exists(MetadataDir), $"Store metadata not found: {MetadataDir}");
            Assert.NotEmpty(Directory.GetDirectories(MetadataDir));
        }

        [Theory]
        [MemberData(nameof(StoreLocales))]
        public void EveryStoreLocale_IsSpokenByTheApp(string locale)
        {
            // The base resource is English, so English listings need no satellite.
            if (locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string? satellite = ResolveSatellite(locale, ShippedCultures());

            Assert.True(
                satellite is not null,
                $"Play Store locale '{locale}' has a localized listing but the app has no matching " +
                $"AppStringsRes.<culture>.resx, so those users would install an English app. " +
                $"Add a satellite for '{locale}' or one of its parent cultures, or drop the listing.");
        }
    }
}
