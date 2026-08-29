using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Common.Messaging;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Navigation;
using Compute.Core.UI;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.InputLocation
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

        /// <summary>
        /// The same page serves both contexts, so the title has to say which one it is —
        /// it read "Add Location" even when editing an existing place.
        /// </summary>
        [ObservableProperty]
        private string pageTitle = string.Empty;


        public InputLocationViewModel(
            ViewModelServices services,
            IToastService toastService,
            IMessagingService messagingService,
            IStringLocalizer<AppStringsRes> localizer
            )
            : base(services)
        {
            _toastService = toastService;
            _messagingService = messagingService;
            _localizer = localizer;


            pageTitle = localizer.GetString("AddLocationLabel");
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
                SaveLocationCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanSaveLocation()
        {
            return !string.IsNullOrWhiteSpace(Location?.Name)
                   && IsValidLatitude(Location.Latitude)
                   && IsValidLongitude(Location.Longitude);
        }

        private static bool IsValidLatitude(double latitude)
        {
            return latitude >= -90 && latitude <= 90;
        }

        private static bool IsValidLongitude(double longitude)
        {
            return longitude >= -180 && longitude <= 180;
        }

        [RelayCommand]
        private void GoBack() => OnBackButtonPressed();

        [RelayCommand(CanExecute = nameof(CanSaveLocation))]
        private async Task SaveLocation()
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

                // Announce it like Edit and Delete already do. Without this the new place only
                // surfaces on the next full re-init of the Locations screen, which is exactly the
                // sort of thing that gets skipped while a device fix is still in flight.
                _messagingService.Send(new LocationMessage(location, LocationInputContext.Add));

                // The place is saved and now current, so going back one step to the city search
                // the user has finished with is the wrong destination — return to Locations,
                // where the result of what they just did is actually visible.
                _locationInputContext = null;
                await _navigationService.NavigateToShellRouteAsync("locations");
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

        [RelayCommand]
        private async Task PrefillCoordinates()
        {
            if (Location == null || IsBusy) return;

            if (_gpsLocationService.DeviceLocation is null)
            {
                IsBusy = true;
                await _gpsLocationService.GetDeviceGeoLocation();
                IsBusy = false;
            }

            Location.Latitude = _gpsLocationService.DeviceLocation?.Latitude ?? Location.Latitude;
            Location.Longitude = _gpsLocationService.DeviceLocation?.Longitude ?? Location.Longitude;
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
                    // Edit works on a copy: the screen binds directly to this object, so editing
                    // the caller's instance would push every keystroke into the lists that hold
                    // it and leave the changes there even when the user backs out without saving.
                    Location = _locationInputContext == LocationInputContext.Edit
                        ? kvp.Value.Clone()
                        : kvp.Value;
                }

                if (Location != null)
                {
                    Location.PropertyChanged += OnPropertyChanged;
                }
            }

            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");

            PageTitle = _locationInputContext.Value == LocationInputContext.Edit
                ? _localizer.GetString("EditLocationLabel")
                : _localizer.GetString("AddLocationLabel");

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
