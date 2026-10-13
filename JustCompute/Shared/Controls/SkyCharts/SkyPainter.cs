using Compute.Core.Domain.ReadModels;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// Where the Sun is all day, which is all the sky behind a day chart needs to know: latitude
    /// and declination give its altitude at any hour angle, and <see cref="NoonHour"/> is where noon
    /// falls on the chart's clock.
    /// </summary>
    internal readonly record struct SolarDay(double LatitudeDeg, double DeclinationDeg, double NoonHour)
    {
        private const double Rad = Math.PI / 180.0;

        public double SunAltitudeDeg(double hour)
        {
            var hourAngle = (hour - NoonHour) * 15.0 * Rad;
            var lat = LatitudeDeg * Rad;
            var dec = DeclinationDeg * Rad;
            var sinAlt = Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(hourAngle);
            return Math.Asin(Math.Clamp(sinAlt, -1.0, 1.0)) / Rad;
        }

        public static SolarDay? Of(CelestialSnapshot? snapshot) =>
            snapshot?.SolarNoon is { } noon
                ? new SolarDay(snapshot.Latitude, snapshot.SunDeclination, SkyPainter.Wrap24((noon - snapshot.GeoDate.Date).TotalHours))
                : null;
    }

    /// <summary>The four corners of the sky's colour: night and day, zenith and horizon.</summary>
    internal readonly record struct SkyPalette(Color NightTop, Color NightHorizon, Color DayTop, Color DayHorizon);

    /// <summary>
    /// What the sun and moon day charts share: the sky behind them, its stars and horizon, and the
    /// arc a body draws across it.
    ///
    /// The colours here are the sky's, not the theme's — a night is navy and a noon blue in every
    /// palette, as they are outside. Only the arc takes a theme colour.
    /// </summary>
    internal static class SkyPainter
    {
        /// <summary>The horizon, as a fraction of the chart's height from the top.</summary>
        public const float HorizonFraction = 0.62f;

        private const double Rad = Math.PI / 180.0;

        /// <summary>
        /// How wide each strip of the sky is, in points. A quarter of an hour each was tried first,
        /// and dawn and dusk, where the sky changes fastest, came out as visible bars; at a point and
        /// a half they are a smooth glow. The sky is its own layer, painted only when the day
        /// changes, so the extra strips cost nothing while the chart animates.
        /// </summary>
        private const float StripWidth = 1.5f;

        /// <summary>
        /// Altitudes each strip takes a colour at, zenith to horizon. Close together near the bottom,
        /// where the warm band of twilight lives and changes fastest.
        /// </summary>
        private static readonly double[] AltitudeStops = [90, 60, 35, 18, 8, 3, 0];

        /// <summary>A few fixed stars, as (x, y, radius): x across the clock, y down to the horizon.</summary>
        private static readonly (float X, float Y, float Radius)[] Stars =
        [
            (0.08f, 0.18f, 1.4f), (0.17f, 0.42f, 1.0f), (0.26f, 0.12f, 1.6f),
            (0.37f, 0.30f, 1.1f), (0.46f, 0.16f, 1.3f), (0.58f, 0.36f, 1.0f),
            (0.67f, 0.14f, 1.5f), (0.74f, 0.30f, 1.2f), (0.83f, 0.20f, 1.1f),
            (0.91f, 0.40f, 1.4f), (0.50f, 0.46f, 0.9f), (0.12f, 0.30f, 1.0f),
        ];

        /// <summary>The colour of the sky low down at dawn and dusk.</summary>
        private static readonly Color TwilightGlow = new(1.00f, 0.52f, 0.24f);

        public static double Wrap24(double hours) => ((hours % 24.0) + 24.0) % 24.0;

        /// <summary>
        /// The sky, coloured by the hour: each strip is the sky at its own time of day, from where
        /// the Sun stands then. Without a <paramref name="day"/> (before the first snapshot) it is
        /// the fixed <paramref name="fallback"/> gradient.
        ///
        /// The colour model is Penombre's, the one its sky chart and day charts share, ported from
        /// its shader. That model looks round the whole sky, and two of its effects — Earth's shadow
        /// and the Belt of Venus — sit opposite the Sun. Across a day chart is time, not compass, so
        /// every strip is sampled thirty degrees round from the Sun: the warm band of twilight stays,
        /// the effects with nowhere to go are left out rather than put somewhere they would be wrong,
        /// and the aureole is kept small instead of burning sunrise and sunset to white.
        /// </summary>
        public static void DrawSky(ICanvas canvas, RectF rect, float horizonY, SolarDay? day, SkyPalette palette, PaintGradientStop[] fallback)
        {
            if (day is not { } solar)
            {
                FillVertical(canvas, rect, fallback);
                return;
            }

            var columns = Math.Max(24, (int)Math.Ceiling(rect.Width / StripWidth));
            var columnWidth = rect.Width / columns;
            // The ground is the horizon's colour taken down into shadow, fully so thirty degrees
            // below it on the sky's scale of ninety to the height above the horizon.
            var groundDepth = (horizonY - rect.Top) / 3f;
            var belowHeight = rect.Bottom - horizonY;
            var groundEnd = belowHeight > 0 ? Math.Min(1f, (groundDepth + 1f) / (belowHeight + 1f)) : 1f;

            for (var c = 0; c < columns; c++)
            {
                var sunAlt = solar.SunAltitudeDeg((c + 0.5) * 24.0 / columns);
                var x = rect.Left + c * columnWidth;

                var stops = new PaintGradientStop[AltitudeStops.Length];
                for (var i = 0; i < AltitudeStops.Length; i++)
                {
                    stops[i] = new PaintGradientStop((float)(1.0 - AltitudeStops[i] / 90.0), SkyColour(AltitudeStops[i], sunAlt, palette));
                }
                // A pixel wider than the strip, so neighbours overlap instead of leaving a seam.
                FillVertical(canvas, new RectF(x, rect.Top, columnWidth + 1f, horizonY - rect.Top), stops);

                var horizon = stops[^1].Color;
                var ground = Ground(horizon);
                // From a pixel above the horizon: where the two halves of a strip met exactly, the
                // edge between them was antialiased on its own and every strip left a dot there.
                FillVertical(canvas, new RectF(x, horizonY - 1f, columnWidth + 1f, belowHeight + 1f),
                [
                    new PaintGradientStop(0f, horizon),
                    new PaintGradientStop(groundEnd, ground),
                    new PaintGradientStop(1f, ground),
                ]);
            }
        }

        /// <summary>
        /// The stars, each fading as the sky at its hour gets too bright to hold it: gone while the
        /// Sun is up, all there once it is eighteen degrees down. Without a day, all of them.
        /// </summary>
        public static void DrawStars(ICanvas canvas, RectF rect, float horizonY, SolarDay? day)
        {
            foreach (var (xf, yf, radius) in Stars)
            {
                var visible = day is { } solar ? (float)Math.Clamp(-solar.SunAltitudeDeg(xf * 24.0) / 18.0, 0.0, 1.0) : 1f;
                if (visible <= 0.01f) continue;

                canvas.FillColor = Colors.White.WithAlpha((0.10f + 0.6f * (1f - yf)) * visible);
                canvas.FillCircle(rect.Left + xf * rect.Width, rect.Top + yf * (horizonY - rect.Top), radius);
            }
        }

        /// <summary>The horizon as a soft line, brightest in the middle and gone at the edges.</summary>
        public static void DrawHorizon(ICanvas canvas, RectF rect, float horizonY, Color glow)
        {
            var line = new RectF(rect.Left, horizonY - 0.75f, rect.Width, 1.5f);
            canvas.SaveState();
            canvas.SetFillPaint(new LinearGradientPaint
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops =
                [
                    new PaintGradientStop(0f, glow.WithAlpha(0f)),
                    new PaintGradientStop(0.5f, glow),
                    new PaintGradientStop(1f, glow.WithAlpha(0f)),
                ],
            }, line);
            canvas.FillRectangle(line);
            canvas.RestoreState();
        }

        /// <summary>
        /// The body's path: a glow under the arc, the arc itself shading from
        /// <paramref name="edge"/> at the horizon to <paramref name="peak"/> at its highest, and the
        /// stretches below the horizon dashed. <paramref name="reveal"/> runs 0..1 as the arc draws
        /// itself in from rise to set and everything else fades in with it.
        /// </summary>
        public static void DrawArc(ICanvas canvas, SkyPath path, float horizonY, Color edge, Color peak, float peakGlow, float reveal)
        {
            foreach (var (from, to) in path.DayIntervals)
            {
                var area = new RectF(path.X(from), horizonY - path.DayAmp, Math.Max(1f, path.X(to) - path.X(from)), Math.Max(1f, path.DayAmp));
                canvas.SaveState();
                canvas.SetFillPaint(new LinearGradientPaint
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops =
                    [
                        new PaintGradientStop(0f, peak.WithAlpha(peakGlow * reveal)),
                        new PaintGradientStop(0.5f, edge.WithAlpha(0.12f * reveal)),
                        new PaintGradientStop(1f, edge.WithAlpha(0f)),
                    ],
                }, area);
                canvas.FillPath(path.FillPath(from, to));
                canvas.RestoreState();
            }

            canvas.SaveState();
            canvas.StrokeColor = edge.WithAlpha(0.45f * reveal);
            canvas.StrokeSize = 1.5f;
            // In stroke widths: three on, four off at this width.
            canvas.StrokeDashPattern = [2f, 2.7f];
            foreach (var (from, to) in path.NightIntervals)
            {
                canvas.DrawPath(path.StrokePath(from, to));
            }
            canvas.RestoreState();

            // A gradient along a line is beyond MAUI's canvas, so the arc is short segments, each
            // in its own colour. Round caps close the joins.
            canvas.SaveState();
            canvas.StrokeSize = 3f;
            canvas.StrokeLineCap = LineCap.Round;
            foreach (var (from, to) in path.DayIntervals)
            {
                var end = from + (to - from) * reveal;
                if (end <= from) continue;

                var left = path.X(from);
                var span = Math.Max(1f, path.X(to) - left);
                PointF? previous = null;
                foreach (var point in path.Points(from, end))
                {
                    if (previous is { } p)
                    {
                        var t = ((p.X + point.X) / 2f - left) / span;
                        canvas.StrokeColor = Mix(edge, peak, 1f - Math.Abs(2f * t - 1f));
                        canvas.DrawLine(p, point);
                    }
                    previous = point;
                }
            }
            canvas.RestoreState();
        }

        /// <summary>A soft round glow, solid at the centre and gone at the edge.</summary>
        public static void FillGlow(ICanvas canvas, PointF centre, float radius, Color colour)
        {
            if (radius <= 0f) return;

            var bounds = new RectF(centre.X - radius, centre.Y - radius, radius * 2f, radius * 2f);
            canvas.SaveState();
            canvas.SetFillPaint(new RadialGradientPaint
            {
                Center = new Point(0.5, 0.5),
                Radius = 0.5,
                GradientStops =
                [
                    new PaintGradientStop(0f, colour),
                    new PaintGradientStop(1f, colour.WithAlpha(0f)),
                ],
            }, bounds);
            canvas.FillCircle(centre, radius);
            canvas.RestoreState();
        }

        /// <summary>
        /// A gradient fill, kept inside its own saved state. On Android a gradient set as the fill
        /// outlived the shape it was meant for: a solid colour set afterwards changed the paint's
        /// colour but left the gradient on it, and the Sun once came out in the sky's cyan.
        /// Restoring the state takes the gradient away with it.
        /// </summary>
        private static void FillVertical(ICanvas canvas, RectF rect, PaintGradientStop[] stops)
        {
            if (rect.Width <= 0f || rect.Height <= 0f) return;

            canvas.SaveState();
            canvas.SetFillPaint(new LinearGradientPaint
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = stops,
            }, rect);
            canvas.FillRectangle(rect);
            canvas.RestoreState();
        }

        /// <summary>The sky at <paramref name="alt"/> degrees up, with the Sun at <paramref name="sunAlt"/>.</summary>
        private static Color SkyColour(double alt, double sunAlt, SkyPalette palette)
        {
            // cos 30°: the strips look thirty degrees round from the Sun; see DrawSky.
            const double toward = 0.8660254037844387;

            var cosGamma = Math.Sin(alt * Rad) * Math.Sin(sunAlt * Rad) + Math.Cos(alt * Rad) * Math.Cos(sunAlt * Rad) * toward;
            // 1 in daylight, 0 once the Sun is eighteen degrees down and it is properly night.
            var dayness = (float)(1.0 - Math.Clamp(-sunAlt / 18.0, 0.0, 1.0));
            // Height above the horizon, curved so that the low sky, where things happen, gets more.
            var h = (float)Math.Pow(Math.Clamp(alt / 90.0, 0.0, 1.0), 0.6);
            var sky = Mix(Mix(palette.NightHorizon, palette.NightTop, h), Mix(palette.DayHorizon, palette.DayTop, h), dayness);

            // The warm band: strongest with the Sun three degrees down, gone by the end of civil
            // twilight, and clinging to the horizon. Mixed in, where Penombre's shader adds it: added
            // to a horizon that is already pale at dawn, the orange ran past white, and sunrise and
            // sunset stood on the chart as two white pillars instead of a glow.
            var t = (sunAlt + 3.0) / 7.0;
            var band = (float)(Math.Exp(-t * t) * Math.Exp(-alt / 10.0) * toward * toward * 0.85);
            sky = Mix(sky, TwilightGlow, band);
            // The daytime aureole around the Sun.
            var aureole = (float)(Math.Pow(Math.Max(cosGamma, 0.0), 24.0) * dayness * 0.35);

            return new Color(
                Math.Clamp(sky.Red + 1.00f * aureole, 0f, 1f),
                Math.Clamp(sky.Green + 0.95f * aureole, 0f, 1f),
                Math.Clamp(sky.Blue + 0.82f * aureole, 0f, 1f));
        }

        /// <summary>
        /// Below the horizon: darker and drained of colour. It is ground, and a bright sky merely
        /// dimmed reads at noon as a pale slab rather than as the earth the Sun has gone behind.
        /// </summary>
        private static Color Ground(Color sky)
        {
            var grey = sky.Red * 0.30f + sky.Green * 0.59f + sky.Blue * 0.11f;
            var drained = Mix(sky, new Color(grey, grey, grey), 0.55f);
            return new Color(drained.Red * 0.20f, drained.Green * 0.20f, drained.Blue * 0.20f);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(
                a.Red + (b.Red - a.Red) * t,
                a.Green + (b.Green - a.Green) * t,
                a.Blue + (b.Blue - a.Blue) * t,
                a.Alpha + (b.Alpha - a.Alpha) * t);
        }

        /// <summary>
        /// Which side of the palette is showing. The charts follow the chosen palette, like the rest
        /// of the app, rather than the device's own light or dark setting; UserAppTheme is set from
        /// the palette, so it still says which.
        /// </summary>
        public static bool IsDark()
        {
            var app = Application.Current;
            return app?.UserAppTheme == AppTheme.Dark
                   || ((app?.UserAppTheme is null or AppTheme.Unspecified) && app?.RequestedTheme == AppTheme.Dark);
        }

        /// <summary>A palette colour as the app currently has it.</summary>
        public static Color ThemeColour(string key, string fallback) =>
            Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color colour
                ? colour
                : Color.FromArgb(fallback);
    }
}
