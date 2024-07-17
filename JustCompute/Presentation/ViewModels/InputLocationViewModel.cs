using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Navigation;
using JustCompute.Presentation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class InputLocationViewModel : BaseViewModel, IQueryParameter
    {
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
            Commands[nameof(SaveLocationCommand)] = new Command(OnSaveLocation, CanSaveLocation);
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

        private async void OnSaveLocation()
        {
            if (Location != null)
                await SaveLocationIfNotExists(Location);

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

        private async Task GoBack()
        {
            // just to simulate app processing something^^
            await Task.Delay(500);
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
            if (parameter is Location location)
            {
                if (Location != null)
                    Location.PropertyChanged -= OnPropertyChanged;
                Location = location;
                Location.PropertyChanged += OnPropertyChanged;
            }
        }

        protected override Task LoadItems()
        {
            return Task.CompletedTask;
        }
    }
}
