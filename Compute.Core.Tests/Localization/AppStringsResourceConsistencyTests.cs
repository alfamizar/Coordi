using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Compute.Core.Tests.Localization
{
    /// <summary>
    /// Guards the app's .resx localization files. For every satellite language
    /// (AppStringsRes.&lt;culture&gt;.resx) it verifies, against the English base
    /// (AppStringsRes.resx):
    ///   1. identical key set (nothing missing, nothing extra);
    ///   2. no empty/whitespace-only values;
    ///   3. format placeholders ({0}, {0:dd MMMM yyyy}, {0:N0}, {1}, ...) match the base exactly.
    /// New languages are picked up automatically (the satellite files are discovered on disk),
    /// so adding AppStringsRes.&lt;new&gt;.resx requires no change here.
    /// </summary>
    public class AppStringsResourceConsistencyTests
    {
        private static readonly string StringsDir = LocateStringsDir();
        private static readonly string BasePath = Path.Combine(StringsDir, "AppStringsRes.resx");

        private static string LocateStringsDir()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JustCompute.sln")))
            {
                dir = dir.Parent;
            }

            if (dir is null)
            {
                throw new DirectoryNotFoundException(
                    $"Could not locate repo root (JustCompute.sln) starting from {AppContext.BaseDirectory}");
            }

            string path = Path.Combine(dir.FullName, "JustCompute", "Resources", "Strings");
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Localization folder not found: {path}");
            }

            return path;
        }

        private static Dictionary<string, string> Load(string path)
        {
            XDocument doc = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            // Only real <data> elements are returned. The schema-example entries
            // (Name1/Color1/...) live inside an XML comment and are not elements.
            return doc.Root!
                .Elements("data")
                .ToDictionary(
                    d => d.Attribute("name")!.Value,
                    d => d.Element("value")?.Value ?? string.Empty);
        }

        private static readonly Regex PlaceholderRegex = new(@"\{[^{}]*\}", RegexOptions.Compiled);

        private static List<string> Placeholders(string value) =>
            PlaceholderRegex.Matches(value)
                .Select(m => m.Value)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

        private static string CultureOf(string file)
        {
            string name = Path.GetFileName(file);            // AppStringsRes.<culture>.resx
            return name["AppStringsRes.".Length..^".resx".Length];
        }

        public static IEnumerable<object[]> SatelliteCultures()
        {
            foreach (string file in Directory.GetFiles(StringsDir, "AppStringsRes.*.resx").OrderBy(f => f))
            {
                yield return new object[] { CultureOf(file) };
            }
        }

        [Fact]
        public void BaseResource_Exists_AndHasKeys()
        {
            Assert.True(File.Exists(BasePath), $"Base resource not found: {BasePath}");
            Assert.NotEmpty(Load(BasePath));
        }

        [Fact]
        public void AtLeastOneSatelliteLanguage_Exists()
        {
            Assert.NotEmpty(SatelliteCultures());
        }

        [Theory]
        [MemberData(nameof(SatelliteCultures))]
        public void Culture_HasSameKeysAsBase(string culture)
        {
            HashSet<string> baseKeys = Load(BasePath).Keys.ToHashSet();
            HashSet<string> locKeys = Load(Path.Combine(StringsDir, $"AppStringsRes.{culture}.resx")).Keys.ToHashSet();

            List<string> missing = baseKeys.Except(locKeys).OrderBy(k => k).ToList();
            List<string> extra = locKeys.Except(baseKeys).OrderBy(k => k).ToList();

            Assert.True(missing.Count == 0, $"[{culture}] missing keys: {string.Join(", ", missing)}");
            Assert.True(extra.Count == 0, $"[{culture}] unexpected keys: {string.Join(", ", extra)}");
        }

        [Theory]
        [MemberData(nameof(SatelliteCultures))]
        public void Culture_HasNoEmptyValues(string culture)
        {
            Dictionary<string, string> loc = Load(Path.Combine(StringsDir, $"AppStringsRes.{culture}.resx"));
            List<string> empty = loc.Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                                    .Select(kv => kv.Key)
                                    .OrderBy(k => k)
                                    .ToList();
            Assert.True(empty.Count == 0, $"[{culture}] empty values for keys: {string.Join(", ", empty)}");
        }

        [Theory]
        [MemberData(nameof(SatelliteCultures))]
        public void Culture_PreservesFormatPlaceholders(string culture)
        {
            Dictionary<string, string> baseRes = Load(BasePath);
            Dictionary<string, string> loc = Load(Path.Combine(StringsDir, $"AppStringsRes.{culture}.resx"));

            List<string> mismatches = new();
            foreach ((string key, string baseValue) in baseRes)
            {
                if (!loc.TryGetValue(key, out string? locValue))
                {
                    continue; // key-parity is covered by Culture_HasSameKeysAsBase
                }

                List<string> basePlaceholders = Placeholders(baseValue);
                List<string> locPlaceholders = Placeholders(locValue);

                if (!basePlaceholders.SequenceEqual(locPlaceholders, StringComparer.Ordinal))
                {
                    mismatches.Add(
                        $"  {key}: base [{string.Join(" ", basePlaceholders)}] != {culture} [{string.Join(" ", locPlaceholders)}]");
                }
            }

            Assert.True(
                mismatches.Count == 0,
                $"[{culture}] format-placeholder mismatches (.NET specifiers must stay verbatim, e.g. yyyy):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }
    }
}
