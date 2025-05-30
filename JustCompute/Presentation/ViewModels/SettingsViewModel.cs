using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.Helpers;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using System.Collections.ObjectModel;
using Compute.Core.Domain.Entities.Models.Distance;
using System.Windows.Input;
using JustCompute.Services;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ThemeHandler _themeHandler;
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        [ObservableProperty]
        private bool isDarkModeEnabled;

        [ObservableProperty]
        private bool is24HourTimeFormat;

        [ObservableProperty]
        private ObservableCollection<DistanceUnitOfMeasure> distanceUnitOfMeasures;

        [ObservableProperty]
        private DistanceUnitOfMeasure selectedDistanceUnitOfMeasure = new (DistanceType.Kilometers, _localizer.GetString("KmLabel"));

        public ICommand SaveSelectedDistanceTypeToSettingsCommand => Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)];

        partial void OnIsDarkModeEnabledChanged(bool value) =>
            ChangeUserAppTheme(value);

        partial void OnIs24HourTimeFormatChanged(bool value) => Settings.Is24HourTimeFormat = value;

        public SettingsViewModel(ThemeHandler themeHandler)
        {
            _themeHandler = themeHandler;

            Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)] = new Command(OnSaveSelectedDistanceTypeToSettings);

            DistanceUnitOfMeasures =
        [
                new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel")),
                new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel")),
                new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel")),
                new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel")),
                new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel"))
        ];
            DistanceType distanceType = Settings.DistanceType;
            SelectedDistanceUnitOfMeasure = distanceType switch
            {
                DistanceType.Meters => new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel")),
                DistanceType.Kilometers => new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel")),
                DistanceType.Miles => new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel")),
                DistanceType.Feets => new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel")),
                DistanceType.NauticalMiles => new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel")),
                _ => SelectedDistanceUnitOfMeasure
            };

            Is24HourTimeFormat = Settings.Is24HourTimeFormat;
        }

        private void OnSaveSelectedDistanceTypeToSettings()
        {
            Settings.DistanceType = SelectedDistanceUnitOfMeasure.DistanceType;
        }

        void ChangeUserAppTheme(bool activateDarkMode)
        {
            Settings.Theme = activateDarkMode
                ? AppTheme.Dark
                : AppTheme.Light;

            _themeHandler.SetTheme();
        }
    }
}
