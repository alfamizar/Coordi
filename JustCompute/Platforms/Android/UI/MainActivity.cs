using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
#if DEBUG
using JustCompute.Shared.Helpers;
#endif

namespace JustCompute.Platforms.Android.UI;

[Activity(
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true, 
    ConfigurationChanges = ConfigChanges.ScreenSize | 
    ConfigChanges.Orientation | 
    ConfigChanges.UiMode | 
    ConfigChanges.ScreenLayout | 
    ConfigChanges.SmallestScreenSize | 
    ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
#if DEBUG
        // Screenshot harness inputs (env vars don't reach Android apps, so use intent extras):
        //   adb shell am start -n <component> --es coordi_route //sun --es coordi_lat 48.8566 --es coordi_lon 2.3522
        ScreenshotHarness.RouteFromPlatform = Intent?.GetStringExtra("coordi_route");
        ScreenshotHarness.LatFromPlatform = Intent?.GetStringExtra("coordi_lat");
        ScreenshotHarness.LonFromPlatform = Intent?.GetStringExtra("coordi_lon");
        ScreenshotHarness.NameFromPlatform = Intent?.GetStringExtra("coordi_name");
        ScreenshotHarness.ThemeFromPlatform = Intent?.GetStringExtra("coordi_theme");
#endif

        base.OnCreate(savedInstanceState);

        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
    }
}
