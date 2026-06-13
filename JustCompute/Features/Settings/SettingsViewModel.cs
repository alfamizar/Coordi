using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.Helpers;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using System.Collections.ObjectModel;
using Compute.Core.Domain.Entities.Models.Distance;
using Compute.Core.Domain.Entities.Models.Speed;
using System.Windows.Input;

namespace JustCompute.Features.Settings
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ThemeHandler _themeHandler;
        private readonly IStringLocalizer<AppStringsRes> _localizer;

        [ObservableProperty]
        private ObservableCollection<ThemeOption> themeOptions = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSystemThemeSelected))]
        [NotifyPropertyChangedFor(nameof(IsLightThemeSelected))]
        [NotifyPropertyChangedFor(nameof(IsDarkThemeSelected))]
        private ThemeOption selectedThemeOption = null!;

        public bool IsSystemThemeSelected => SelectedThemeOption?.Theme == AppTheme.Unspecified;
        public bool IsLightThemeSelected => SelectedThemeOption?.Theme == AppTheme.Light;
        public bool IsDarkThemeSelected => SelectedThemeOption?.Theme == AppTheme.Dark;

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

        public ICommand SaveSelectedDistanceTypeToSettingsCommand => Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)];
        public ICommand SelectThemeCommand => Commands[nameof(SelectThemeCommand)];
        public ICommand SelectSpeedUnitCommand => Commands[nameof(SelectSpeedUnitCommand)];

        partial void OnIs24HourTimeFormatChanged(bool value) => global::JustCompute.Shared.Helpers.Settings.Is24HourTimeFormat = value;

        partial void OnSelectedThemeOptionChanged(ThemeOption value)
        {
            if (value is null) return;
            global::JustCompute.Shared.Helpers.Settings.Theme = value.Theme;
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

            Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)] = new Command(OnSaveSelectedDistanceTypeToSettings);
            Commands[nameof(SelectThemeCommand)] = new Command<string>(OnSelectTheme);
            Commands[nameof(SelectSpeedUnitCommand)] = new Command<string>(OnSelectSpeedUnit);

            ThemeOptions =
            [
                new ThemeOption(AppTheme.Unspecified, _localizer.GetString("SystemThemeLabel")),
                new ThemeOption(AppTheme.Light, _localizer.GetString("LightThemeLabel")),
                new ThemeOption(AppTheme.Dark, _localizer.GetString("DarkThemeLabel"))
            ];
            SelectedThemeOption = ThemeOptions.FirstOrDefault(o => o.Theme == global::JustCompute.Shared.Helpers.Settings.Theme)
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
            SelectedDistanceUnitOfMeasure = distanceType switch
            {
                DistanceType.Meters => new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel")),
                DistanceType.Kilometers => new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel")),
                DistanceType.Miles => new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel")),
                DistanceType.Feets => new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel")),
                DistanceType.NauticalMiles => new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel")),
                _ => SelectedDistanceUnitOfMeasure
            };

            Is24HourTimeFormat = global::JustCompute.Shared.Helpers.Settings.Is24HourTimeFormat;
        }

        private void OnSaveSelectedDistanceTypeToSettings()
        {
            global::JustCompute.Shared.Helpers.Settings.DistanceType = SelectedDistanceUnitOfMeasure.DistanceType;
        }

        private void OnSelectTheme(string? themeName)
        {
            if (string.IsNullOrEmpty(themeName)) return;
            if (!Enum.TryParse<AppTheme>(themeName, true, out var theme)) return;
            var option = ThemeOptions.FirstOrDefault(o => o.Theme == theme);
            if (option != null) SelectedThemeOption = option;
        }

        private void OnSelectSpeedUnit(string? speedTypeName)
        {
            if (string.IsNullOrEmpty(speedTypeName)) return;
            if (!Enum.TryParse<SpeedType>(speedTypeName, true, out var speedType)) return;
            var option = SpeedOptions.FirstOrDefault(o => o.SpeedType == speedType);
            if (option != null) SelectedSpeedOption = option;
        }
    }
}
