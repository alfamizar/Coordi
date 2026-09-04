using Compute.Core.Domain.Entities.Models.Distance;
using JustCompute.Shared.Theming;
using Compute.Core.Domain.Entities.Models.Speed;

namespace JustCompute.Shared.Helpers;

public static class Settings
{
    /// <summary>
    /// The chosen theme. Stored by name so the enum may be reordered freely.
    ///
    /// Falls back to whatever the old <c>Theme</c> preference held, so an install that predates
    /// named themes keeps the side it was on: Light becomes Ocean, Dark becomes Midnight.
    /// </summary>
    public static AppThemeId ThemeId
    {
        get
        {
            string? stored = Preferences.Get(nameof(ThemeId), null);
            if (stored is not null && Enum.TryParse<AppThemeId>(stored, true, out var themeId))
            {
                return themeId;
            }

            return MigrateFromLegacyTheme();
        }
        set => Preferences.Set(nameof(ThemeId), value.ToString());
    }

    private static AppThemeId MigrateFromLegacyTheme()
    {
        Enum.TryParse<AppTheme>(
            Preferences.Get("Theme", Enum.GetName(AppTheme.Unspecified)), true, out var legacy);

        return legacy switch
        {
            AppTheme.Light => AppThemeId.Ocean,
            AppTheme.Dark => AppThemeId.Midnight,
            _ => AppThemeId.System,
        };
    }

    public static DistanceType DistanceType
    {
        get
        {
            Enum.TryParse<DistanceType>(Preferences.Get(nameof(DistanceType), Enum.GetName(DistanceType.Kilometers)), true, out var distanceType);
            return distanceType;
        }
        set => Preferences.Set(nameof(DistanceType), value.ToString());
    }

    /// <summary>
    /// Whether the Optics calculator reopens with the kit last entered. Off by default: a
    /// calculator that silently remembers is one that quietly hands you somebody else's answer.
    /// </summary>
    public static bool RememberOpticsInputs
    {
        get => Preferences.Get(nameof(RememberOpticsInputs), false);
        set => Preferences.Set(nameof(RememberOpticsInputs), value);
    }

    /// <summary>
    /// The Optics inputs, stored as the text the user typed rather than parsed numbers — a field
    /// mid-edit is not always a valid number, and round-tripping through a double would rewrite
    /// what they were in the middle of typing.
    /// </summary>
    public static string OpticsFocalLengthMm
    {
        get => Preferences.Get(nameof(OpticsFocalLengthMm), "50");
        set => Preferences.Set(nameof(OpticsFocalLengthMm), value);
    }

    public static string OpticsAperture
    {
        get => Preferences.Get(nameof(OpticsAperture), "8");
        set => Preferences.Set(nameof(OpticsAperture), value);
    }

    public static string OpticsSubjectDistanceMeters
    {
        get => Preferences.Get(nameof(OpticsSubjectDistanceMeters), "5");
        set => Preferences.Set(nameof(OpticsSubjectDistanceMeters), value);
    }

    public static string OpticsSensorFormat
    {
        get => Preferences.Get(nameof(OpticsSensorFormat), "Full frame");
        set => Preferences.Set(nameof(OpticsSensorFormat), value);
    }

    public static string OpticsImageWidthPixels
    {
        get => Preferences.Get(nameof(OpticsImageWidthPixels), "6000");
        set => Preferences.Set(nameof(OpticsImageWidthPixels), value);
    }

    public static string OpticsDeclinationDeg
    {
        get => Preferences.Get(nameof(OpticsDeclinationDeg), "0");
        set => Preferences.Set(nameof(OpticsDeclinationDeg), value);
    }

    public static SpeedType SpeedType
    {
        get
        {
            Enum.TryParse<SpeedType>(Preferences.Get(nameof(SpeedType), Enum.GetName(SpeedType.MetersPerSecond)), true, out var speedType);
            return speedType;
        }
        set => Preferences.Set(nameof(SpeedType), value.ToString());
    }

    // Cached in memory: TimeFormatConverter reads this on every visible label during scroll
    // (5+ calls per row on the eclipse/cycle pages). Reads Preferences once, then mirrors writes.
    private static bool? _is24HourTimeFormatCache;
    public static bool Is24HourTimeFormat
    {
        get => _is24HourTimeFormatCache ??= Preferences.Get(nameof(Is24HourTimeFormat), true);
        set
        {
            _is24HourTimeFormatCache = value;
            Preferences.Set(nameof(Is24HourTimeFormat), value);
        }
    }

    /// <summary>
    /// When true the eclipse screens list only eclipses actually observable from the selected
    /// location; when false they show the whole century's catalogue with the unobservable ones
    /// marked. Defaults to showing everything: knowing an eclipse exists but misses you is more
    /// useful than the event silently not being there.
    /// </summary>
    public static bool ShowOnlyVisibleEclipses
    {
        get => Preferences.Get(nameof(ShowOnlyVisibleEclipses), false);
        set => Preferences.Set(nameof(ShowOnlyVisibleEclipses), value);
    }

    /// <summary>
    /// False until the user explicitly picks a location. While false the app is showing the
    /// placeholder, and the onboarding card says so rather than pretending the data is theirs.
    /// </summary>
    public static bool HasUserSetLocation
    {
        get => Preferences.Get(nameof(HasUserSetLocation), false);
        set => Preferences.Set(nameof(HasUserSetLocation), value);
    }

    public static bool IsWifiOnlyEnabled
    {
        get => Preferences.Get(nameof(IsWifiOnlyEnabled), false);
        set => Preferences.Set(nameof(IsWifiOnlyEnabled), value);
    }

    // Whether the user has seen and accepted the prominent disclosure shown before the app starts
    // collecting location in the background for trip tracking (Google Play location policy). Shown
    // once; once accepted we don't prompt again.
    public static bool HasAcceptedBackgroundLocationDisclosure
    {
        get => Preferences.Get(nameof(HasAcceptedBackgroundLocationDisclosure), false);
        set => Preferences.Set(nameof(HasAcceptedBackgroundLocationDisclosure), value);
    }

    /// <summary>
    /// The Ruler's fuel figures, kept as the text the user typed rather than parsed numbers — a
    /// field mid-edit is not always a valid number, and round-tripping through a double would
    /// rewrite what they were in the middle of typing. Same reasoning as the Optics inputs.
    /// </summary>
    public static string FuelConsumption
    {
        get => Preferences.Get(nameof(FuelConsumption), string.Empty);
        set => Preferences.Set(nameof(FuelConsumption), value);
    }

    /// <summary>
    /// How the reader quotes fuel consumption. Empty until they choose, so the first visit can
    /// guess from their region rather than assuming everyone counts litres per 100 km.
    /// </summary>
    public static string FuelConsumptionMode
    {
        get => Preferences.Get(nameof(FuelConsumptionMode), string.Empty);
        set => Preferences.Set(nameof(FuelConsumptionMode), value);
    }

    public static string FuelPrice
    {
        get => Preferences.Get(nameof(FuelPrice), string.Empty);
        set => Preferences.Set(nameof(FuelPrice), value);
    }

    public static DateTime Birthday
    {
        get => Preferences.Get(nameof(Birthday), new DateTime());
        set => Preferences.Set(nameof(Birthday), value);
    }
}
