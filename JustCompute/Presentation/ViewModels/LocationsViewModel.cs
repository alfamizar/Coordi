using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Common.Device;
using Compute.Core.Helpers;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class LocationsViewModel : BaseViewModel
    {
        private readonly IDevicePermissionsService<PermissionStatus> _devicePermissionsService;
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private Timer? _timer;

        [ObservableProperty]
        private int locationsCount;

        [ObservableProperty]
        private RangeEnabledObservableCollection<Location> locations = [];

        [ObservableProperty]
        private Location? carouselSelectedItem;

        [ObservableProperty]
        private Coordinate? coordinate;

        [ObservableProperty]
        private DateTime currentTime = DateTime.Now;

        public LocationsViewModel(
            IStringLocalizer<AppStringsRes> localizer,
            IDevicePermissionsService<PermissionStatus> devicePermissionsService
            )
        {
            _localizer = localizer;
            _devicePermissionsService = devicePermissionsService;

            InitializeCommands();

            Locations.CollectionChanged += LocationsCollectionChanged;
            _locationManager.DeviceLocationChanged += DeviceLocationChanged;          
        }

        private void InitializeCommands()
        {
            Commands.Add("GoToAddLocationCommand", new Command(OnGoToAddLocation));
            Commands.Add("GoToSearchByCityCommand", new Command(OnGoSearchByCityLocation));
            Commands.Add("GoToSavedLocationsCommand", new Command(OnGoToSavedLocations));
            Commands.Add("SetSelectedLocationCommand", new Command<Location>(SetSelectedLocation));
            Commands.Add("DeleteLocationCommand", new Command<Location>(async (location) => await OnDeleteLocation(location)));
            Commands.Add("RefreshCommand", new Command(InitViewModel));
        }

        private void DeviceLocationChanged(object? sender, EventArgs e)
        {
            var deviceLocation = _locationManager.DeviceLocation;
            if (deviceLocation != null)
            {
                if (Locations.Count > 0)
                {
                    Locations[0] = deviceLocation;
                }
                else
                {
                    Locations.Add(deviceLocation);
                }
            }
        }

        private void StartTimer(int offsetHours = 0)
        {
            _timer?.Dispose();

            _timer = new Timer(
                (_) => { CurrentTime = DateTime.UtcNow.AddHours(offsetHours); },
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1)
            );
        }

        private void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void RestartTimer(int offsetHours = 0)
        {
            StopTimer();
            StartTimer(offsetHours);
        }

        private void LocationsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LocationsCount = Locations.Count;
        }

        private async Task InitDeviceLocation()
        {
            if (IsBusy) return;

            var permissionsAllowed = await HandlePermissions();
            if (!permissionsAllowed || _locationManager.DeviceLocation != null)
            {
                return;
            }

            IsBusy = true;

            var locationResult = await _locationManager.GetDeviceLocation();
            _locationManager.OnStartListeningDeciveLocation();

            IsBusy = false;

            if (!locationResult.IsSuccessful)
            {
                // todo HandleGetCurrentLocationError(locationResult.Error);
            }
        }

        private async Task GetSavedLocations()
        {
            // Retrieve saved locations from the location manager
            var savedLocations = await _locationManager.GetSavedLocations();

            // Filter out locations that are already in the Locations list
            var newLocations = savedLocations
                .Where(newLoc => !Locations.Any(existingLoc => existingLoc.Name == newLoc.Name))
                .ToList();

            // Insert the new locations that are not already present
            Locations.InsertRange(newLocations);
        }

        protected async Task<bool> HandlePermissions()
        {
            PermissionStatus permissionStatus = await _devicePermissionsService
                .CheckPermissionAndRequestIfNeeded(Permission.DeviceLocation);
            if (permissionStatus == PermissionStatus.Denied)
            {
                // Store the localized strings in variables
                var closeString = _localizer.GetString("Close");
                var goToSettingsString = _localizer.GetString("GoToSettings");

                var result = await _dialogService.DisplayAlert(
                    _localizer.GetString("PermissionRequiredDialogTitle"),
                    _localizer.GetString("PermissionRequiredDialogMessage"),
                    closeString,
                    goToSettingsString
                );

                switch (result)
                {
                    case var str when str == closeString:
                        Application.Current?.Quit();
                        break;
                    case var str when str == goToSettingsString:
                        AppInfo.Current.ShowSettingsUI();
                        break;
                }

                return false;
            }
            return true;
        }

        public override void OnAppWindowResumed()
        {
            // to cover case when returned from settings after changing permission status
            InitViewModel();
        }

        public override void OnPageAppearing()
        {
            InitViewModel();
        }

        public override void OnPageDisappearing()
        {
            base.OnPageDisappearing();
        }

        private async Task SaveLocationIfNotExists(Location location)
        {
            var savedLocations = await _locationManager.GetSavedLocations();

            if (!savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationManager.SaveLocation(location);
            }
        }

        private async Task OnDeleteLocation(Location location)
        {
            if (location == _locationManager.DeviceLocation)
            {
                string text = "You cannot delete current location!";
                ToastDuration duration = ToastDuration.Short;

                var toast = Toast.Make(text, duration);

                await toast.Show();

                return;
            }

            await _locationManager.DeleteLocation(location);

            Locations.Remove(location);
            if (LocationsCount == 0)
            {
                _locationManager.SelectedLocation = null;
                UpdateAtThisLocationInfo(null);
            }
        }

        private void SetSelectedLocation(Location? location)
        {
            if (location == null) return;

            _locationManager.SelectedLocation = location;
            UpdateAtThisLocationInfo(location);
        }

        private void UpdateAtThisLocationInfo(Location? location)
        {
            if (location is null)
            {
                Coordinate = null;
                StopTimer();
                return;
            }

            Coordinate = new Coordinate(location.LatitudeDouble, location.LongitudeDouble, DateTime.Now)
            {
                Offset = location.TimeZoneOffset.Hours
            };

            RestartTimer(location.TimeZoneOffset.Hours);
        }

        private async void OnGoToAddLocation()
        {
            await _navigationService.NavigateToAsync<InputLocationViewModel>();
        }

        private async void OnGoSearchByCityLocation()
        {
            await _navigationService.NavigateToAsync<SearchByCityViewModel>();
        }

        private async void OnGoToSavedLocations()
        {
            await _navigationService.NavigateToAsync<SavedLocationsViewModel>();
        }

        private async void InitViewModel()
        {
            await InitDeviceLocation();
            await GetSavedLocations();
            UpdateAtThisLocationInfo(_locationManager.SelectedLocation);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.QuitApp();
            return true;
        }
    }
}