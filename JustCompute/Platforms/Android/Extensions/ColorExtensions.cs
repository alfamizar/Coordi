using AndroidColor = Android.Graphics.Color;
using SystemDrawingColor = System.Drawing.Color;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace JustCompute.Platforms.Android.Extensions
{
    public static partial class ColorExtensions
    {
        public static SystemDrawingColor ColorAndroidToSystem(this AndroidColor color) =>
            SystemDrawingColor.FromArgb(color.A, color.R, color.G, color.B);

        public static AndroidColor ColorSystemToAndroid(this SystemDrawingColor color) =>
            new(color.R, color.G, color.B, color.A);

        public static AndroidColor ColorMauiToAndroid(this MauiColor color)
        {
            color.ToRgba(out byte r, out byte g, out byte b, out byte a);
            return new AndroidColor(r, g, b, a);
        }
    }
}