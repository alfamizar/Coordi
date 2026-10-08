using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Compute.Core.Tests.Design
{
    /// <summary>
    /// Keeps every screen on the design system in Resources/Styles/Styles.xaml.
    ///
    /// The scale is there so that all screens space, set type and round corners alike. A screen
    /// that writes Padding="6" is where the drift starts, and the next screen copies it; that is
    /// how the app came to have corners of 5, 8, 10, 12 and 25. So outside the stylesheet,
    /// spacing, type and corners are written as tokens - a StaticResource, a
    /// <c>{design:Inset ...}</c> or a keyed style - and the only number allowed is zero. The
    /// stylesheet keeps its spacing on the 4 grid, and every touch minimum is the one token.
    ///
    /// The size of a thing - a 10 dot, a 1 rule, a 220 popup - is not spacing, and stays a number.
    /// </summary>
    public class DesignTokenUsageTests
    {
        private static readonly string RepoRoot = LocateRepoRoot();

        private static string AppDir => Path.Combine(RepoRoot, "JustCompute");

        private static string StylesDir => Path.Combine(AppDir, "Resources", "Styles");

        private static string StylesPath => Path.Combine(StylesDir, "Styles.xaml");

        /// <summary>What the scale is made of: spacing, type and corners.</summary>
        private static readonly HashSet<string> ScaleProperties = new(StringComparer.Ordinal)
        {
            "Padding", "Margin", "Spacing", "RowSpacing", "ColumnSpacing", "ItemSpacing",
            "HorizontalItemSpacing", "VerticalItemSpacing", "FontSize", "CornerRadius", "StrokeShape",
        };

        private static readonly HashSet<string> SpacingProperties = new(StringComparer.Ordinal)
        {
            "Padding", "Margin", "Spacing", "RowSpacing", "ColumnSpacing",
        };

        private static readonly HashSet<string> SizeProperties = new(StringComparer.Ordinal)
        {
            "WidthRequest", "HeightRequest", "MinimumWidthRequest", "MinimumHeightRequest",
        };

        // "16", "0,8", "12, 8, 12, 0", "RoundRectangle 10": a value written as numbers instead
        // of taken from a resource. Anything in braces is a markup extension and passes.
        private static readonly Regex Literal =
            new(@"^(RoundRectangle\s+)?-?\d+(\.\d+)?(\s*,\s*-?\d+(\.\d+)?)*$", RegexOptions.CultureInvariant);

        private static readonly Regex Number = new(@"(?<![\w.])\d+(\.\d+)?", RegexOptions.CultureInvariant);

        // A view property given a number in code. Text drawn on a canvas is part of a picture
        // and is sized with it, so canvas.FontSize is not a view's type.
        private static readonly Regex CodeAssignment = new(
            @"(?<!canvas\.)\b(FontSize|Spacing|RowSpacing|ColumnSpacing|Padding|Margin)\s*=\s*(?<value>-?\d+(\.\d+)?)[fdm]?\b",
            RegexOptions.CultureInvariant);

        private static readonly Regex CodeThickness = new(@"new\s+Thickness\((?<args>[^;]*?)\)\s*[,;}]", RegexOptions.CultureInvariant);

        public static IEnumerable<object[]> ScreenFiles() =>
            Directory.EnumerateFiles(AppDir, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !IsBuildOutput(f) && !f.StartsWith(StylesDir + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(f => Path.GetRelativePath(AppDir, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .Select(f => new object[] { f });

        [Fact]
        public void Screens_AreFound()
        {
            Assert.True(File.Exists(StylesPath), $"Stylesheet not found: {StylesPath}");
            Assert.Contains(ScreenFiles(), f => (string)f[0] == Path.Combine("Features", "Today", "TodayPage.xaml"));
        }

        [Theory]
        [MemberData(nameof(ScreenFiles))]
        public void Screen_TakesSpacingTypeAndCornersFromTheScale(string file)
        {
            var found = new List<string>();

            foreach (var element in Load(Path.Combine(AppDir, file)).Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (ScaleProperties.Contains(attribute.Name.LocalName) && IsNonZeroLiteral(attribute.Value))
                    {
                        found.Add($"  line {Line(attribute)}: {element.Name.LocalName} {attribute.Name.LocalName}=\"{attribute.Value}\"");
                    }
                }

                if (SetterOf(element) is ({ } property, { } value) && ScaleProperties.Contains(property) && IsNonZeroLiteral(value))
                {
                    found.Add($"  line {Line(element)}: Setter {property}=\"{value}\"");
                }
            }

            Assert.True(found.Count == 0,
                $"{file} writes the scale as numbers; use a StaticResource from Styles.xaml, " +
                $"{{design:Inset ...}} or a keyed style:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
        }

        [Theory]
        [MemberData(nameof(ScreenFiles))]
        public void Buttons_TakeTheirSizeFromTheTouchTarget(string file)
        {
            var found = Load(Path.Combine(AppDir, file)).Descendants()
                .Where(e => e.Name.LocalName is "Button" or "ImageButton")
                .SelectMany(e => e.Attributes()
                    .Where(a => SizeProperties.Contains(a.Name.LocalName) && IsNonZeroLiteral(a.Value))
                    .Select(a => $"  line {Line(a)}: {e.Name.LocalName} {a.Name.LocalName}=\"{a.Value}\""))
                .ToList();

            Assert.True(found.Count == 0,
                $"{file} sizes a button by hand; use MinTouchTarget or one of the button styles " +
                $"(IconButton, FilledIconButton, FilledIconChip, HeroIconButton):{Environment.NewLine}" +
                string.Join(Environment.NewLine, found));
        }

        [Fact]
        public void Stylesheet_SpacingSitsOnTheFourGrid()
        {
            var found = new List<string>();

            foreach (var element in Load(StylesPath).Descendants())
            {
                var key = element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value;

                if ((element.Name.LocalName == "Double" && key is not null && (key.StartsWith("Spacing", StringComparison.Ordinal) || key is "CardGap" or "RowGap"))
                    || element.Name.LocalName == "Thickness")
                {
                    found.AddRange(OffGrid(element.Value).Select(n => $"  line {Line(element)}: {key} = {n}"));
                }

                // An ImageButton's padding is how big its glyph is drawn, not a gap between things.
                if (SetterOf(element) is ({ } property, { } value)
                    && SpacingProperties.Contains(property)
                    && StyleTargetOf(element) != "ImageButton")
                {
                    found.AddRange(OffGrid(value).Select(n => $"  line {Line(element)}: {StyleNameOf(element)} {property} = {n}"));
                }
            }

            Assert.True(found.Count == 0,
                $"Styles.xaml spaces off the 4 grid:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
        }

        [Fact]
        public void Stylesheet_EveryTouchMinimumIsTheToken()
        {
            var found = Load(StylesPath).Descendants()
                .Where(e => SetterOf(e) is ({ } property, _) && property is "MinimumHeightRequest" or "MinimumWidthRequest")
                .Where(e => e.Attribute("Value")?.Value != "{StaticResource MinTouchTarget}")
                // A glyph that does not take taps is not a touch target: the cell around it is.
                .Where(e => !IsInertStyle(e))
                .Select(e => $"  line {Line(e)}: {StyleNameOf(e)} {e.Attribute("Property")!.Value} = {e.Attribute("Value")?.Value}")
                .ToList();

            Assert.True(found.Count == 0,
                $"Styles.xaml has a touch minimum that is not MinTouchTarget:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
        }

        [Fact]
        public void Stylesheet_TypeComesFromTheTypeScale()
        {
            var found = Load(StylesPath).Descendants()
                .Where(e => SetterOf(e) is ("FontSize", { } value) && !value.StartsWith("{StaticResource FontSize", StringComparison.Ordinal))
                .Select(e => $"  line {Line(e)}: {StyleNameOf(e)} FontSize = {e.Attribute("Value")?.Value}")
                .ToList();

            Assert.True(found.Count == 0,
                $"Styles.xaml sets a font size off the type scale:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
        }

        [Fact]
        public void ViewsBuiltInCode_TakeTheScaleFromTheStylesheet()
        {
            var found = new List<string>();

            foreach (var path in Directory.EnumerateFiles(AppDir, "*.cs", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f)))
            {
                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    var assignment = CodeAssignment.Match(lines[i]);
                    if (assignment.Success && IsNonZero(assignment.Groups["value"].Value))
                    {
                        found.Add($"  {Path.GetRelativePath(AppDir, path)}:{i + 1}: {lines[i].Trim()}");
                    }

                    var thickness = CodeThickness.Match(lines[i]);
                    if (thickness.Success && Number.Matches(thickness.Groups["args"].Value).Any(n => IsNonZero(n.Value)))
                    {
                        found.Add($"  {Path.GetRelativePath(AppDir, path)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }

            Assert.True(found.Count == 0,
                "Views built in code set the scale as numbers; read it with DesignTokens:" +
                $"{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
        }

        private static XDocument Load(string path) => XDocument.Load(path, LoadOptions.SetLineInfo);

        private static (string? Property, string? Value) SetterOf(XElement element) =>
            element.Name.LocalName == "Setter"
                ? (element.Attribute("Property")?.Value, element.Attribute("Value")?.Value)
                : (null, null);

        private static string? StyleTargetOf(XElement element) =>
            element.Ancestors().FirstOrDefault(a => a.Name.LocalName == "Style")?.Attribute("TargetType")?.Value;

        private static string StyleNameOf(XElement element)
        {
            var style = element.Ancestors().FirstOrDefault(a => a.Name.LocalName == "Style");
            var key = style?.Attributes().FirstOrDefault(a => a.Name.LocalName == "Key")?.Value;
            return key ?? $"implicit {style?.Attribute("TargetType")?.Value}";
        }

        private static bool IsInertStyle(XElement element) =>
            element.Ancestors().FirstOrDefault(a => a.Name.LocalName == "Style")?.Elements()
                .Any(s => SetterOf(s) is ("InputTransparent", "True")) == true;

        private static bool IsNonZeroLiteral(string value) =>
            Literal.IsMatch(value.Trim()) && Number.Matches(value).Any(n => IsNonZero(n.Value));

        private static bool IsNonZero(string number) =>
            double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture) != 0;

        private static IEnumerable<string> OffGrid(string value) =>
            Literal.IsMatch(value.Trim())
                ? Number.Matches(value).Select(n => n.Value)
                    .Where(n => double.Parse(n, NumberStyles.Float, CultureInfo.InvariantCulture) % 4 != 0)
                : [];

        private static int Line(IXmlLineInfo node) => node.LineNumber;

        private static bool IsBuildOutput(string path)
        {
            var separator = Path.DirectorySeparatorChar;
            return path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
                || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
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
