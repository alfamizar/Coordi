using Compute.Core.Domain.Entities.Models.Distance;

namespace JustCompute.Presentation.Helpers;

public static class Settings
{
    public static AppTheme Theme
    {
        get
        {
            Enum.TryParse<AppTheme>(Preferences.Get(nameof(Theme), Enum.GetName(AppTheme.Light)), true, out var appTheme);
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

    public static bool Is24HourTimeFormat
    {
        get => Preferences.Get(nameof(Is24HourTimeFormat), true);
        set => Preferences.Set(nameof(Is24HourTimeFormat), value);
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
