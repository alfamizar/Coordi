using Microsoft.Maui.Graphics;

namespace JustCompute.Shared.Controls
{
    public class DayNightTimelineBar : GraphicsView
    {
        public static readonly BindableProperty RiseTimeProperty = BindableProperty.Create(
            nameof(RiseTime), typeof(DateTime?), typeof(DayNightTimelineBar), null,
            propertyChanged: OnVisualChanged);

        public static readonly BindableProperty SetTimeProperty = BindableProperty.Create(
            nameof(SetTime), typeof(DateTime?), typeof(DayNightTimelineBar), null,
            propertyChanged: OnVisualChanged);

        public static readonly BindableProperty IsMoonModeProperty = BindableProperty.Create(
            nameof(IsMoonMode), typeof(bool), typeof(DayNightTimelineBar), false,
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

        public bool IsMoonMode
        {
            get => (bool)GetValue(IsMoonModeProperty);
            set => SetValue(IsMoonModeProperty, value);
        }

        private readonly TimelineDrawable _drawable = new();

        public DayNightTimelineBar()
        {
            Drawable = _drawable;
            HeightRequest = 30;
            PushChanges();
        }

        private void PushChanges()
        {
            _drawable.RiseTime = RiseTime;
            _drawable.SetTime = SetTime;
            _drawable.IsMoonMode = IsMoonMode;
            Invalidate();
        }

        private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((DayNightTimelineBar)bindable).PushChanges();
        }

        private sealed class TimelineDrawable : IDrawable
        {
            public DateTime? RiseTime { get; set; }
            public DateTime? SetTime { get; set; }
            public bool IsMoonMode { get; set; }

            private static readonly Color SunDayColor = Color.FromArgb("#FFB300");
            private static readonly Color SunNightColor = Color.FromArgb("#1A237E");
            private static readonly Color MoonUpColor = Color.FromArgb("#D7D2E8");
            private static readonly Color MoonDownColor = Color.FromArgb("#0B1230");
            private static readonly Color TickColor = Color.FromArgb("#80FFFFFF");
            private static readonly Color LabelColor = Color.FromArgb("#B0FFFFFF");

            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                const float tickHeight = 4f;
                const float labelHeight = 10f;
                var width = dirtyRect.Width;
                var barHeight = dirtyRect.Height - tickHeight - labelHeight;
                if (barHeight <= 0) barHeight = dirtyRect.Height;

                var nightColor = IsMoonMode ? MoonDownColor : SunNightColor;
                var dayColor = IsMoonMode ? MoonUpColor : SunDayColor;

                canvas.FillColor = nightColor;
                canvas.FillRoundedRectangle(0, 0, width, barHeight, 4);

                if (RiseTime is { } rise && SetTime is { } set)
                {
                    var riseFrac = (float)(rise.TimeOfDay.TotalHours / 24.0);
                    var setFrac = (float)(set.TimeOfDay.TotalHours / 24.0);

                    canvas.FillColor = dayColor;
                    if (riseFrac <= setFrac)
                    {
                        FillSegment(canvas, width, barHeight, riseFrac, setFrac);
                    }
                    else
                    {
                        FillSegment(canvas, width, barHeight, 0f, setFrac);
                        FillSegment(canvas, width, barHeight, riseFrac, 1f);
                    }
                }

                canvas.StrokeColor = TickColor;
                canvas.StrokeSize = 1;
                canvas.FontColor = LabelColor;
                canvas.FontSize = 9;

                for (int h = 0; h <= 24; h += 6)
                {
                    var x = (h / 24f) * width;
                    var clampedX = Math.Clamp(x, 0.5f, width - 0.5f);
                    canvas.DrawLine(clampedX, barHeight, clampedX, barHeight + tickHeight);

                    var label = h.ToString("00");
                    var labelWidth = 24f;
                    var labelX = x - labelWidth / 2f;
                    if (h == 0) labelX = 0;
                    else if (h == 24) labelX = width - labelWidth;
                    canvas.DrawString(label, labelX, barHeight + tickHeight,
                        labelWidth, labelHeight,
                        HorizontalAlignment.Center, VerticalAlignment.Top);
                }
            }

            private static void FillSegment(ICanvas canvas, float width, float height, float fromFrac, float toFrac)
            {
                var x = fromFrac * width;
                var w = (toFrac - fromFrac) * width;
                if (w <= 0) return;
                canvas.FillRoundedRectangle(x, 0, w, height, 3);
            }
        }
    }
}
