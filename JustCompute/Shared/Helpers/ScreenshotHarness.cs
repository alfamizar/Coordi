using System.Diagnostics;
using System.Globalization;
using JustCompute.Shared.Theming;
using Compute.Core.Domain.Services;
using JustCompute.Services;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// DEBUG-only harness for capturing localized screenshots of any screen (not just the
    /// launch screen), deterministically.
    ///
    /// Two inputs (both optional), read at startup:
    ///   * a Shell route to deep-link to  (e.g. "//sun", "//settings")
    ///   * a fixed location (lat/lon/name) to seed as the SelectedLocation, so Sun/Moon/Eclipse
    ///     screens show identical, comparable content across languages instead of depending on
    ///     GPS. Each screen recomputes from SelectedLocation when navigated to, so seeding then
    ///     navigating yields deterministic data on the target screen.
    ///
    /// Input channels (first non-empty wins):
    ///   * iOS / desktop: environment variables (e.g. iOS simulator via SIMCTL_CHILD_*):
    ///       COORDI_SCREENSHOT_ROUTE, COORDI_SCREENSHOT_LAT, COORDI_SCREENSHOT_LON, COORDI_SCREENSHOT_NAME
    ///   * Android: intent extras (env vars don't reach Android apps), copied here by MainActivity:
    ///       --es coordi_route //sun --es coordi_lat 48.8566 --es coordi_lon 2.3522 --es coordi_name Paris
    ///
    /// All call sites are removed in Release via <see cref="ConditionalAttribute"/>.
    /// </summary>
    public static class ScreenshotHarness
    {
        public const string RouteEnv = "COORDI_SCREENSHOT_ROUTE";
        public const string LatEnv = "COORDI_SCREENSHOT_LAT";
        public const string LonEnv = "COORDI_SCREENSHOT_LON";
        public const string NameEnv = "COORDI_SCREENSHOT_NAME";
        public const string ThemeEnv = "COORDI_SCREENSHOT_THEME";

        /// <summary>
        /// Whether the eclipse screens show only what is observable from the seeded location.
        /// A persisted preference, so without this a capture inherits whatever the last person
        /// to touch the device left it on — and the store listing is not the place to find that
        /// out.
        /// </summary>
        public const string OnlyVisibleEclipsesEnv = "COORDI_SCREENSHOT_ONLY_VISIBLE";

        // Populated by the Android MainActivity from intent extras.
        public static string? RouteFromPlatform;
        public static string? LatFromPlatform;
        public static string? LonFromPlatform;
        public static string? NameFromPlatform;
        public static string? ThemeFromPlatform;
        public static string? OnlyVisibleEclipsesFromPlatform;

        private static string? Pick(string? platformValue, string envKey)
        {
            if (!string.IsNullOrWhiteSpace(platformValue))
            {
                return platformValue!.Trim();
            }

            string? env = Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
        }

        /// <summary>Seed the fixed location (if provided) and deep-link to the requested route (if provided).</summary>
        [Conditional("DEBUG")]
        public static void Apply()
        {
            try
            {
                ApplyTheme();
                ApplyEclipseFilter();
                SeedLocation();
                Navigate();
            }
            catch
            {
                // A screenshot aid must never crash startup.
            }
        }

        private static void ApplyEclipseFilter()
        {
            string? value = Pick(OnlyVisibleEclipsesFromPlatform, OnlyVisibleEclipsesEnv);
            if (value is null) return;

            if (bool.TryParse(value, out bool onlyVisible))
            {
                Settings.ShowOnlyVisibleEclipses = onlyVisible;
            }
        }

        private static void ApplyTheme()
        {
            string? theme = Pick(ThemeFromPlatform, ThemeEnv);
            if (theme is null)
            {
                return;
            }

            // "light" and "dark" are kept as aliases for the default palette on each side, so
            // existing capture scripts keep working now that themes have names.
            AppThemeId? mapped = theme.ToLowerInvariant() switch
            {
                "light" or "ocean" => AppThemeId.Ocean,
                "blossom" or "pink" => AppThemeId.Blossom,
                "dark" or "midnight" => AppThemeId.Midnight,
                "ember" or "orange" => AppThemeId.Ember,
                "system" or "default" or "unspecified" => AppThemeId.System,
                _ => null,
            };
            if (mapped is null)
            {
                return;
            }

            // Drive the app's own theme exactly like the Settings toggle does:
            // persist the choice and re-run ThemeHandler (sets UserAppTheme + nav/status bars).
            Settings.ThemeId = mapped.Value;

            ThemeHandler? handler = ServicesProvider.GetService<ThemeHandler>();
            if (handler is not null)
            {
                handler.SetTheme();
            }
            else if (Application.Current is not null)
            {
                // No handler resolved (very early startup): at least land on the right side of
                // light/dark, so the capture is not taken against the wrong background.
                Application.Current.UserAppTheme =
                    AppThemes.For(mapped.Value, Application.Current.RequestedTheme).IsDark
                        ? AppTheme.Dark
                        : AppTheme.Light;
            }
        }

        private static bool TryParseCoordinate(string? value, out double result) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        private static void SeedLocation()
        {
            string? latStr = Pick(LatFromPlatform, LatEnv);
            string? lonStr = Pick(LonFromPlatform, LonEnv);

            // Invariant, not the current culture. The coordinates arrive as "35.6762" from a
            // shell script, but the harness runs under whichever locale is being captured: in
            // German that period is a group separator, so TryParse *succeeds* and hands back
            // 356762. Every comma-decimal locale was screenshotted at a nonsense location —
            // wrong sun times, and a weather request the API rejects.
            if (!TryParseCoordinate(latStr, out double lat) ||
                !TryParseCoordinate(lonStr, out double lon))
            {
                return;
            }

            IGPSLocationService? gps = ServicesProvider.GetService<IGPSLocationService>();
            if (gps is null)
            {
                return;
            }

            gps.SelectedLocation = new Location
            {
                Name = Pick(NameFromPlatform, NameEnv) ?? "Screenshot",
                Latitude = lat,
                Longitude = lon,
                IsCurrent = true,
            };
        }

        private static void Navigate()
        {
            string? route = Pick(RouteFromPlatform, RouteEnv);
            if (route is null)
            {
                return;
            }

            if (!route.StartsWith("//", StringComparison.Ordinal))
            {
                route = "//" + route.TrimStart('/');
            }

            Shell? shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            string target = route;
            // Let the Shell settle one tick before navigating.
            shell.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Task.Delay(300);
                    await shell.GoToAsync(target);
                }
                catch
                {
                    // Ignore navigation failures (e.g. unknown route) — keep the app usable.
                }
            });
        }
    }
}
