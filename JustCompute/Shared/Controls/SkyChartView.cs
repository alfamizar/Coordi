using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using Microsoft.Maui.Graphics;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// The whole sky over one place at one instant, drawn on a disc: zenith at the centre,
    /// horizon at the rim, north up and east on the left — a planisphere held overhead.
    ///
    /// Everything on it comes out of Compute.Astro: the Yale bright stars to magnitude 4.5, the
    /// traditional stick figures, the IAU boundaries precessed out of B1875, and the Sun, Moon
    /// and planets. Nothing is fetched and nothing is stored; give it a place and a Julian Day
    /// and it draws that moment.
    /// </summary>
    public class SkyChartView : GraphicsView
    {
        public static readonly BindableProperty LatitudeProperty = BindableProperty.Create(
            nameof(Latitude), typeof(double), typeof(SkyChartView), 0.0, propertyChanged: OnVisualChanged);

        public static readonly BindableProperty LongitudeProperty = BindableProperty.Create(
            nameof(Longitude), typeof(double), typeof(SkyChartView), 0.0, propertyChanged: OnVisualChanged);

        public static readonly BindableProperty JdUtcProperty = BindableProperty.Create(
            nameof(JdUtc), typeof(double), typeof(SkyChartView), AstroTime.J2000, propertyChanged: OnVisualChanged);

        public static readonly BindableProperty ShowStickFiguresProperty = BindableProperty.Create(
            nameof(ShowStickFigures), typeof(bool), typeof(SkyChartView), true, propertyChanged: OnVisualChanged);

        public static readonly BindableProperty ShowBordersProperty = BindableProperty.Create(
            nameof(ShowBorders), typeof(bool), typeof(SkyChartView), false, propertyChanged: OnVisualChanged);

        /// <summary>
        /// The compass letters, passed in rather than looked up: the drawable has no business
        /// knowing about localization, and Polish needs "Wsch" where English needs "E".
        /// </summary>
        public static readonly BindableProperty CardinalsProperty = BindableProperty.Create(
            nameof(Cardinals), typeof(string[]), typeof(SkyChartView), new[] { "N", "E", "S", "W" },
            propertyChanged: OnVisualChanged);

        /// <summary>The seven planet names, outward from the Sun, for the same reason.</summary>
        public static readonly BindableProperty PlanetNamesProperty = BindableProperty.Create(
            nameof(PlanetNames), typeof(string[]), typeof(SkyChartView), Array.Empty<string>(),
            propertyChanged: OnVisualChanged);

        public double Latitude
        {
            get => (double)GetValue(LatitudeProperty);
            set => SetValue(LatitudeProperty, value);
        }

        public double Longitude
        {
            get => (double)GetValue(LongitudeProperty);
            set => SetValue(LongitudeProperty, value);
        }

        public double JdUtc
        {
            get => (double)GetValue(JdUtcProperty);
            set => SetValue(JdUtcProperty, value);
        }

        public bool ShowStickFigures
        {
            get => (bool)GetValue(ShowStickFiguresProperty);
            set => SetValue(ShowStickFiguresProperty, value);
        }

        public bool ShowBorders
        {
            get => (bool)GetValue(ShowBordersProperty);
            set => SetValue(ShowBordersProperty, value);
        }

        public string[] Cardinals
        {
            get => (string[])GetValue(CardinalsProperty);
            set => SetValue(CardinalsProperty, value);
        }

        public string[] PlanetNames
        {
            get => (string[])GetValue(PlanetNamesProperty);
            set => SetValue(PlanetNamesProperty, value);
        }

        private readonly SkyDrawable _drawable = new();

        public SkyChartView()
        {
            Drawable = _drawable;
            PushChanges();
        }

        private void PushChanges()
        {
            _drawable.Latitude = Latitude;
            _drawable.Longitude = Longitude;
            _drawable.JdUtc = JdUtc;
            _drawable.ShowStickFigures = ShowStickFigures;
            _drawable.ShowBorders = ShowBorders;
            _drawable.Cardinals = Cardinals;
            _drawable.PlanetNames = PlanetNames;
            Invalidate();
        }

        private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue) =>
            ((SkyChartView)bindable).PushChanges();

        private sealed class SkyDrawable : IDrawable
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double JdUtc { get; set; }
            public bool ShowStickFigures { get; set; }
            public bool ShowBorders { get; set; }
            public string[] Cardinals { get; set; } = ["N", "E", "S", "W"];
            public string[] PlanetNames { get; set; } = [];

            /// <summary>
            /// A sky is a sky. These do not follow the app's theme, because the chart is a picture
            /// of something that is one colour whatever the reader has chosen for their buttons.
            /// </summary>
            private static readonly Color DaySky = Color.FromArgb("#5C8FC4");
            private static readonly Color NightSky = Color.FromArgb("#0A0E1E");
            private static readonly Color HorizonRing = Color.FromArgb("#4DFFFFFF");
            private static readonly Color AltitudeRing = Color.FromArgb("#1FFFFFFF");
            private static readonly Color BorderLine = Color.FromArgb("#2EFFFFFF");
            private static readonly Color FigureLine = Color.FromArgb("#596FA8DC");
            private static readonly Color CardinalText = Color.FromArgb("#BFD8F2");
            private static readonly Color SunDisc = Color.FromArgb("#FFD24A");
            private static readonly Color MoonDisc = Color.FromArgb("#E8E6DE");
            private static readonly Color PlanetDisc = Color.FromArgb("#FFB27A");
            private static readonly Color BodyLabel = Color.FromArgb("#E6F0FF");
            private static readonly Color BodyRing = Color.FromArgb("#99FFFFFF");
            private static readonly Color StarLabel = Color.FromArgb("#A8C6E8");

            /// <summary>Space kept outside the disc for the compass letters.</summary>
            private const float CardinalGap = 20f;

            /// <summary>
            /// The catalogue precessed to the chart's epoch, and the epoch it was precessed to.
            ///
            /// J2000 positions drift about 0.36° by 2026 — small, but a chart is the one place
            /// that shows it, and precessing nine hundred stars on every frame of a dragged
            /// slider is the most expensive thing here by a wide margin. A month's worth of
            /// drift is under an arcsecond, so the cache is refreshed no more often than that.
            /// </summary>
            /// <summary>
            /// Names collected while the markers are drawn and flushed once the clip is lifted.
            /// Clipped to the disc they lose their tails — "Arcturus" arrives as "Arcturu" — and
            /// a name that sits half outside the horizon ring is still perfectly readable.
            /// </summary>
            private readonly List<(string Text, float X, float Y, Color Colour, float Size)> _labels = [];

            private double _epochJd = double.NaN;
            private (double Ra, double Dec, double Magnitude, double Bv, string? Name)[] _stars = [];
            private double[][] _figures = [];
            private double[][] _borders = [];

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                var cx = dirtyRect.Center.X;
                var cy = dirtyRect.Center.Y;
                var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - CardinalGap;
                if (radius <= 4f) return;

                EnsureCatalogue();
                _labels.Clear();

                var dome = SkyDome.For(JdUtc, Latitude, Longitude);
                var sun = Sun.PositionAt(JdUtc);
                var sunAltitude = dome.Horizontal(sun.RightAscension, sun.Declination).AltitudeDeg;

                DrawSky(canvas, cx, cy, radius, sunAltitude);

                // Everything inside the horizon, so a marker near the rim cannot spill out of it.
                canvas.SaveState();
                canvas.ClipPath(CircleAt(cx, cy, radius));

                if (ShowBorders) DrawPolylines(canvas, dome, cx, cy, radius, _borders, BorderLine, 1f);
                if (ShowStickFigures) DrawPolylines(canvas, dome, cx, cy, radius, _figures, FigureLine, 1.1f);

                DrawStars(canvas, dome, cx, cy, radius, sunAltitude);
                DrawSolarSystem(canvas, dome, cx, cy, radius);

                canvas.RestoreState();

                DrawFrame(canvas, cx, cy, radius);
                FlushLabels(canvas);
            }

            /// <summary>
            /// Daylight blue through to night, by how far the Sun is below the horizon. The whole
            /// of astronomical twilight is the transition, so the disc says at a glance whether
            /// what it is showing could actually be seen.
            /// </summary>
            private static void DrawSky(ICanvas canvas, float cx, float cy, float radius, double sunAltitude)
            {
                var night = Math.Clamp(-sunAltitude / 18.0, 0.0, 1.0);

                canvas.FillColor = Lerp(DaySky, NightSky, (float)night);
                canvas.FillCircle(cx, cy, radius);

                canvas.StrokeColor = AltitudeRing;
                canvas.StrokeSize = 1f;
                foreach (var altitude in new[] { 30.0, 60.0 })
                {
                    canvas.DrawCircle(cx, cy, radius * (float)((90.0 - altitude) / 90.0));
                }
            }

            private void DrawStars(ICanvas canvas, SkyDome dome, float cx, float cy, float radius, double sunAltitude)
            {
                // Faded out by daylight rather than hidden: the chart still answers "what is up
                // there right now" at noon, and saying so quietly is better than an empty disc.
                var visibility = (float)Math.Clamp(0.25 - sunAltitude / 12.0, 0.18, 1.0);

                foreach (var (ra, dec, magnitude, bv, name) in _stars)
                {
                    var h = dome.Horizontal(ra, dec);
                    if (SkyDome.Project(h.AzimuthDeg, h.AltitudeDeg) is not { } at) continue;

                    // Magnitude 4.5 is the faintest in the catalogue and belongs at the edge of
                    // visibility; magnitude 0 wants to read as a landmark.
                    var brightness = (float)Math.Clamp((4.8 - magnitude) / 4.8, 0.08, 1.0);
                    var size = 0.6f + 2.0f * brightness * brightness * (radius / 160f);

                    var x = cx + (float)at.X * radius;
                    var y = cy + (float)at.Y * radius;

                    canvas.FillColor = StarColour(bv).WithAlpha(Math.Min(1f, 0.35f + 0.65f * brightness) * visibility);
                    canvas.FillCircle(x, y, size);

                    if (name is null || magnitude > NamedStarMagnitude) continue;

                    // Catalogue proper names, used as they are: Vega is Vega in an atlas in any
                    // language, and transliterating twenty of them into sixteen would invent
                    // spellings no reader of a star chart expects.
                    _labels.Add((name, x, y + size + 1f, StarLabel.WithAlpha(visibility), 9f));
                }
            }

            /// <summary>
            /// Only the landmarks get a name. Everything to magnitude 4.5 is on the chart, and
            /// nine hundred labels would be a page of text with some stars behind it.
            /// </summary>
            private const double NamedStarMagnitude = 1.6;

            /// <summary>
            /// B−V as a tint: hot stars run blue-white, cool ones orange. Compressed hard,
            /// because a sky drawn in saturated colour looks like a toy and the eye sees very
            /// little colour in real starlight.
            /// </summary>
            private static Color StarColour(double bv)
            {
                var t = (float)Math.Clamp((bv + 0.3) / 1.8, 0.0, 1.0);
                return Lerp(Color.FromArgb("#CFE3FF"), Color.FromArgb("#FFD2A6"), t);
            }

            private static void DrawPolylines(
                ICanvas canvas,
                SkyDome dome,
                float cx,
                float cy,
                float radius,
                double[][] polylines,
                Color colour,
                float width)
            {
                canvas.StrokeColor = colour;
                canvas.StrokeSize = width;

                foreach (var line in polylines)
                {
                    for (var i = 0; i + 3 < line.Length; i += 2)
                    {
                        var a = dome.Horizontal(line[i], line[i + 1]);
                        var b = dome.Horizontal(line[i + 2], line[i + 3]);

                        // Both ends or neither: a segment leaving the dome would need clipping
                        // along a great circle, and the few it drops are all at the rim where
                        // the projection is stretched and nothing is legible anyway.
                        if (SkyDome.Project(a.AzimuthDeg, a.AltitudeDeg) is not { } from) continue;
                        if (SkyDome.Project(b.AzimuthDeg, b.AltitudeDeg) is not { } to) continue;

                        canvas.DrawLine(
                            cx + (float)from.X * radius, cy + (float)from.Y * radius,
                            cx + (float)to.X * radius, cy + (float)to.Y * radius);
                    }
                }
            }

            private void DrawSolarSystem(ICanvas canvas, SkyDome dome, float cx, float cy, float radius)
            {
                var sun = Sun.PositionAt(JdUtc);
                DrawBody(canvas, dome, cx, cy, radius, sun.RightAscension, sun.Declination, SunDisc, 6f, "☉");

                var moon = Moon.PositionAt(JdUtc);
                DrawBody(canvas, dome, cx, cy, radius, moon.RightAscensionDeg, moon.DeclinationDeg, MoonDisc, 6f, "☽");

                Planet[] planets =
                [
                    Planet.Mercury, Planet.Venus, Planet.Mars,
                    Planet.Jupiter, Planet.Saturn, Planet.Uranus, Planet.Neptune,
                ];

                for (var i = 0; i < planets.Length; i++)
                {
                    var eq = Planets.GeocentricEquatorial(planets[i], JdUtc);
                    var label = i < PlanetNames.Length ? PlanetNames[i] : null;
                    DrawBody(canvas, dome, cx, cy, radius, eq.RightAscensionDeg, eq.DeclinationDeg, PlanetDisc, 3.5f, label);
                }
            }

            private void DrawBody(
                ICanvas canvas,
                SkyDome dome,
                float cx,
                float cy,
                float radius,
                double raDeg,
                double decDeg,
                Color colour,
                float size,
                string? label)
            {
                var h = dome.Horizontal(raDeg, decDeg);
                if (SkyDome.Project(h.AzimuthDeg, h.AltitudeDeg) is not { } at) return;

                var x = cx + (float)at.X * radius;
                var y = cy + (float)at.Y * radius;
                var scaled = size * Math.Max(0.7f, radius / 160f);

                canvas.FillColor = colour;
                canvas.FillCircle(x, y, scaled);

                // A ring, because the reddest stars on this chart are very nearly the colour of
                // a planet and nothing else would tell them apart at three pixels across.
                canvas.StrokeColor = BodyRing;
                canvas.StrokeSize = 1f;
                canvas.DrawCircle(x, y, scaled + 1.5f);

                if (label is null) return;

                _labels.Add((label, x, y + scaled + 3f, BodyLabel, 10f));
            }

            private void DrawFrame(ICanvas canvas, float cx, float cy, float radius)
            {
                canvas.StrokeColor = HorizonRing;
                canvas.StrokeSize = 1.2f;
                canvas.DrawCircle(cx, cy, radius);

                canvas.FillColor = HorizonRing;
                canvas.FillCircle(cx, cy, 1.5f);

                canvas.FontColor = CardinalText;
                canvas.FontSize = 12f;

                // North, east, south, west — anticlockwise, because the chart is the view up.
                for (var i = 0; i < 4; i++)
                {
                    var azimuth = i * 90.0 * Math.PI / 180.0;
                    var at = radius + CardinalGap * 0.55f;
                    var x = cx - (float)Math.Sin(azimuth) * at;
                    var y = cy - (float)Math.Cos(azimuth) * at;

                    canvas.DrawString(Cardinals[i], x - 24f, y - 8f, 48f, 16f,
                        HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }

            /// <summary>
            /// Precesses the three catalogues to the chart's epoch, or keeps what it has when the
            /// last one is still current to within a month.
            /// </summary>
            private void EnsureCatalogue()
            {
                if (!double.IsNaN(_epochJd) && Math.Abs(JdUtc - _epochJd) < 30.0) return;

                _epochJd = JdUtc;

                _stars =
                [
                    .. BrightStars.All.Select(star =>
                    {
                        var eq = Coordinates.PrecessEquatorial(AstroTime.J2000, JdUtc, star.RaDeg, star.DecDeg);
                        return (eq.RightAscensionDeg, eq.DeclinationDeg, star.Magnitude, star.Bv, star.Name);
                    })
                ];

                _figures =
                [
                    .. ConstellationLines.Polylines.Select(line => PrecessPolyline(
                        line, (ra, dec) => Coordinates.PrecessEquatorial(AstroTime.J2000, JdUtc, ra, dec)))
                ];

                _borders =
                [
                    .. Enum.GetValues<Constellation>().Select(c => PrecessPolyline(
                        Densify(Constellations.BoundaryB1875(c)),
                        (ra, dec) => Coordinates.PrecessFromB1875(JdUtc, ra, dec)))
                ];
            }

            private static double[] PrecessPolyline(double[] interleaved, Func<double, double, Equatorial> precess)
            {
                var result = new double[interleaved.Length];
                for (var i = 0; i + 1 < interleaved.Length; i += 2)
                {
                    var eq = precess(interleaved[i], interleaved[i + 1]);
                    result[i] = eq.RightAscensionDeg;
                    result[i + 1] = eq.DeclinationDeg;
                }

                return result;
            }

            /// <summary>
            /// The IAU boundaries run along constant right ascension and declination, which are
            /// curves in this projection and not the straight lines two vertices would draw. A
            /// vertex every two degrees is enough that the difference stops being visible.
            /// </summary>
            private static double[] Densify(IReadOnlyList<(double RaDeg, double DecDeg)> outline)
            {
                const double stepDeg = 2.0;
                var points = new List<double>(outline.Count * 4);

                for (var i = 0; i < outline.Count; i++)
                {
                    var (ra, dec) = outline[i];
                    points.Add(ra);
                    points.Add(dec);

                    if (i + 1 >= outline.Count) break;

                    var (nextRa, nextDec) = outline[i + 1];
                    var steps = (int)Math.Min(64, Math.Max(
                        Math.Abs(nextRa - ra) / stepDeg,
                        Math.Abs(nextDec - dec) / stepDeg));

                    for (var s = 1; s < steps; s++)
                    {
                        var t = (double)s / steps;
                        points.Add(ra + (nextRa - ra) * t);
                        points.Add(dec + (nextDec - dec) * t);
                    }
                }

                return [.. points];
            }

            private void FlushLabels(ICanvas canvas)
            {
                foreach (var (text, x, y, colour, size) in _labels)
                {
                    canvas.FontColor = colour;
                    canvas.FontSize = size;
                    canvas.DrawString(text, x - 48f, y, 96f, 13f,
                        HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }

            private static PathF CircleAt(float cx, float cy, float radius)
            {
                var path = new PathF();
                path.AppendCircle(cx, cy, radius);
                return path;
            }

            private static Color Lerp(Color from, Color to, float t) => new(
                from.Red + (to.Red - from.Red) * t,
                from.Green + (to.Green - from.Green) * t,
                from.Blue + (to.Blue - from.Blue) * t);
        }
    }
}
