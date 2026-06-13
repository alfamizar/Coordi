using AndroidX.Core.View;
using Compute.Core.UI;
using JustCompute.Platforms.Android.Extensions;

namespace JustCompute.Platforms.Android.UI
{
    public class Environment : IEnvironment
    {
        public void SetNavigationBarColor(System.Drawing.Color color)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23)) return;

            var window = Platform.CurrentActivity?.Window;
            if (window == null) return;

            // Window.SetNavigationBarColor is deprecated under Android 15 (API 35) edge-to-edge, but it
            // still works on shipping devices and there is no non-deprecated API to paint the bar a
            // solid colour. Retained to keep the navigation bar consistent with the app's coloured bars.
#pragma warning disable CA1422
            window.SetNavigationBarColor(color.ColorSystemToAndroid());
#pragma warning restore CA1422
        }

        public void ResetNavigationBarColor()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(23)) return;

            var activity = Platform.CurrentActivity;
            var window = activity?.Window;
            var resources = activity?.Resources;
            if (window == null || resources == null) return;

            // See the note in SetNavigationBarColor: deprecated under edge-to-edge but still required
            // to paint the bar a solid colour, with no non-deprecated replacement.
#pragma warning disable CA1422
            window.SetNavigationBarColor(
                resources.GetColor(_Microsoft.Android.Resource.Designer.ResourceConstant.Color.background_material_dark, activity!.Theme));
#pragma warning restore CA1422
        }
    }
}
