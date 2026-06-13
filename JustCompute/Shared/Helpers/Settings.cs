using Compute.Core.Domain.Entities.Models.Distance;
using Compute.Core.Domain.Entities.Models.Speed;

namespace JustCompute.Shared.Helpers;

public static class Settings
{
    public static AppTheme Theme
    {
        get
        {
            Enum.TryParse<AppTheme>(Preferences.Get(nameof(Theme), Enum.GetName(AppTheme.Unspecified)), true, out var appTheme);
            return appTheme;
        }
        set => Preferences.Set(nameof(Theme), value.ToString());
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

    public static bool IsWifiOnlyEnabled
    {
        get => Preferences.Get(nameof(IsWifiOnlyEnabled), false);
        set => Preferences.Set(nameof(IsWifiOnlyEnabled), value);
    }

    public static DateTime Birthday
    {
        get => Preferences.Get(nameof(Birthday), new DateTime());
        set => Preferences.Set(nameof(Birthday), value);
    }
}
