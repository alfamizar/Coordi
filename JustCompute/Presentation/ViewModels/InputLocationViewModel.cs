using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Navigation;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using JustCompute.Presentation.ViewModels.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class InputLocationViewModel : BaseViewModel, IQueryParameter
    {
        private LocationInputContext? _locationInputContext;

        [ObservableProperty]
        private TimeZoneOffset selectedTimeZoneOffset;

        [ObservableProperty]
        private ObservableCollection<TimeZoneOffset> timeZoneOffsets;

        [ObservableProperty]
        private Location? location;

        public ICommand SaveLocationCommand => Commands[nameof(SaveLocationCommand)];
        public ICommand PrefillCoordinatesCommand => Commands[nameof(GoBackCommand)];
        public ICommand GoBackCommand => Commands[nameof(GoBackCommand)];

        public InputLocationViewModel()
        {
            Commands[nameof(SaveLocationCommand)] = new Command(OnSaveButtonClicked, CanSaveLocation);
            Commands[nameof(PrefillCoordinatesCommand)] = new Command(OnPrefillCoordinates);
            Commands[nameof(GoBackCommand)] = new Command(() => OnBackButtonPressed());
            timeZoneOffsets = TimeZoneOffset.GetUtcOffsets();
            selectedTimeZoneOffset = TimeZoneOffset.DefaultTimeZoneOffset;
            PropertyChanged += OnPropertyChanged;
            Location = new();
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

        private async void OnSaveButtonClicked()
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
            await GoBack();
        }

        private async Task SaveLocationIfNotExists(Location location)
        {
            var savedLocations = await _locationManager.GetSavedLocations();
            if (!savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationManager.SaveLocation(location);
            }
        }

        private async Task UpdateLocation(Location location)
        {
            await _locationManager.UpdateLocation(location);

            WeakReferenceMessenger.Default.Send(new LocationMessage(location, LocationInputContext.Edit));
        }

        private async Task GoBack()
        {
            // just to simulate app processing something^^
            await Task.Delay(500);
            _locationInputContext = null;
            await _navigationService.NavigateBackAsync();
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.NavigateBackAsync();
            return true;
        }

        public async void OnPrefillCoordinates()
        {
            if (Location == null || IsBusy) return;

            if (_locationManager.DeviceLocation is null)
            {
                IsBusy = true;

                await _locationManager.GetDeviceLocation();

                IsBusy = false;
            }

            Location.Latitude = _locationManager.DeviceLocation?.LatitudeDouble.ToString() ?? string.Empty;
            Location.Longitude = _locationManager.DeviceLocation?.LongitudeDouble.ToString() ?? string.Empty;
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
                Location = kvp.Value ?? Location;


                if (Location != null)
                {
                    Location.PropertyChanged += OnPropertyChanged;
                }
            }

            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");
        }

        protected override Task LoadItems()
        {
            return Task.CompletedTask;
        }
    }
}
