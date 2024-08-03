using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Common.Messaging;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Navigation;
using Compute.Core.UI;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using JustCompute.Presentation.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class InputLocationViewModel : BaseViewModel, IQueryParameter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly IMessagingService _messagingService;
        private LocationInputContext? _locationInputContext;

        [ObservableProperty]
        private TimeZoneOffset selectedTimeZoneOffset;

        [ObservableProperty]
        private ObservableCollection<TimeZoneOffset> timeZoneOffsets;

        [ObservableProperty]
        private Location location = new();

        public ICommand SaveLocationCommand => Commands[nameof(SaveLocationCommand)];
        public ICommand PrefillCoordinatesCommand => Commands[nameof(GoBackCommand)];
        public ICommand GoBackCommand => Commands[nameof(GoBackCommand)];

        public InputLocationViewModel(
            IToastService toastService,
            IMessagingService messagingService,
            IStringLocalizer<AppStringsRes> localizer
            )
        {
            _toastService = toastService;
            _messagingService = messagingService;
            _localizer = localizer;

            Commands[nameof(SaveLocationCommand)] = new Command(OnSaveLocation, CanSaveLocation);
            Commands[nameof(PrefillCoordinatesCommand)] = new Command(OnPrefillCoordinates);
            Commands[nameof(GoBackCommand)] = new Command(() => OnBackButtonPressed());

            timeZoneOffsets = TimeZoneOffset.GetUtcOffsets();
            selectedTimeZoneOffset = TimeZoneOffset.DefaultTimeZoneOffset;

            PropertyChanged += OnPropertyChanged;
            Location.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location) ||
                e.PropertyName == nameof(Location.Name) ||
                e.PropertyName == nameof(Location.Latitude) ||
                e.PropertyName == nameof(Location.Longitude))
            {
                (Commands[nameof(SaveLocationCommand)] as Command)?.ChangeCanExecute();
            }
        }

        private bool CanSaveLocation()
        {
            return !string.IsNullOrWhiteSpace(Location?.Name)
                   && IsValidLatitude(Location.LatitudeDouble)
                   && IsValidLongitude(Location.LongitudeDouble);
        }

        private static bool IsValidLatitude(double latitude)
        {
            return latitude >= -90 && latitude <= 90;
        }

        private static bool IsValidLongitude(double longitude)
        {
            return longitude >= -180 && longitude <= 180;
        }

        private async void OnSaveLocation()
        {
            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");

            switch (_locationInputContext.Value)
            {
                case LocationInputContext.Add:
                    {
                        if (Location != null)
                            await SaveLocationIfNotExists(Location);
                        break;
                    }
                case LocationInputContext.Edit:
                    {
                        if (Location != null)
                            await UpdateLocation(Location);
                        break;
                    }
            }
        }

        private async Task SaveLocationIfNotExists(Location location)
        {
            var savedLocations = await _locationService.GetSavedLocations();
            if (!savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationService.SaveLocation(location);
                OnBackButtonPressed();
            }
            else
            {
                await _toastService.ShowToast(_localizer.GetString("DuplicatedLocationToastMessge"));
            }
        }

        private async Task UpdateLocation(Location location)
        {
            await _locationService.UpdateLocation(location);

            _messagingService.Send(new LocationMessage(location, LocationInputContext.Edit));

            OnBackButtonPressed();
        }

        public async void OnPrefillCoordinates()
        {
            if (Location == null || IsBusy) return;

            if (_gpsLocationService.DeviceLocation is null)
            {
                IsBusy = true;
                await _gpsLocationService.GetDeviceGeoLocation();
                IsBusy = false;
            }

            Location.Latitude = _gpsLocationService.DeviceLocation?.LatitudeDouble.ToString() ?? Location.Latitude;
            Location.Longitude = _gpsLocationService.DeviceLocation?.LongitudeDouble.ToString() ?? Location.Latitude;
        }

        public void ApplyQueryParameter(object? parameter)
        {
            if (parameter is Dictionary<LocationInputContext, Location> locationAndContext)
            {
                if (Location != null)
                {
                    Location.PropertyChanged -= OnPropertyChanged;
                }

                var kvp = locationAndContext.FirstOrDefault();
                _locationInputContext = kvp.Key;

                if (kvp.Value != null)
                {
                    Location = kvp.Value;
                }

                if (Location != null)
                {
                    Location.PropertyChanged += OnPropertyChanged;
                }
            }

            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");

            if (_locationInputContext.Value == LocationInputContext.Add && Location != null)
            {
                Location.Name = string.Empty;
            }
        }

        public override bool OnBackButtonPressed()
        {
            _locationInputContext = null;
            _navigationService.NavigateBackAsync();
            return true;
        }
    }
}
