using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Domain.Errors;
using Compute.Core.Helpers;
using Compute.Core.Services;
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
        private TimeModel _timeModel;

        [ObservableProperty]
        string performanceCounter;

        [ObservableProperty]
        string obtainCurrentLocationErrorMessage;

        [ObservableProperty]
        int locationsCount;

        [ObservableProperty]
        Location currentLocation;

        [ObservableProperty]
        RangeEnabledObservableCollection<Location> locations;

        [ObservableProperty]
        Coordinate coordinate;

        private DateTime _currentTime;

        public DateTime CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public LocationsViewModel(IStringLocalizer<AppStringsRes> localizer,
            IDevicePermissionsService<PermissionStatus> devicePermissionsService)
        {
            _localizer = localizer;
            _devicePermissionsService = devicePermissionsService;

            Commands.Add("GoToAddLocationCommand", new Command(OnGoToAddLocation));

            Commands.Add("UpdateCurrentLocationCommand",
                new Command<Location>(UpdateCurrentLocation));

            Commands.Add("DeleteLocationCommand",
                new Command<Location>(async (location) => await OnDeleteLocation(location)));

            Commands.Add("RefreshCommand", new Command(InitViewModel));

            Locations = new RangeEnabledObservableCollection<Location>();
            Locations.CollectionChanged += LocationsCollectionChanged;
        }

        private void StartTimer(int offsetHours = 0)
        {
            _timeModel = new TimeModel(offsetHours);
            Timer timer = new(UpdateTime, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void UpdateTime(object state)
        {
            _timeModel.UpdateTime();
            CurrentTime = _timeModel.CurrentTime;
        }

        private void LocationsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LocationsCount = Locations.Count;
            if (LocationsCount == 0)
            {
                UpdateCurrentLocation(null);
            }
        }

        private async Task InitCurrentLocation()
        {
            if (IsBusy) return;

            var permissionsAllowed = await HandlePermissions();
            if (!permissionsAllowed || CurrentLocation != null)
            {
                return;
            }

            IsBusy = true;

            var locationResult = await _locationManager.RequestCurrentLocation();

            if (locationResult.IsSuccessful)
            {
                CurrentLocation = locationResult.Value;
            }
            else
            {
                HandleGetCurrentLocationError(locationResult.Error);
            }

            IsBusy = false;
        }

        private void HandleGetCurrentLocationError(FaultCode faultCode)
        {
            // todo
        }

        private async Task GetSavedLocations()
        {
            var savedLocations = await _locationManager.GetSavedLocations();
            Locations.Clear();
            Locations.InsertRange(savedLocations);
        }

        protected async Task<bool> HandlePermissions()
        {
            PermissionStatus permissionStatus = await _devicePermissionsService
                .CheckPermissionAndRequestIfNeeded(Permission.CurrentLocation);
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
                        Application.Current.Quit();
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
            if (location == null) return;

            var savedLocations = await _locationManager.GetSavedLocations();
            if (!savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationManager.SaveLocation(location);
            }
        }

        private async Task OnDeleteLocation(Location location)
        {
            await _locationManager.DeleteLocation(location);
            Locations.Remove(location);
        }

        private void UpdateCurrentLocation(Location location)
        {
            CurrentLocation = location;
            _locationManager.CurrentLocation = location;
            UpdateAtThisCoordinate(location);
        }

        private void UpdateAtThisCoordinate(Location location)
        {
            if (location is not null)
            {
                Coordinate = new Coordinate(location.Latitude, location.Longitude, DateTime.Now);
                Coordinate.Offset += location.TimeZoneOffset;
                StartTimer(location.TimeZoneOffset);
            }
        }

        private async void OnGoToAddLocation()
        {
            await Shell.Current.GoToAsync("addlocation");
        }

        private async void InitViewModel()
        {
            await InitCurrentLocation();
            await SaveLocationIfNotExists(CurrentLocation);
            await GetSavedLocations();
        }
    }
}