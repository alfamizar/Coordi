using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Presentation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class AddLocationViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string locationName;

        [ObservableProperty]
        private string country;

        [ObservableProperty]
        private string city;

        [ObservableProperty]
        private string latitude;

        [ObservableProperty]
        private string longitude;

        [ObservableProperty]
        private TimeZoneOffset selectedTimeZoneOffset;

        [ObservableProperty]
        private ObservableCollection<TimeZoneOffset> timeZoneOffsets;

        public ICommand SaveLocationCommand => Commands[nameof(SaveLocationCommand)];

        public AddLocationViewModel()
        {
            Commands[nameof(SaveLocationCommand)] = new Command(OnSaveLocation, CanSaveLocation);
            timeZoneOffsets = TimeZoneOffset.GetUtcOffsets();
            selectedTimeZoneOffset = new TimeZoneOffset("UTC±0 (UTC)", 0);
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocationName) ||
                e.PropertyName == nameof(Latitude) ||
                e.PropertyName == nameof(Longitude))
            {
                (Commands[nameof(SaveLocationCommand)] as Command).ChangeCanExecute();
            }
        }

        private bool CanSaveLocation()
        {
            return !string.IsNullOrWhiteSpace(LocationName)
                   && IsValidLatitude(Latitude)
                   && IsValidLongitude(Longitude);
        }

        private static bool IsValidLatitude(string latitude)
        {
            if (double.TryParse(latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latValue))
            {
                return latValue >= -90 && latValue <= 90;
            }
            return false;
        }

        private static bool IsValidLongitude(string longitude)
        {
            if (double.TryParse(longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double lonValue))
            {
                return lonValue >= -180 && lonValue <= 180;
            }
            return false;
        }

        private async void OnSaveLocation()
        {
            City city = new()
            {
                CountryName = Country,
                CityName = City
            };

            Location location = new()
            {
                City = city,
                Latitude = double.Parse(Latitude, NumberStyles.Any, CultureInfo.InvariantCulture),
                Longitude = double.Parse(Longitude, NumberStyles.Any, CultureInfo.InvariantCulture),
                Name = LocationName,
                TimeZoneOffset = SelectedTimeZoneOffset.Hours
            };

            await SaveLocationIfNotExists(location);

            await GoBack();
        }

        private async Task SaveLocationIfNotExists(Location location)
        {
            var savedLocations = await _locationManager.GetSavedLocations();
            if (location != null && !savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationManager.SaveLocation(location);
            }
        }

        private static async Task GoBack()
        {
            // just to simulate app processing something^^
            await Task.Delay(500);
            await Shell.Current.GoToAsync("..");
        }
    }
}
