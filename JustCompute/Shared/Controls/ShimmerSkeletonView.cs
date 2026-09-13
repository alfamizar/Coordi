namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// A placeholder that mimics the shape of the seven-day weather strip while the forecast is
    /// in flight, so the card keeps its height and the layout below does not jump when the data
    /// lands. Hand-rolled on <see cref="GraphicsView"/> to match <see cref="SunPathView"/> and
    /// <c>DayNightTimelineBar</c> rather than pulling in a dependency for one control.
    /// </summary>
    public class ShimmerSkeletonView : GraphicsView
    {
        private const string ShimmerAnimation = "shimmer";
        private const uint SweepMilliseconds = 1400;

        public static readonly BindableProperty CellCountProperty = BindableProperty.Create(
            nameof(CellCount), typeof(int), typeof(ShimmerSkeletonView), 7,
            propertyChanged: (bindable, _, _) => ((ShimmerSkeletonView)bindable).Invalidate());

        /// <summary>How many placeholder columns to draw. Defaults to the forecast's seven days.</summary>
        public int CellCount
        {
            get => (int)GetValue(CellCountProperty);
            set => SetValue(CellCountProperty, value);
        }

        private readonly SkeletonDrawable _drawable = new();

        public ShimmerSkeletonView()
        {
            Drawable = _drawable;
            HeightRequest = 128;

            Loaded += (_, _) => StartShimmer();
            Unloaded += (_, _) => StopShimmer();
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // No point burning frames on a control nobody can see.
            if (propertyName == nameof(IsVisible))
            {
                if (IsVisible) StartShimmer();
                else StopShimmer();
            }
        }

        private void StartShimmer()
        {
            StopShimmer();
            if (!IsVisible) return;

            _drawable.CellCount = CellCount;
            new Animation(progress =>
            {
                _drawable.Progress = progress;
                Invalidate();
            }, 0.0, 1.0).Commit(this, ShimmerAnimation, length: SweepMilliseconds, repeat: () => IsVisible);
        }

        private void StopShimmer() => this.AbortAnimation(ShimmerAnimation);

        private sealed class SkeletonDrawable : IDrawable
        {
            /// <summary>Sweep position, 0..1, driven by the animation.</summary>
            public double Progress { get; set; }

            public int CellCount { get; set; } = 7;

            public void Draw(ICanvas canvas, RectF rect)
            {
                var cells = Math.Max(1, CellCount);
                var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

                var baseColor = isDark ? Color.FromRgba(255, 255, 255, 28) : Color.FromRgba(0, 0, 0, 22);
                var glowColor = isDark ? Color.FromRgba(255, 255, 255, 52) : Color.FromRgba(255, 255, 255, 165);

                var cellWidth = rect.Width / cells;
                var padding = Math.Min(10f, cellWidth * 0.16f);

                for (var i = 0; i < cells; i++)
                {
                    var left = rect.Left + i * cellWidth + padding;
                    var width = cellWidth - padding * 2;
                    if (width <= 0) continue;

                    var centreX = left + width / 2f;
                    canvas.FillColor = baseColor;

                    // Day name, date, icon, temperature — the four rows of a real cell.
                    canvas.FillRoundedRectangle(left, rect.Top + 14f, width, 10f, 5f);
                    canvas.FillRoundedRectangle(left + width * 0.15f, rect.Top + 32f, width * 0.7f, 8f, 4f);
                    canvas.FillCircle(centreX, rect.Top + 62f, Math.Min(14f, width / 2f));
                    canvas.FillRoundedRectangle(left + width * 0.2f, rect.Top + 88f, width * 0.6f, 10f, 5f);
                    canvas.FillRoundedRectangle(left + width * 0.1f, rect.Top + 106f, width * 0.8f, 8f, 4f);
                }

                DrawSweep(canvas, rect, glowColor);
            }

            /// <summary>A soft highlight travelling left to right, the bit that reads as "loading".</summary>
            private void DrawSweep(ICanvas canvas, RectF rect, Color glowColor)
            {
                var bandWidth = rect.Width * 0.28f;
                // Start fully off the left edge and finish fully off the right one.
                var centre = (float)(-bandWidth + Progress * (rect.Width + bandWidth * 2));

                canvas.SaveState();
                canvas.ClipRectangle(rect);

                const int steps = 14;
                for (var i = 0; i < steps; i++)
                {
                    var offset = (i / (float)(steps - 1) - 0.5f) * bandWidth;
                    var falloff = 1f - Math.Abs(offset) / (bandWidth / 2f);
                    if (falloff <= 0) continue;

                    canvas.FillColor = glowColor.WithAlpha(glowColor.Alpha * falloff * 0.6f);
                    canvas.FillRectangle(centre + offset, rect.Top, bandWidth / steps + 1f, rect.Height);
                }

                canvas.RestoreState();
            }
        }
    }
}
