using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.Helpers;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using System.Collections.ObjectModel;
using Compute.Core.Domain.Entities.Models.Distance;
using Compute.Core.Domain.Entities.Models.Speed;
using System.Windows.Input;
using JustCompute.Shared.Theming;

namespace JustCompute.Features.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ThemeHandler _themeHandler;
        private readonly IStringLocalizer<AppStringsRes> _localizer;

        [ObservableProperty]
        private ObservableCollection<ThemeOption> themeOptions = [];

        [ObservableProperty]
        private ThemeOption selectedThemeOption = null!;

        [ObservableProperty]
        private ObservableCollection<SpeedOption> speedOptions = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMetersPerSecondSelected))]
        [NotifyPropertyChangedFor(nameof(IsKilometersPerHourSelected))]
        [NotifyPropertyChangedFor(nameof(IsMilesPerHourSelected))]
        private SpeedOption selectedSpeedOption = null!;

        public bool IsMetersPerSecondSelected => SelectedSpeedOption?.SpeedType == SpeedType.MetersPerSecond;
        public bool IsKilometersPerHourSelected => SelectedSpeedOption?.SpeedType == SpeedType.KilometersPerHour;
        public bool IsMilesPerHourSelected => SelectedSpeedOption?.SpeedType == SpeedType.MilesPerHour;

        [ObservableProperty]
        private bool is24HourTimeFormat;


        [ObservableProperty]
        private ObservableCollection<DistanceUnitOfMeasure> distanceUnitOfMeasures = [];

        [ObservableProperty]
        private DistanceUnitOfMeasure selectedDistanceUnitOfMeasure = null!;


        partial void OnIs24HourTimeFormatChanged(bool value) => global::JustCompute.Shared.Helpers.Settings.Is24HourTimeFormat = value;

        /// <summary>
        /// Saves the chosen distance unit.
        ///
        /// This used to hang off an EventToCommandBehavior on the picker's SelectedIndexChanged,
        /// which was not firing — the picker showed the new unit, nothing was written, and every
        /// screen that reads the setting carried on in the old one. The theme and the clock
        /// format persist from their property hooks; this now does too.
        /// </summary>
        partial void OnSelectedDistanceUnitOfMeasureChanged(DistanceUnitOfMeasure value)
        {
            if (value is null) return;

            global::JustCompute.Shared.Helpers.Settings.DistanceType = value.DistanceType;
        }


        partial void OnSelectedThemeOptionChanged(ThemeOption value)
        {
            if (value is null) return;
            global::JustCompute.Shared.Helpers.Settings.ThemeId = value.Theme;
            ApplyTheme();
        }

        partial void OnSelectedSpeedOptionChanged(SpeedOption value)
        {
            if (value is null) return;
            global::JustCompute.Shared.Helpers.Settings.SpeedType = value.SpeedType;
        }

        private void ApplyTheme()
        {
            if (MainThread.IsMainThread)
            {
                _themeHandler.SetTheme();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(_themeHandler.SetTheme);
            }
        }

        public SettingsViewModel(
            ViewModelServices services,
            ThemeHandler themeHandler,
            IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _themeHandler = themeHandler;
            _localizer = localizer;


            ThemeOptions =
            [
                new ThemeOption(AppThemeId.System, _localizer.GetString("SystemThemeLabel")),
                new ThemeOption(AppThemeId.Ocean, _localizer.GetString("ThemeOceanLabel")),
                new ThemeOption(AppThemeId.Blossom, _localizer.GetString("ThemeBlossomLabel")),
                new ThemeOption(AppThemeId.Midnight, _localizer.GetString("ThemeMidnightLabel")),
                new ThemeOption(AppThemeId.Ember, _localizer.GetString("ThemeEmberLabel")),
            ];
            SelectedThemeOption = ThemeOptions.FirstOrDefault(o => o.Theme == global::JustCompute.Shared.Helpers.Settings.ThemeId)
                                  ?? ThemeOptions[0];

            SpeedOptions =
            [
                new SpeedOption(SpeedType.MetersPerSecond, _localizer.GetString("MetersPerSecondLabel")),
                new SpeedOption(SpeedType.KilometersPerHour, _localizer.GetString("KilometersPerHourLabel")),
                new SpeedOption(SpeedType.MilesPerHour, _localizer.GetString("MilesPerHourLabel"))
            ];
            SelectedSpeedOption = SpeedOptions.FirstOrDefault(o => o.SpeedType == global::JustCompute.Shared.Helpers.Settings.SpeedType)
                                  ?? SpeedOptions[0];

            DistanceUnitOfMeasures =
            [
                new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel")),
                new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel")),
                new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel")),
                new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel")),
                new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel"))
            ];
            DistanceType distanceType = global::JustCompute.Shared.Helpers.Settings.DistanceType;
            SelectedDistanceUnitOfMeasure =
                DistanceUnitOfMeasures.FirstOrDefault(o => o.DistanceType == distanceType)
                ?? DistanceUnitOfMeasures[0];

            Is24HourTimeFormat = global::JustCompute.Shared.Helpers.Settings.Is24HourTimeFormat;
        }

        [RelayCommand]
        private void SelectSpeedUnit(string? speedTypeName)
        {
            if (string.IsNullOrEmpty(speedTypeName)) return;
            if (!Enum.TryParse<SpeedType>(speedTypeName, true, out var speedType)) return;
            var option = SpeedOptions.FirstOrDefault(o => o.SpeedType == speedType);
            if (option != null) SelectedSpeedOption = option;
        }
    }
}
