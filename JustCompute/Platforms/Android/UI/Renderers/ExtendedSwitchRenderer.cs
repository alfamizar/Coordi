using Android.Content;
using Android.OS;
using Android.Widget;
using Microsoft.Maui.Controls.Compatibility.Platform.Android.AppCompat;
using Microsoft.Maui.Controls.Platform;
using AndroidX.Core.Graphics;
using Microsoft.Maui.Controls.Compatibility;
using Switch = Microsoft.Maui.Controls.Switch;
using AColor = Android.Graphics.Color;
using JustCompute.Presentation.ExtendedControls;
using JustCompute.Platforms.Android.UI.Renderers;
using JustCompute.Platforms.Android.Extensions;

[assembly: ExportRenderer(typeof(ExtendedSwitch), typeof(ExtendedSwitchRenderer))]

namespace JustCompute.Platforms.Android.UI.Renderers
{
    public class ExtendedSwitchRenderer(Context context) : SwitchRenderer(context)
    {
        private ExtendedSwitch? view;

        protected override void OnElementChanged(ElementChangedEventArgs<Switch> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || e.NewElement == null) return;
            view = (ExtendedSwitch)Element;

            if (Control != null)
            {
                if (Control.Checked)
                {
                    SetTrackColor(view.OnColor.ColorMauiToAndroid());
                }
                else
                {
                    SetTrackColor(view.OffColor.ColorMauiToAndroid());
                }
                Control.CheckedChange += OnCheckedChange;
            }
        }

        private void OnCheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
        {
            Element.IsToggled = Control.Checked;

            if (Control.Checked)
            {
                SetTrackColor(view?.OnColor.ColorMauiToAndroid());
            }
            else
            {
                SetTrackColor(view?.OffColor.ColorMauiToAndroid());
            }
        }

        private void SetTrackColor(AColor? color)
        {
            if (Looper.MainLooper != null && color != null && BlendModeCompat.SrcAtop != null)
            {
                new Handler(Looper.MainLooper).Post(() =>
                {
                    Control?.TrackDrawable?.ClearColorFilter();
                    Control?.TrackDrawable?.SetColorFilter(
                        BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(color.Value, BlendModeCompat.SrcAtop));
                });
            }

        }

        protected override void Dispose(bool disposing)
        {
            Control.CheckedChange -= OnCheckedChange;
            base.Dispose(disposing);
        }
    }
}