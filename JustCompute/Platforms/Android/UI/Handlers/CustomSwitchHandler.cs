using Android.Widget;
using JustCompute.Presentation.ExtendedControls;
using Microsoft.Maui.Handlers;
using AndroidX.AppCompat.Widget;
using Android.Graphics;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using AndroidX.Core.Graphics;
using Android.OS;
using Android.Graphics.Drawables;
using Android.Content;
using Paint = Android.Graphics.Paint;

namespace JustCompute.Platforms.Android.UI.Handlers
{
    public class CustomSwitchHandler : SwitchHandler
    {
        private Drawable _originalThumbDrawable;

        protected override SwitchCompat CreatePlatformView()
        {
            return new SwitchCompat(Context);
        }

        protected override void ConnectHandler(SwitchCompat platformView)
        {
            base.ConnectHandler(platformView);

            _originalThumbDrawable = platformView.ThumbDrawable;

            if (VirtualView is CustomSwitch customSwitch)
            {
                UpdateThumbColor(platformView.Checked ? customSwitch.ThumbColor : customSwitch.OffColor);
                UpdateTrackColor(platformView.Checked ? customSwitch.TrackOnColor : customSwitch.TrackOffColor);
                UpdateThumbIcon(platformView.Checked ? customSwitch.ThumbOnIcon : customSwitch.ThumbOffIcon);
            }

            platformView.CheckedChange += OnCheckedChanged;
        }

        protected override void DisconnectHandler(SwitchCompat platformView)
        {
            platformView.CheckedChange -= OnCheckedChanged;
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        private void OnCheckedChanged(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (VirtualView is CustomSwitch customSwitch)
            {
                UpdateThumbColor(PlatformView.Checked ? customSwitch.OnColor : customSwitch.OffColor);
                UpdateTrackColor(PlatformView.Checked ? customSwitch.TrackOnColor : customSwitch.TrackOffColor);
                UpdateThumbIcon(PlatformView.Checked ? customSwitch.ThumbOnIcon : customSwitch.ThumbOffIcon);
            }
        }

        private void UpdateThumbColor(Microsoft.Maui.Graphics.Color color)
        {
            if (color == null) return;

            if (PlatformView.Checked)
            {
                PlatformView.ThumbDrawable.SetColorFilter(
                    BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(color.ToAndroid(), BlendModeCompat.SrcAtop));
            }
            else
            {
                PlatformView.ThumbDrawable.SetColorFilter(
                    BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(color.ToAndroid(), BlendModeCompat.SrcAtop));
            }
        }

        private void UpdateTrackColor(Microsoft.Maui.Graphics.Color color)
        {
            if (color == null) return;

            new Handler(Looper.MainLooper).Post(() =>
            {
                PlatformView.TrackDrawable.ClearColorFilter();
                PlatformView.TrackDrawable.SetColorFilter(
                    BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(color.ToAndroid(), BlendModeCompat.SrcAtop));
            });
        }

        private void UpdateThumbIcon(string text)
        {
            if (text == null) return;

            if (VirtualView is CustomSwitch customSwitch)
            {
                if (string.IsNullOrEmpty(customSwitch.ThumbOnIcon))
                {
                    PlatformView.ThumbDrawable = _originalThumbDrawable;
                    return;
                }

                TextDrawable textDrawable = new()
                {
                    Text = text,
                    TextSize = 40
                };
                Drawable[] drawabless = { _originalThumbDrawable, textDrawable };
                LayerDrawable layerDrawable = new(drawabless);
                PlatformView.ThumbDrawable = layerDrawable;
            }
        }

        private class TextDrawable : Drawable
        {
            private readonly Paint _paint;
            public string Text { get; set; }
            public float TextSize { get; set; }

            public TextDrawable()
            {
                _paint = new Paint()
                {
                    TextAlign = Paint.Align.Center,
                    Color = Microsoft.Maui.Graphics.Color.FromArgb("ffffff").ToAndroid()
                };
            }

            public override void Draw(Canvas canvas)
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    _paint.TextSize = TextSize;
                    canvas.DrawText(Text, Bounds.CenterX(), Bounds.CenterY() - ((_paint.Descent() + _paint.Ascent()) / 2), _paint);
                }
            }

            public override void SetAlpha(int alpha)
            {
                _paint.Alpha = alpha;
            }

            public override void SetColorFilter(ColorFilter colorFilter)
            {
                _paint.SetColorFilter(colorFilter);
            }

            public override int Opacity => (int)_paint.Alpha;
        }
    }
}