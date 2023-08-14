﻿using Compute.Core.Domain.Entities.Models.Distance;

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
            Enum.TryParse<DistanceType>(Preferences.Get(nameof(DistanceType), nameof(DistanceType.Kilometers)), true, out var distanceType);
            return distanceType;
        }
        set => Preferences.Set(nameof(DistanceType), value.ToString());
    }

    public static bool IsWifiOnlyEnabled
    {
        get => Preferences.Get(nameof(IsWifiOnlyEnabled), false);
        set => Preferences.Set(nameof(IsWifiOnlyEnabled), value);
    }

    public static double CurrentPositionPlayer
    {
        get => Preferences.Get(nameof(CurrentPositionPlayer), 0.0);
        set => Preferences.Set(nameof(CurrentPositionPlayer), value);
    }

    public static string EpisodeId
    {
        get => Preferences.Get(nameof(EpisodeId), null);
        set => Preferences.Set(nameof(EpisodeId), value);
    }

    public static string EpisodeTitle
    {
        get => Preferences.Get(nameof(EpisodeTitle), null);
        set => Preferences.Set(nameof(EpisodeTitle), value);
    }

    public static string EpisodeDescription
    {
        get => Preferences.Get(nameof(EpisodeDescription), null);
        set => Preferences.Set(nameof(EpisodeDescription), value);
    }

    public static string EpisodeDuration
    {
        get => Preferences.Get(nameof(EpisodeDuration), null);
        set => Preferences.Set(nameof(EpisodeDuration), value);
    }

    public static string EpisodeUrl
    {
        get => Preferences.Get(nameof(EpisodeUrl), null);
        set => Preferences.Set(nameof(EpisodeUrl), value);
    }

    public static DateTime EpisodePublished
    {
        get => Preferences.Get(nameof(EpisodePublished), new DateTime());
        set => Preferences.Set(nameof(EpisodePublished), value);
    }
}
