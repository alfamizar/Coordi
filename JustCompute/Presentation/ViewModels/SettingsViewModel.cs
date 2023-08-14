using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.Helpers;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using System.Collections.ObjectModel;
using Compute.Core.Domain.Entities.Models.Distance;
using System.Windows.Input;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ThemeHandler _themeHandler;
        private readonly IStringLocalizer<AppStringsRes> _localizer;

        [ObservableProperty]
        bool isDarkModeEnabled;

        [ObservableProperty]
        private ObservableCollection<DistanceUnitOfMeasure> distanceUnitOfMeasures;

        [ObservableProperty]
        private DistanceUnitOfMeasure selectedDistanceUnitOfMeasure;

        public ICommand SaveSelectedDistanceTypeToSettingsCommand => Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)];

        partial void OnIsDarkModeEnabledChanged(bool value) =>
            ChangeUserAppTheme(value);

        public SettingsViewModel(ThemeHandler themeHandler,
            IStringLocalizer<AppStringsRes> localizer)
        {
            _themeHandler = themeHandler;
            _localizer = localizer;

            Commands[nameof(SaveSelectedDistanceTypeToSettingsCommand)] = new Command(OnSaveSelectedDistanceTypeToSettings);

            DistanceUnitOfMeasures = new ObservableCollection<DistanceUnitOfMeasure>
        {
                new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel")),
                new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel")),
                new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel")),
                new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel")),
                new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel"))
        };
            DistanceType distanceType = Settings.DistanceType;
            switch (distanceType)
            {
                case DistanceType.Meters:
                    SelectedDistanceUnitOfMeasure = new DistanceUnitOfMeasure(DistanceType.Meters, _localizer.GetString("MetersLabel"));
                    break;
                case DistanceType.Kilometers:
                    SelectedDistanceUnitOfMeasure = new DistanceUnitOfMeasure(DistanceType.Kilometers, _localizer.GetString("KmLabel"));
                    break;
                case DistanceType.Miles:
                    SelectedDistanceUnitOfMeasure = new DistanceUnitOfMeasure(DistanceType.Miles, _localizer.GetString("MilesLabel"));
                    break;
                case DistanceType.Feets:
                    SelectedDistanceUnitOfMeasure = new DistanceUnitOfMeasure(DistanceType.Feets, _localizer.GetString("FeetsLabel"));
                    break;
                case DistanceType.NauticalMiles:
                    SelectedDistanceUnitOfMeasure = new DistanceUnitOfMeasure(DistanceType.NauticalMiles, _localizer.GetString("NauticalMilesLabel"));
                    break;
            }
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
