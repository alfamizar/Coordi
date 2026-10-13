using Compute.Core.Domain.ReadModels;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// The Moon's day from midnight to midnight, the companion to <see cref="SunPathView"/>: the same
    /// sky, coloured by the Sun because the Moon is often up in daylight and should look it; the
    /// Moon's arc in silver; and the Moon where it is now, drawn at tonight's phase.
    ///
    /// Ported from Penombre's moon chart. Penombre shades its Moon on the GPU from a map of the real
    /// surface; MAUI's canvas has no shaders, so this one is the phase's true outline over a sphere
    /// shaded by a gradient, with the large maria laid on as soft patches.
    /// </summary>
    public class MoonPathView : SkyPathView
    {
        private static readonly SkyPalette DarkSky = new(
            Color.FromArgb("#05071A"), Color.FromArgb("#221C3E"), Color.FromArgb("#2A5C90"), Color.FromArgb("#83AACB"));

        private static readonly SkyPalette LightSky = new(
            Color.FromArgb("#1E2749"), Color.FromArgb("#3F3A60"), Color.FromArgb("#5C77B0"), Color.FromArgb("#C9CFE6"));

        private static readonly PaintGradientStop[] DarkFallback =
        [
            new(0f, Color.FromArgb("#05071A")), new(0.55f, Color.FromArgb("#141235")),
            new(0.62f, Color.FromArgb("#221C3E")), new(1f, Color.FromArgb("#03040C")),
        ];

        private static readonly PaintGradientStop[] LightFallback =
        [
            new(0f, Color.FromArgb("#5C77B0")), new(0.5f, Color.FromArgb("#9FB6DD")),
            new(0.62f, Color.FromArgb("#C9CFE6")), new(1f, Color.FromArgb("#E7ECF6")),
        ];

        /// <summary>The sunlit surface.</summary>
        private static readonly Color MoonLit = Color.FromArgb("#ECE7F7");

        /// <summary>The lit surface towards the limb, where it is seen at a slant and looks darker.</summary>
        private static readonly Color MoonLimb = Color.FromArgb("#B9B4CC");

        /// <summary>The unlit side.</summary>
        private static readonly Color MoonDark = Color.FromArgb("#2A2740");

        /// <summary>Sunlight returned by the Earth, which keeps the unlit side faintly visible.</summary>
        private static readonly Color Earthshine = Color.FromArgb("#9FB4D8");

        /// <summary>The basalt plains: darker, and greyer than the highlands around them.</summary>
        private static readonly Color Mare = Color.FromArgb("#8E8AA6");

        /// <summary>
        /// The large maria, as the northern hemisphere sees the near side: centre and radius as
        /// fractions of the Moon's radius, x to the right, y down.
        /// </summary>
        private static readonly (float X, float Y, float Radius)[] Maria =
        [
            (-0.55f, -0.05f, 0.30f), // Oceanus Procellarum
            (-0.30f, -0.40f, 0.24f), // Mare Imbrium
            (0.12f, -0.40f, 0.15f),  // Mare Serenitatis
            (0.30f, -0.08f, 0.17f),  // Mare Tranquillitatis
            (0.62f, -0.28f, 0.09f),  // Mare Crisium
            (0.55f, 0.15f, 0.11f),   // Mare Fecunditatis
            (0.33f, 0.25f, 0.07f),   // Mare Nectaris
            (-0.18f, 0.40f, 0.15f),  // Mare Nubium
        ];

        /// <summary>The thinnest crescent drawn; see <see cref="DrawMoon"/>.</summary>
        private const double MinimumVisibleFraction = 0.10;

        protected override (float Low, float High, uint HalfBreathMillis) PulseShape => (0.85f, 1.2f, 2200);

        protected override (double? Rise, double? Set, bool AlwaysUp) Span(CelestialSnapshot snapshot) =>
        (
            // Unlike the Sun's, the Moon's events are found for the local day itself, and its times
            // move by most of an hour a day, so one outside the day would not be this day's.
            Within(HourOfDay(snapshot.MoonRise, snapshot.GeoDate)),
            Within(HourOfDay(snapshot.MoonSet, snapshot.GeoDate)),
            snapshot.MoonAlwaysUp
        );

        protected override void DrawSky(ICanvas canvas, RectF rect)
        {
            var dark = SkyPainter.IsDark();
            var horizonY = HorizonY(rect);
            var day = SolarDay.Of(Snapshot);

            SkyPainter.DrawSky(canvas, rect, horizonY, day, dark ? DarkSky : LightSky, dark ? DarkFallback : LightFallback);
            SkyPainter.DrawStars(canvas, rect, horizonY, day);
            SkyPainter.DrawHorizon(canvas, rect, horizonY, Colors.White.WithAlpha(0.4f));
        }

        protected override void DrawOverlay(ICanvas canvas, RectF rect)
        {
            if (PathFor(rect) is not { } path || Snapshot is not { } snapshot) return;

            SkyPainter.DrawArc(canvas, path, HorizonY(rect), Earthshine, MoonLit, peakGlow: 0.30f, Reveal);

            if (NowHour is { } now)
            {
                var up = path.IsUp(now);
                DrawMoon(
                    canvas,
                    new PointF(path.X(now), path.Y(now)),
                    up ? 13f : 9f,
                    snapshot.MoonIllumination,
                    snapshot.MoonWaxing,
                    southern: snapshot.Latitude < 0,
                    up,
                    Pulse,
                    Reveal);
            }
        }

        private static void DrawMoon(
            ICanvas canvas, PointF centre, float radius, double illumination, bool waxing, bool southern, bool up, float pulse, float alpha)
        {
            canvas.SaveState();

            if (up)
            {
                SkyPainter.FillGlow(canvas, centre, radius * 2.4f * pulse, MoonLit.WithAlpha(0.5f * alpha));
            }

            // The unlit side, faintly lit by the Earth and letting the sky through: an opaque disc
            // read as a hole in a daytime sky, which is not what a Moon there looks like.
            canvas.FillColor = SkyPainter.Mix(MoonDark, Earthshine, 0.25f).WithAlpha(0.88f * alpha);
            canvas.FillCircle(centre, radius);

            // Seen from the south the Moon is upside down: the waxing side is on the left, and the
            // maria turn round with it.
            var flip = southern ? -1f : 1f;
            var side = (waxing ? 1f : -1f) * flip;

            if (illumination > 0.005)
            {
                // A crescent of a few days is a sliver under two points wide on a disc this size, and
                // vanishes. It is drawn no thinner than a 10% Moon, which still reads as a crescent.
                var lit = LitPath(centre, radius, Math.Max(illumination, MinimumVisibleFraction), side);
                var bounds = new RectF(centre.X - radius, centre.Y - radius, radius * 2f, radius * 2f);

                // Brightest towards the lit limb's middle, falling off to the edge: the sphere.
                canvas.SaveState();
                canvas.SetFillPaint(new RadialGradientPaint
                {
                    Center = new Point(0.5 + side * 0.18, 0.40),
                    Radius = 0.75,
                    GradientStops =
                    [
                        new PaintGradientStop(0f, MoonLit.WithAlpha(alpha)),
                        new PaintGradientStop(1f, MoonLimb.WithAlpha(alpha)),
                    ],
                }, bounds);
                canvas.FillPath(lit);
                canvas.RestoreState();

                canvas.SaveState();
                canvas.ClipPath(lit);
                canvas.FillColor = SkyPainter.Mix(MoonLit, Mare, 0.38f).WithAlpha(alpha);
                foreach (var (x, y, r) in Maria)
                {
                    var mx = centre.X + flip * x * radius;
                    var my = centre.Y + flip * y * radius;
                    canvas.FillEllipse(mx - r * radius, my - r * radius * 0.8f, r * radius * 2f, r * radius * 1.6f);
                }
                canvas.RestoreState();
            }

            canvas.StrokeColor = Colors.White.WithAlpha(0.25f * alpha);
            canvas.StrokeSize = 1f;
            canvas.DrawCircle(centre, radius);

            canvas.RestoreState();
        }

        /// <summary>
        /// The lit part of the disc, for illuminated fraction <paramref name="k"/>: the lit limb, a
        /// half circle on <paramref name="side"/> (+1 right, −1 left), closed by the terminator, a
        /// half ellipse of half-width R·(1 − 2k). That bulges towards the lit side while k is under a
        /// half (a crescent) and away from it after (gibbous), and is straight at the quarters.
        /// </summary>
        private static PathF LitPath(PointF centre, float radius, double k, float side)
        {
            const int steps = 24;
            var bulge = (float)(1.0 - 2.0 * k);
            var path = new PathF();

            for (var i = 0; i <= steps; i++)
            {
                var a = -Math.PI / 2 + Math.PI * i / steps;
                var x = centre.X + side * radius * (float)Math.Cos(a);
                var y = centre.Y + radius * (float)Math.Sin(a);
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }

            for (var i = steps; i >= 0; i--)
            {
                var a = -Math.PI / 2 + Math.PI * i / steps;
                path.LineTo(centre.X + side * radius * bulge * (float)Math.Cos(a), centre.Y + radius * (float)Math.Sin(a));
            }

            path.Close();
            return path;
        }

        private static double? Within(double? hour) => hour is >= 0.0 and <= 24.0 ? hour : null;
    }
}
