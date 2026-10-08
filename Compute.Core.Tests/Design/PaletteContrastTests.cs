using System.Globalization;
using System.Text.RegularExpressions;

namespace Compute.Core.Tests.Design
{
    /// <summary>
    /// Every palette meets WCAG AA for the text the app actually writes on it.
    ///
    /// A colour is chosen once and then trusted by every screen, and contrast is the one property
    /// of it nobody can judge by eye on their own monitor: Ocean shipped with white button labels at
    /// 2.6:1 and Midnight at 3.8:1, both under the 4.5:1 normal text needs. The pairs below are the
    /// ones the styles use, so a new palette, or a changed colour, has to clear all of them.
    /// </summary>
    public class PaletteContrastTests
    {
        private static readonly string RepoRoot = LocateRepoRoot();

        private static string AppThemesPath => Path.Combine(RepoRoot, "JustCompute", "Shared", "Theming", "AppThemes.cs");

        private static string ColorsPath => Path.Combine(RepoRoot, "JustCompute", "Resources", "Styles", "Colors.xaml");

        private static string VerdictConverterPath => Path.Combine(RepoRoot, "JustCompute", "Shared", "Converters", "SkyVerdictConverter.cs");

        /// <summary>Foreground on background, the least contrast allowed, and where the pair is used.
        /// A name is a palette slot; a # value is a colour the styles fix for every palette.</summary>
        private static readonly (string Fore, string Back, double Minimum, string Use)[] Pairs =
        [
            ("OnPrimary", "Primary", 4.5, "button labels, the selected segment"),
            ("#FFFFFF", "PrimaryVariant", 4.5, "the menu's selected row"),
            ("OnSecondary", "Secondary", 4.5, "the weather strip"),
            ("OnBackground", "Background", 4.5, "text on the page"),
            ("OnBackground", "Surface", 4.5, "text in a card"),
            ("OnSurface", "Surface", 4.5, "text in a card"),
            ("OnBackground", "MutedSurface", 4.5, "a selected row's text"),
            ("#FFFFFF", "HeaderBackground", 4.5, "the title bar and the menu"),
            ("AccentStrong", "Surface", 4.5, "accent text in a card"),
            ("AccentStrong", "Background", 4.5, "accent text on the page"),
            ("Error", "Surface", 4.5, "error text in a card"),
            ("Error", "Background", 4.5, "error text on the page"),
            // Not text: WCAG asks 3:1 of the outline that shows a control or a selection.
            ("Primary", "Surface", 3.0, "outlines and checkboxes in a card"),
            ("Primary", "Background", 3.0, "outlines and checkboxes on the page"),
        ];

        public static IEnumerable<object[]> PaletteNames() =>
            Palettes().Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => new object[] { k });

        [Fact]
        public void Palettes_AreFound()
        {
            Assert.Equal(["Blossom", "Ember", "Midnight", "Ocean"], Palettes().Keys.OrderBy(k => k, StringComparer.Ordinal));
        }

        [Theory]
        [MemberData(nameof(PaletteNames))]
        public void Palette_MeetsWcagAaForWhatIsWrittenOnIt(string palette)
        {
            var slots = Palettes()[palette];
            var failures = new List<string>();

            foreach (var (fore, back, minimum, use) in Pairs)
            {
                var foreground = fore.StartsWith('#') ? fore : slots[fore];
                var background = slots[back];
                var ratio = Contrast(foreground, background);
                if (ratio < minimum)
                {
                    failures.Add($"  {fore} {foreground} on {back} {background}: {ratio:F2}:1, needs {minimum}:1 ({use})");
                }
            }

            Assert.True(failures.Count == 0, $"{palette} fails contrast:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        /// <summary>
        /// The tab bar on Android is the header's colour, and its unselected tabs are white on the
        /// light palettes and Gray300 on the dark ones (Styles.xaml's TabBarUnselectedColor). The
        /// white is covered by the header pair above; this is the gray. The selected tab is shown
        /// by a pill, not a shade, so both have to read in full.
        /// </summary>
        [Theory]
        [MemberData(nameof(PaletteNames))]
        public void UnselectedTabs_ReadOnTheDarkTabBar(string palette)
        {
            var slots = Palettes()[palette];
            if (!IsDark(palette))
            {
                return;
            }

            var ratio = Contrast("#ACACAC", slots["HeaderBackground"]);
            Assert.True(ratio >= 4.5, $"{palette}: Gray300 on the tab bar {slots["HeaderBackground"]} is {ratio:F2}:1, needs 4.5:1");
        }

        /// <summary>
        /// Colors.xaml carries the Theme* slots as parse-time defaults until a palette is applied,
        /// and says they are Ocean. When they drift, the first frame of the app is a palette nobody
        /// chose.
        /// </summary>
        [Fact]
        public void ParseTimeDefaults_AreTheOceanPalette()
        {
            var ocean = Palettes()["Ocean"];
            var defaults = Regex.Matches(File.ReadAllText(ColorsPath), "<Color x:Key=\"Theme(?<slot>\\w+)\">(?<hex>#[0-9A-Fa-f]{6,8})</Color>")
                .ToDictionary(m => m.Groups["slot"].Value, m => m.Groups["hex"].Value);

            Assert.NotEmpty(defaults);
            var different = defaults
                .Where(d => !string.Equals(Normalize(d.Value), Normalize(ocean[d.Key]), StringComparison.OrdinalIgnoreCase))
                .Select(d => $"  Theme{d.Key}: {d.Value}, Ocean has {ocean[d.Key]}")
                .ToList();

            Assert.True(different.Count == 0, $"Colors.xaml's defaults are not Ocean:{Environment.NewLine}{string.Join(Environment.NewLine, different)}");
        }

        /// <summary>
        /// The sky banner on Today carries its own colours, whatever the palette, with white text:
        /// the verdict in full and the lines under it dimmed to 80%. Both have to read.
        /// </summary>
        [Fact]
        public void SkyBanners_KeepTheirDimmedLinesReadable()
        {
            var colours = Regex.Matches(File.ReadAllText(VerdictConverterPath), "Color\\.FromArgb\\(\"(?<hex>#[0-9A-Fa-f]{6})\"\\)")
                .Select(m => m.Groups["hex"].Value)
                .Distinct()
                .ToList();

            Assert.NotEmpty(colours);
            var failures = colours
                .Select(c => (Colour: c, Ratio: Contrast(Blend("#FFFFFF", c, 0.8), c)))
                .Where(x => x.Ratio < 4.5)
                .Select(x => $"  80% white on {x.Colour}: {x.Ratio:F2}:1")
                .ToList();

            Assert.True(failures.Count == 0, $"Banner text fails contrast:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        /// <summary>The palettes in AppThemes.cs, slot name to #RRGGBB.</summary>
        private static Dictionary<string, Dictionary<string, string>> Palettes()
        {
            var source = Regex.Replace(File.ReadAllText(AppThemesPath), "//[^\n]*", string.Empty);
            return Regex.Matches(source, "ThemePalette (?<name>\\w+) = new\\((?<body>.*?)\\);", RegexOptions.Singleline)
                .ToDictionary(
                    m => m.Groups["name"].Value,
                    m => Regex.Matches(m.Groups["body"].Value, "(?<slot>\\w+): (?:Colors\\.(?<named>White|Black)|Color\\.FromArgb\\(\"(?<hex>#[0-9A-Fa-f]{6,8})\"\\))")
                        .ToDictionary(
                            s => s.Groups["slot"].Value,
                            s => s.Groups["named"].Success
                                ? (s.Groups["named"].Value == "White" ? "#FFFFFF" : "#000000")
                                : s.Groups["hex"].Value));
        }

        private static bool IsDark(string palette)
        {
            var source = File.ReadAllText(AppThemesPath);
            var body = Regex.Match(source, "ThemePalette " + palette + " = new\\((?<body>.*?)\\);", RegexOptions.Singleline).Groups["body"].Value;
            return Regex.IsMatch(body, "IsDark: true");
        }

        /// <summary>#AARRGGBB to #RRGGBB, the form both files use for an opaque colour.</summary>
        private static string Normalize(string hex) => hex.Length == 9 ? "#" + hex[3..] : hex;

        /// <summary>WCAG 2 contrast ratio, (L1 + 0.05) / (L2 + 0.05) with L1 the lighter.</summary>
        private static double Contrast(string a, string b)
        {
            var la = RelativeLuminance(a);
            var lb = RelativeLuminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double RelativeLuminance(string hex)
        {
            var (r, g, b) = Channels(hex);
            return 0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);
        }

        private static double Linear(int channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        private static (int R, int G, int B) Channels(string hex)
        {
            var h = Normalize(hex).TrimStart('#');
            return (int.Parse(h[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(h[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(h[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        /// <summary><paramref name="fore"/> at <paramref name="opacity"/> over <paramref name="back"/>.</summary>
        private static string Blend(string fore, string back, double opacity)
        {
            var f = Channels(fore);
            var b = Channels(back);
            int Mix(int x, int y) => (int)Math.Round(opacity * x + (1 - opacity) * y);
            return $"#{Mix(f.R, b.R):X2}{Mix(f.G, b.G):X2}{Mix(f.B, b.B):X2}";
        }

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
    }
}
