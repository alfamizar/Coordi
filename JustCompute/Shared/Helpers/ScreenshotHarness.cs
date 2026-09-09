using System.Diagnostics;
using System.Globalization;
using JustCompute.Shared.Theming;
using Compute.Core.Domain.Services;
using Compute.Core.Repository;
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

        /// <summary>
        /// Places to save, as "Name,lat,lon" separated by ";". The Locations screen is empty on a
        /// fresh install, and a store screenshot of an empty list shows nothing of the app - not
        /// the rows, and not the edit and delete actions that live on them.
        /// </summary>
        public const string SavedLocationsEnv = "COORDI_SCREENSHOT_SAVED";

        /// <summary>
        /// Pins for the Ruler, same format. The route lives in memory, so it cannot be prepared
        /// beforehand the way saved places can: the capture script stops the app between every
        /// screen, and an unseeded Ruler opens with a single pin and no distances at all.
        /// </summary>
        public const string RouteStopsEnv = "COORDI_SCREENSHOT_STOPS";

        /// <summary>
        /// A finished trip for Speed and Distance, as "travelledMetres,directMetres,bearing".
        /// Its summary only exists while a trip is running, which no deep link can produce.
        /// </summary>
        public const string TripEnv = "COORDI_SCREENSHOT_TRIP";

        // Populated by the Android MainActivity from intent extras.
        public static string? RouteFromPlatform;
        public static string? LatFromPlatform;
        public static string? LonFromPlatform;
        public static string? NameFromPlatform;
        public static string? ThemeFromPlatform;
        public static string? OnlyVisibleEclipsesFromPlatform;
        public static string? SavedLocationsFromPlatform;
        public static string? RouteStopsFromPlatform;
        public static string? TripFromPlatform;

        /// <summary>
        /// Pins the Ruler should open with, or empty. Read by the view model when it seeds, so
        /// the harness does not have to reach into the screen.
        /// </summary>
        public static IReadOnlyList<Location> SeededRouteStops { get; private set; } = [];

        /// <summary>Travelled metres, straight-line metres and bearing, or null for no demo trip.</summary>
        public static (double Travelled, double Direct, int Bearing)? SeededTrip { get; private set; }

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
                SeedSavedLocations();
                SeedRouteStops();
                SeedTrip();
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

        /// <summary>Adds the listed places to the user's saved locations, skipping any already there.</summary>
        private static void SeedSavedLocations()
        {
            var wanted = ParsePlaces(Pick(SavedLocationsFromPlatform, SavedLocationsEnv));
            if (wanted.Count == 0) return;

            var repository = ServicesProvider.GetService<ISavedLocationsRepository>();
            if (repository is null) return;

            // Fire and forget: the harness runs during window creation and must not block it.
            // A duplicate run is harmless because existing names are skipped.
            _ = Task.Run(async () =>
            {
                try
                {
                    var existing = await repository.GetAllAsync();
                    foreach (var place in wanted)
                    {
                        if (existing.Any(l => string.Equals(l.Name, place.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                        await repository.AddAsync(place);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Screenshot harness could not seed saved locations: {ex}");
                }
            });
        }

        private static void SeedRouteStops() =>
            SeededRouteStops = ParsePlaces(Pick(RouteStopsFromPlatform, RouteStopsEnv));

        private static void SeedTrip()
        {
            var value = Pick(TripFromPlatform, TripEnv);
            if (value is null) return;

            var parts = value.Split(',');
            if (parts.Length < 2) return;
            if (!TryParseCoordinate(parts[0], out double travelled)) return;
            if (!TryParseCoordinate(parts[1], out double direct)) return;

            int bearing = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b)
                ? b
                : 0;

            SeededTrip = (travelled, direct, bearing);
        }

        /// <summary>"Name,lat,lon" entries separated by ";". Invariant parsing, as for the seed
        /// location - a comma-decimal locale would otherwise read 52.2297 as 522297.</summary>
        private static List<Location> ParsePlaces(string? value)
        {
            var places = new List<Location>();
            if (value is null) return places;

            foreach (var entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split(',');
                if (parts.Length < 3) continue;
                if (!TryParseCoordinate(parts[1], out double lat)) continue;
                if (!TryParseCoordinate(parts[2], out double lon)) continue;

                places.Add(new Location
                {
                    Name = parts[0].Trim(),
                    Latitude = lat,
                    Longitude = lon,
                });
            }
            return places;
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
