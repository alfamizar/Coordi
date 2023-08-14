using Android.OS;
using AndroidX.Core.View;
using Compute.Core.Helpers.UI;
using JustCompute.Platforms.Android.Extensions;

namespace JustCompute.Platforms.Android.UI
{
    public class Environment : IEnvironment
    {
        public void SetStatusBarColor(System.Drawing.Color color, bool statusBarTint)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;

            var activity = Platform.CurrentActivity;
            var window = activity.Window;

            // this may not be necessary(but may be for older than M)
            //window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            //window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            //await Task.Delay(50);
            WindowCompat.GetInsetsController(window, window.DecorView).AppearanceLightStatusBars = statusBarTint;

            window.SetStatusBarColor(color.ColorSystemToAndroid());
        }

        public void SetNavigationBarColor(System.Drawing.Color color)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;

            var activity = Platform.CurrentActivity;
            var window = activity.Window;

            // this may not be necessary(but may be for older than M)
            //window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            //window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            //await Task.Delay(50);
            //WindowCompat.GetInsetsController(window, window.DecorView).AppearanceLightStatusBars = statusBarTint;
            window.SetNavigationBarColor(color.ColorSystemToAndroid());

            /*navbarBackupColor = window.NavigationBarColor;
            var colorr = color.ColorSystemToAndroid().ToArgb();*/

            /*window.SetNavigationBarColor(activity.Resources
                .GetColor(Resource.Color.background_material_dark, activity.Theme));*/
        }

        public void ResetNavigationBarColor()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M) return;

            var activity = Platform.CurrentActivity;
            var window = activity.Window;

            window.SetNavigationBarColor(
                activity.Resources.GetColor(Resource.Color.background_material_dark, activity.Theme));
        }
    }
}
