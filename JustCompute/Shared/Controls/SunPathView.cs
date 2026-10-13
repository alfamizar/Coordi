using Compute.Core.Domain.ReadModels;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// The Sun's day from midnight to midnight: the sky at every hour, the arc above the horizon
    /// with a warm glow under it, the night dashed below, and the Sun where it is now — glowing and
    /// rayed when it is up, a dim ember when it is down.
    ///
    /// Penombre's sun chart started as a port of this view and went on to gain all of that; this is
    /// the same chart brought back.
    /// </summary>
    public class SunPathView : SkyPathView
    {
        // At full night these are the colours the chart always had; the day is what it never had.
        private static readonly SkyPalette DarkSky = new(
            Color.FromArgb("#070B22"), Color.FromArgb("#2A1E4A"), Color.FromArgb("#2C6094"), Color.FromArgb("#86AFCE"));

        private static readonly SkyPalette LightSky = new(
            Color.FromArgb("#243056"), Color.FromArgb("#4A3F6B"), Color.FromArgb("#6FB2E8"), Color.FromArgb("#FFE7BE"));

        /// <summary>Before the first snapshot: a fixed sky, night above the horizon fading to dusk.</summary>
        private static readonly PaintGradientStop[] DarkFallback =
        [
            new(0f, Color.FromArgb("#070B22")), new(0.55f, Color.FromArgb("#1B1746")),
            new(0.62f, Color.FromArgb("#2A1E4A")), new(1f, Color.FromArgb("#05060F")),
        ];

        private static readonly PaintGradientStop[] LightFallback =
        [
            new(0f, Color.FromArgb("#6FB2E8")), new(0.5f, Color.FromArgb("#BFE0F6")),
            new(0.62f, Color.FromArgb("#FFE7BE")), new(1f, Color.FromArgb("#EAF4FB")),
        ];

        protected override (float Low, float High, uint HalfBreathMillis) PulseShape => (0.82f, 1.18f, 1700);

        protected override (double? Rise, double? Set, bool AlwaysUp) Span(CelestialSnapshot snapshot) =>
        (
            // The Sun's events repeat daily, so one that lands just outside the day — a sunrise at
            // 23:50 the evening before, in a zone far from its meridian — is drawn where it falls on
            // the clock.
            HourOfDay(snapshot.SunRise, snapshot.GeoDate) is { } rise ? SkyPainter.Wrap24(rise) : null,
            HourOfDay(snapshot.SunSet, snapshot.GeoDate) is { } set ? SkyPainter.Wrap24(set) : null,
            snapshot.SunAlwaysUp
        );

        protected override void DrawSky(ICanvas canvas, RectF rect)
        {
            var dark = SkyPainter.IsDark();
            var horizonY = HorizonY(rect);
            var day = SolarDay.Of(Snapshot);

            SkyPainter.DrawSky(canvas, rect, horizonY, day, dark ? DarkSky : LightSky, dark ? DarkFallback : LightFallback);
            SkyPainter.DrawStars(canvas, rect, horizonY, day);
            SkyPainter.DrawHorizon(canvas, rect, horizonY, (dark ? Colors.White : Color.FromArgb("#FFC46B")).WithAlpha(0.5f));
        }

        protected override void DrawOverlay(ICanvas canvas, RectF rect)
        {
            if (PathFor(rect) is not { } path) return;

            var dark = SkyPainter.IsDark();
            var sun = Color.FromArgb(dark ? "#FFD24A" : "#FF7A1A");
            var curve = SkyPainter.ThemeColour("ThemePrimary", dark ? "#FF9500" : "#0071AE");

            SkyPainter.DrawArc(canvas, path, HorizonY(rect), curve, sun, peakGlow: 0.42f, Reveal);

            if (NowHour is { } now)
            {
                DrawSun(canvas, new PointF(path.X(now), path.Y(now)), sun, path.IsUp(now), Pulse, Reveal);
            }
        }

        /// <summary>
        /// A glowing, rayed Sun when it is up; a smaller, dimmer glow when it is below the horizon.
        /// <paramref name="pulse"/> breathes the glow and the rays, and <paramref name="alpha"/>
        /// fades the whole marker in with the arc.
        /// </summary>
        private static void DrawSun(ICanvas canvas, PointF centre, Color sun, bool up, float pulse, float alpha)
        {
            canvas.SaveState();
            if (up)
            {
                SkyPainter.FillGlow(canvas, centre, 15f * pulse, sun.WithAlpha(0.6f * alpha));

                canvas.StrokeColor = sun.WithAlpha(0.9f * alpha);
                canvas.StrokeSize = 1.5f;
                canvas.StrokeLineCap = LineCap.Round;
                const float inner = 5f;
                var outer = 8f * pulse;
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var dx = (float)Math.Cos(angle);
                    var dy = (float)Math.Sin(angle);
                    canvas.DrawLine(centre.X + dx * inner, centre.Y + dy * inner, centre.X + dx * outer, centre.Y + dy * outer);
                }

                canvas.FillColor = Colors.White.WithAlpha(alpha);
                canvas.FillCircle(centre, 4.5f);
                canvas.FillColor = sun.WithAlpha(alpha);
                canvas.FillCircle(centre, 3.5f);
            }
            else
            {
                SkyPainter.FillGlow(canvas, centre, 8f * pulse, sun.WithAlpha(0.35f * alpha));
                canvas.FillColor = sun.WithAlpha(0.85f * alpha);
                canvas.FillCircle(centre, 3f);
                canvas.FillColor = Colors.White.WithAlpha(0.45f * alpha);
                canvas.FillCircle(centre, 1.5f);
            }
            canvas.RestoreState();
        }
    }
}
