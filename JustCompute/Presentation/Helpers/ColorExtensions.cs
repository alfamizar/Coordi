using SystemDrawingColor = System.Drawing.Color;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace JustCompute.Presentation.Helpers
{
    public static class ColorExtensions
    {
        public static SystemDrawingColor ColorMauiToSystem(this MauiColor color)
        {
            return SystemDrawingColor.FromArgb((color).ToInt());
        }
    }
}
