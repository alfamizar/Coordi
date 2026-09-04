using Microsoft.Maui.Graphics;

namespace JustCompute.Shared.Controls
{
    public class SunPathView : GraphicsView
    {
        public static readonly BindableProperty RiseTimeProperty = BindableProperty.Create(
            nameof(RiseTime), typeof(DateTime?), typeof(SunPathView), null,
            propertyChanged: OnVisualChanged);

        public static readonly BindableProperty SetTimeProperty = BindableProperty.Create(
            nameof(SetTime), typeof(DateTime?), typeof(SunPathView), null,
            propertyChanged: OnVisualChanged);

        public static readonly BindableProperty CurrentTimeProperty = BindableProperty.Create(
            nameof(CurrentTime), typeof(DateTime?), typeof(SunPathView), null,
            propertyChanged: OnVisualChanged);

        public DateTime? RiseTime
        {
            get => (DateTime?)GetValue(RiseTimeProperty);
            set => SetValue(RiseTimeProperty, value);
        }

        public DateTime? SetTime
        {
            get => (DateTime?)GetValue(SetTimeProperty);
            set => SetValue(SetTimeProperty, value);
        }

        public DateTime? CurrentTime
        {
            get => (DateTime?)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        private readonly SunPathDrawable _drawable = new();

        public SunPathView()
        {
            Drawable = _drawable;
            HeightRequest = 160;
            Push();

            // Application.Current outlives every page, so a subscription taken in the constructor
            // and never released roots the control for the life of the app. Tie it to the visual
            // tree instead: today this control is a singleton page's only instance, but that is
            // not a property worth depending on.
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeChanged -= OnAppThemeChanged;
                app.RequestedThemeChanged += OnAppThemeChanged;
            }
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeChanged -= OnAppThemeChanged;
            }
        }

        private void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

        private void Push()
        {
            _drawable.RiseTime = RiseTime;
            _drawable.SetTime = SetTime;
            _drawable.CurrentTime = CurrentTime;
            Invalidate();
        }

        private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((SunPathView)bindable).Push();
        }

        private sealed class SunPathDrawable : IDrawable
        {
            public DateTime? RiseTime { get; set; }
            public DateTime? SetTime { get; set; }
            public DateTime? CurrentTime { get; set; }

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                var w = dirtyRect.Width;
                var h = dirtyRect.Height;
                if (w <= 0 || h <= 0) return;

                var palette = ResolvePalette();

                // Background — SOLID fill, not a LinearGradientPaint. The previous
                // gradient version rendered correctly on iOS but on Android's MAUI
                // Graphics backend the gradient paint state could persist into the
                // subsequent solid-color fills (FillColor = sunColor for the sun
                // marker), so the sun ended up sampling the cyan background gradient
                // instead of orange. A solid bg sidesteps the issue and is visually
                // almost identical at this size.
                canvas.FillColor = palette.BgTop;
                canvas.FillRectangle(dirtyRect);

                var horizonY = h * 0.60f;
                var dayAmp = h * 0.26f;     // height of the daytime apex above horizon
                var nightAmp = h * 0.13f;   // depth of the night trough below horizon

                canvas.StrokeColor = palette.Horizon;
                canvas.StrokeSize = 1f;
                canvas.DrawLine(0, horizonY, w, horizonY);

                // The arc depends only on the date's rise/set; the sun marker depends on "now".
                // Keep them separate so a date other than today still draws its path.
                if (RiseTime is not { } rise || SetTime is not { } set)
                {
                    return;
                }

                var dayLen = (set - rise).TotalHours;
                if (dayLen <= 0 || dayLen >= 24) return;

                var solarNoon = rise + (set - rise) / 2;
                var windowStart = solarNoon - TimeSpan.FromHours(12);

                double WindowHour(DateTime t)
                {
                    var hrs = (t - windowStart).TotalHours % 24.0;
                    if (hrs < 0) hrs += 24.0;
                    return hrs;
                }

                var riseH = WindowHour(rise);
                var setH = WindowHour(set);
                var nightLen = 24.0 - dayLen;

                float YForHour(double hour)
                {
                    if (hour >= riseH && hour <= setH)
                    {
                        var f = (hour - riseH) / (setH - riseH);
                        return (float)(horizonY - dayAmp * Math.Sin(Math.PI * f));
                    }

                    var nf = hour < riseH
                        ? (hour - (setH - 24.0)) / nightLen   // pre-dawn (prev evening's night)
                        : (hour - setH) / nightLen;           // post-dusk
                    nf = Math.Clamp(nf, 0.0, 1.0);
                    return (float)(horizonY + nightAmp * Math.Sin(Math.PI * nf));
                }

                PathF BuildPath(double fromHour, double toHour)
                {
                    var p = new PathF();
                    const int seg = 120;
                    for (int i = 0; i <= seg; i++)
                    {
                        var hour = fromHour + (toHour - fromHour) * i / seg;
                        var x = (float)(hour / 24.0) * w;
                        var y = YForHour(hour);
                        if (i == 0) p.MoveTo(x, y); else p.LineTo(x, y);
                    }
                    return p;
                }

                // Pre-mixed faded color (no alpha) so Android renders it consistently.
                canvas.StrokeColor = MixSolid(palette.Curve, palette.BgTop, 0.55f);
                canvas.StrokeSize = 2f;
                canvas.StrokeDashPattern = [3f, 3f];
                canvas.DrawPath(BuildPath(0, riseH));
                canvas.DrawPath(BuildPath(setH, 24));
                canvas.StrokeDashPattern = null;

                canvas.StrokeColor = palette.Curve;
                canvas.StrokeSize = 3f;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(BuildPath(riseH, setH));

                // No "now" marker when the chart is showing some other day.
                if (CurrentTime is { } now)
                {
                    var nowH = WindowHour(now);
                    var sunX = (float)(nowH / 24.0) * w;
                    var sunY = YForHour(nowH);
                    var aboveHorizon = nowH >= riseH && nowH <= setH;

                    canvas.SaveState();
                    DrawSun(canvas, sunX, sunY, palette.Sun, aboveHorizon);
                    canvas.RestoreState();
                }
            }

            private static void DrawSun(ICanvas canvas, float x, float y, Color sunColor, bool aboveHorizon)
            {
                // All layers are SOLID (pre-mixed) colors so Android's compositor renders them consistently.
                if (aboveHorizon)
                {
                    canvas.FillColor = MixSolid(sunColor, Colors.White, 0.55f);
                    canvas.FillCircle(x, y, 12f);

                    canvas.FillColor = Colors.White;
                    canvas.FillCircle(x, y, 8.5f);

                    canvas.FillColor = sunColor;
                    canvas.FillCircle(x, y, 6.5f);
                }
                else
                {
                    canvas.FillColor = Colors.White;
                    canvas.FillCircle(x, y, 7f);

                    canvas.FillColor = MixSolid(sunColor, Color.FromArgb("#8893A0"), 0.65f);
                    canvas.FillCircle(x, y, 4.5f);
                }
            }

            // Linear-RGB mix that produces a SOLID color, so we don't depend on the
            // platform's compositor to do alpha blending consistently.
            private static Color MixSolid(Color a, Color b, float t)
            {
                t = Math.Clamp(t, 0f, 1f);
                return new Color(
                    a.Red * (1 - t) + b.Red * t,
                    a.Green * (1 - t) + b.Green * t,
                    a.Blue * (1 - t) + b.Blue * t,
                    1f);
            }

            private readonly record struct Palette(Color BgTop, Color BgBottom, Color Horizon, Color Curve, Color Sun);

            private static Palette ResolvePalette()
            {
                var app = Application.Current;
                // The chart takes its colours from the active theme's semantic slots, not from
                // the device's light/dark setting, so it follows the chosen palette like the rest
                // of the UI. UserAppTheme is set from that palette, so it still says which side
                // we are on.
                var isDark = (app?.UserAppTheme ?? AppTheme.Unspecified) == AppTheme.Dark
                             || (app?.UserAppTheme == AppTheme.Unspecified && app?.RequestedTheme == AppTheme.Dark);

                Color Resource(string key, string fallback) =>
                    app?.Resources.TryGetValue(key, out var v) == true && v is Color c
                        ? c
                        : Color.FromArgb(fallback);

                var background = Resource("ThemeBackground", isDark ? "#121316" : "#FFFFFF");
                var surface = Resource("ThemeSurface", isDark ? "#1C1C1E" : "#CDF5FD");
                var primary = Resource("ThemePrimary", isDark ? "#FF9500" : "#00A9FF");
                var secondary = Resource("ThemeSecondary", isDark ? "#2C2C2E" : "#89CFF3");
                var outline = Resource("ThemeOutline", isDark ? "#2C2C2E" : "#89CFF3");
                var onSurface = Resource("ThemeOnSurface", isDark ? "#FFFFFF" : "#212121");

                if (isDark)
                {
                    return new Palette(
                        BgTop: surface,
                        BgBottom: background,
                        Horizon: outline.WithAlpha(0.7f),
                        // The primary, as on the light side. This used to be the secondary, which
                        // worked only while every dark palette happened to have a bright one; a
                        // theme whose secondary is a muted surface drew the sun's arc in grey.
                        Curve: primary,
                        Sun: Color.FromArgb("#FFC844"));
                }

                return new Palette(
                    BgTop: surface,
                    BgBottom: secondary.WithAlpha(0.6f),
                    Horizon: onSurface.WithAlpha(0.25f),
                    Curve: primary,
                    Sun: Color.FromArgb("#FF6B1F"));
            }
        }
    }
}
