using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Storage;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// DEBUG-only helper to force the app UI language for localization testing,
    /// WITHOUT changing the device/system language.
    ///
    /// Resolution order (first non-empty wins):
    ///   1. Environment variable <c>COORDI_UI_CULTURE</c>
    ///        - iOS simulator: pass it through with the SIMCTL_CHILD_ prefix, e.g.
    ///          <c>SIMCTL_CHILD_COORDI_UI_CULTURE=fr-FR xcrun simctl launch booted com.cutecompute.coordi</c>
    ///        - Desktop/dev runs: just set the env var before launching.
    ///   2. Preference <c>debug_ui_culture</c> (persisted; can be set from a future debug menu).
    ///
    /// On Android 13+ the cleanest option needs NO code — set the OS per-app locale:
    ///   <c>adb shell cmd locale set-app-locales com.cutecompute.coordi --locales fr-FR</c>
    ///
    /// The call site is removed entirely in Release builds via <see cref="ConditionalAttribute"/>,
    /// so this can never affect shipping behavior.
    /// </summary>
    public static class DebugCulture
    {
        public const string EnvVarName = "COORDI_UI_CULTURE";
        public const string PreferenceKey = "debug_ui_culture";

        [Conditional("DEBUG")]
        public static void ApplyOverrideIfPresent()
        {
            try
            {
                string code = Environment.GetEnvironmentVariable(EnvVarName) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(code))
                {
                    code = Preferences.Default.Get(PreferenceKey, string.Empty);
                }

                if (string.IsNullOrWhiteSpace(code))
                {
                    return;
                }

                CultureInfo culture = new(code.Trim());

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Invalid override code — fall back to the system culture.
            }
            catch (Exception)
            {
                // A debug aid must never crash startup.
            }
        }
    }
}
