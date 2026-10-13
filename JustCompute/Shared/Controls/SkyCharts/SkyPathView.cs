using Compute.Core.Domain.ReadModels;

namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// A body's day on a midnight-to-midnight axis, over a sky coloured by where the Sun is at each
    /// hour: its arc above the horizon, its path below it, and where it is now.
    ///
    /// Two layers. The sky is some seventy gradient bands, so it is a canvas of its own, drawn
    /// only when the day, the size or the theme changes. The arc and the marker are on a second,
    /// transparent canvas above it, which is the one the clock and the animations redraw: a frame
    /// of the pulse costs a dozen shapes, not seventy gradients.
    ///
    /// On appearing, the arc draws itself in from rise to set and the marker breathes twice, then
    /// both are still. The pulse used to run for ever in Penombre and kept a CPU core busy to move
    /// one dot; two breaths say "this is now" as well.
    /// </summary>
    public abstract class SkyPathView : ContentView
    {
        public static readonly BindableProperty SnapshotProperty = BindableProperty.Create(
            nameof(Snapshot), typeof(CelestialSnapshot), typeof(SkyPathView), null,
            propertyChanged: (b, o, n) => ((SkyPathView)b).OnSnapshotChanged(o as CelestialSnapshot, n as CelestialSnapshot));

        public static readonly BindableProperty CurrentTimeProperty = BindableProperty.Create(
            nameof(CurrentTime), typeof(DateTime?), typeof(SkyPathView), null,
            propertyChanged: (b, _, _) => ((SkyPathView)b)._overlay.Invalidate());

        private const string RevealAnimation = "SkyPathReveal";
        private const string PulseAnimation = "SkyPathPulse";

        private readonly GraphicsView _sky;
        private readonly GraphicsView _overlay;

        protected SkyPathView()
        {
            _sky = new GraphicsView { Drawable = new Layer(DrawSky) };
            _overlay = new GraphicsView { Drawable = new Layer(DrawOverlay), BackgroundColor = Colors.Transparent };
            Content = new Grid { Children = { _sky, _overlay } };
            HeightRequest = 160;

            // A picture of what the rows under it say in words; a screen reader reads those.
            AutomationProperties.SetIsInAccessibleTree(this, false);

            // Application.Current outlives every page, so a subscription taken here and never
            // released would keep the chart alive for the life of the app. Tied to the visual tree.
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>The day being shown.</summary>
        public CelestialSnapshot? Snapshot
        {
            get => (CelestialSnapshot?)GetValue(SnapshotProperty);
            set => SetValue(SnapshotProperty, value);
        }

        /// <summary>The time at the location, or null when the day shown is not today.</summary>
        public DateTime? CurrentTime
        {
            get => (DateTime?)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        /// <summary>0..1 while the arc draws itself in; 1 the rest of the time.</summary>
        protected float Reveal { get; private set; } = 1f;

        /// <summary>The marker's breath, around 1; exactly 1 once it has settled.</summary>
        protected float Pulse { get; private set; } = 1f;

        /// <summary>How far the marker breathes either side of 1, and how long each way takes.</summary>
        protected abstract (float Low, float High, uint HalfBreathMillis) PulseShape { get; }

        /// <summary>Where the horizon is drawn, for a chart of this height.</summary>
        protected static float HorizonY(RectF rect) => rect.Top + rect.Height * SkyPainter.HorizonFraction;

        /// <summary>The body's rise and set, as hours of the day shown, and whether it is up all day.</summary>
        protected abstract (double? Rise, double? Set, bool AlwaysUp) Span(CelestialSnapshot snapshot);

        protected abstract void DrawSky(ICanvas canvas, RectF rect);

        protected abstract void DrawOverlay(ICanvas canvas, RectF rect);

        /// <summary>The hour now, when the day shown is today.</summary>
        protected double? NowHour =>
            CurrentTime is { } now && Snapshot is { } snapshot && now.Date == snapshot.GeoDate.Date
                ? now.TimeOfDay.TotalHours
                : null;

        /// <summary>The body's path for the day shown, laid out for <paramref name="rect"/>.</summary>
        private protected SkyPath? PathFor(RectF rect)
        {
            if (Snapshot is not { } snapshot) return null;
            var (rise, set, alwaysUp) = Span(snapshot);
            return SkyPath.Of(rise, set, rect.Width, rect.Height, HorizonY(rect), alwaysUp);
        }

        private void OnSnapshotChanged(CelestialSnapshot? previous, CelestialSnapshot? current)
        {
            _sky.Invalidate();

            // Drawn in again only when there is a different arc to draw: the Today screen hands
            // over a new snapshot for the same day when it refreshes, and that is not news.
            var before = previous is null ? default : Span(previous);
            var after = current is null ? default : Span(current);
            if (before != after && IsLoaded)
            {
                StartReveal();
            }
            else
            {
                _overlay.Invalidate();
            }
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeChanged -= OnThemeChanged;
                app.RequestedThemeChanged += OnThemeChanged;
            }

            StartReveal();
            StartPulse();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeChanged -= OnThemeChanged;
            }

            this.AbortAnimation(RevealAnimation);
            this.AbortAnimation(PulseAnimation);
            Reveal = 1f;
            Pulse = 1f;
        }

        private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            _sky.Invalidate();
            _overlay.Invalidate();
        }

        private void StartReveal()
        {
            this.AbortAnimation(RevealAnimation);
            this.Animate(RevealAnimation, v =>
            {
                Reveal = (float)v;
                _overlay.Invalidate();
            }, 0, 1, rate: 16, length: 900, easing: Easing.CubicOut, finished: (_, _) =>
            {
                Reveal = 1f;
                _overlay.Invalidate();
            });
        }

        private void StartPulse()
        {
            const int breaths = 2;
            var (low, high, half) = PulseShape;

            void Breathe(double v)
            {
                Pulse = (float)v;
                _overlay.Invalidate();
            }

            // Each breath is low to high and back; then a half-length settle onto 1.
            var steps = breaths * 2 + 0.5;
            var pulse = new Animation();
            var at = 0.0;
            for (var i = 0; i < breaths; i++)
            {
                pulse.Add(at / steps, (at + 1) / steps, new Animation(Breathe, low, high, Easing.SinInOut));
                at++;
                pulse.Add(at / steps, (at + 1) / steps, new Animation(Breathe, high, low, Easing.SinInOut));
                at++;
            }
            pulse.Add(at / steps, 1, new Animation(Breathe, low, 1, Easing.SinOut));

            this.AbortAnimation(PulseAnimation);
            // Thirty frames a second: a slow breath needs no more.
            pulse.Commit(this, PulseAnimation, rate: 33, length: (uint)(half * steps), finished: (_, _) => Breathe(1));
        }

        /// <summary>The hours from the start of the day shown to <paramref name="time"/>.</summary>
        protected static double? HourOfDay(DateTime? time, DateTime day) =>
            time is { } t ? (t - day.Date).TotalHours : null;

        private sealed class Layer(Action<ICanvas, RectF> draw) : IDrawable
        {
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                if (dirtyRect.Width > 0 && dirtyRect.Height > 0)
                {
                    draw(canvas, dirtyRect);
                }
            }
        }
    }
}
