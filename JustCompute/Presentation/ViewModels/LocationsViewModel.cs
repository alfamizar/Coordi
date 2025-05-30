using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Compute.Core.Common.Device;
using Compute.Core.Common.Messaging;
using Compute.Core.Helpers;
using Compute.Core.UI;
using CoordinateSharp;
using DotNext;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using JustCompute.Presentation.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.Collections.Specialized;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class LocationsViewModel : BaseViewModel, IRecipient<LocationMessage>
    {
        private readonly IDevicePermissionsService<PermissionStatus> _devicePermissionsService;
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;

        private Timer? _timer;

        [ObservableProperty]
        private int locationsCount;

        [ObservableProperty]
        private RangeEnabledObservableCollection<Location> locations = [];

        [ObservableProperty]
        private Coordinate? coordinate;

        [ObservableProperty]
        private DateTime currentTime = DateTime.Now;

        public LocationsViewModel(
            IStringLocalizer<AppStringsRes> localizer,
            IDevicePermissionsService<PermissionStatus> devicePermissionsService,
            IMessagingService messagerService,
            IToastService toastService
            )
        {
            _localizer = localizer;
            _devicePermissionsService = devicePermissionsService;
            _toastService = toastService;

            InitializeCommands();

            Locations.CollectionChanged += Locations_CollectionChanged;
            messagerService.Subscribe<IRecipient<LocationMessage>, LocationMessage>(this);
        }

        private void InitializeCommands()
        {
            Commands.Add("GoToAddLocationCommand", new Command(OnGoToAddLocation));
            Commands.Add("GoToSearchByCityCommand", new Command(OnGoSearchByCityLocation));
            Commands.Add("GoToSavedLocationsCommand", new Command(OnGoToSavedLocations));
            Commands.Add("SetSelectedLocationCommand", new Command<Location>(SetSelectedLocation));
            Commands.Add("RefreshCommand", new Command(InitViewModel));
        }

        private async void OnDeviceLocationChangedCallback(object? sender, GeolocationLocationChangedEventArgs e)
        {
            var deviceLocation =
                await _locationService.GetLocationFromCoordinates(e.Location.Latitude, e.Location.Longitude);

            if (deviceLocation != null)
            {
                _gpsLocationService.DeviceLocation = deviceLocation;

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

        private async void OnListeningDeviceLocationFailedCallback(object? sender, GeolocationListeningFailedEventArgs e)
        {
            await _toastService.ShowToast(_localizer.GetString("ListeningLocationFailedToastMessage"));
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

        private void Locations_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LocationsCount = Locations.Count;
        }

        private async Task InitDeviceLocation()
        {
            if (IsBusy) return;

            if (!await HandlePermissions()) return;

            IsBusy = true;

            await StopListeningLocation();
            await StartListeningLocation();

            IsBusy = false;

            if (_gpsLocationService.DeviceLocation != null) return;

            IsBusy = true;

            var locationResult = await _gpsLocationService.GetDeviceGeoLocation();

            IsBusy = false;

            if (!locationResult.IsSuccessful)
            {
                // todo HandleGetCurrentLocationError(locationResult.Error);
            }
        }

        private async Task UpdateSavedLocations()
        {
            // Retrieve saved locations from the location manager
            var savedLocations = await _locationService.GetSavedLocations();

            lock (Locations)
            {
                // Filter out locations that are already in the Locations list
                var newLocations = savedLocations
                    .Where(newLoc => !Locations.Any(existingLoc => existingLoc.Id == newLoc.Id))
                    .ToList();

                // Insert the new locations that are not already present
                Locations.InsertRange(newLocations);
            }
        }

        protected async Task<bool> HandlePermissions()
        {
            PermissionStatus permissionStatus = await _devicePermissionsService
                .CheckPermissionAndRequestIfNeeded(Permission.DeviceLocation);
            if (permissionStatus == PermissionStatus.Denied)
            {
                var result = await _dialogService.DisplayAlert(
                    _localizer.GetString("PermissionRequiredDialogTitle"),
                    _localizer.GetString("PermissionRequiredDialogMessage"),
                    _localizer.GetString("Close"),
                    _localizer.GetString("GoToSettings")
                    );

                if (result == _localizer.GetString("Close"))
                {
                    Application.Current?.Quit();
                }
                else if (result == _localizer.GetString("GoToSettings"))
                {
                    AppInfo.Current.ShowSettingsUI();
                }

                return false;
            }
            return true;
        }

        public override void OnAppWindowResumed()
        {
            // to cover case when returned from settings after changing permission status
            OnNavigatedTo();
        }

        public override void OnNavigatedTo()
        {
            InitViewModel();
        }

        public override async void OnPageDisappearing()
        {
            await StopListeningLocation();
        }

        private void SetSelectedLocation(Location? location)
        {
            if (location == null) return;

            _gpsLocationService.SelectedLocation = location;
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
            Dictionary<LocationInputContext, Location?> locationAndContext = [];
            locationAndContext[LocationInputContext.Add] = null;
            await _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        private async void OnGoSearchByCityLocation()
        {
            var searchLocationContext = SearchLocationContext.GoAhead;
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(searchLocationContext);
        }

        private async void OnGoToSavedLocations()
        {
            await _navigationService.NavigateToAsync<SavedLocationsViewModel>();
        }

        private async void InitViewModel()
        {
            await InitDeviceLocation();
            await UpdateSavedLocations();
            UpdateAtThisLocationInfo(_gpsLocationService.SelectedLocation);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.QuitApp();
            return true;
        }

        private async Task<Result<bool>> StartListeningLocation()
        {
            var result = 
                await _gpsLocationService.StartListeningForDeviceGeoLocation<GeolocationLocationChangedEventArgs, GeolocationListeningFailedEventArgs>(
                    OnDeviceLocationChangedCallback,
                    OnListeningDeviceLocationFailedCallback);
            if (!result.IsSuccessful)
            {
                await _toastService.ShowToast(_localizer.GetString("CannotStartListeningLocationToastMessage"));
            }
            return result;
        }

        private async Task<Result<bool>> StopListeningLocation()
        {
            var result = await Task.FromResult(_gpsLocationService
                .StoptListeningForDeviceLocation<GeolocationLocationChangedEventArgs, GeolocationListeningFailedEventArgs>(
                    OnDeviceLocationChangedCallback,
                    OnListeningDeviceLocationFailedCallback));
            if (!result.IsSuccessful)
            {
                await _toastService.ShowToast(_localizer.GetString("CannotStopListeningLocationToastMessage"));
            }
            return result;
        }

        void IRecipient<LocationMessage>.Receive(LocationMessage message)
        {
            switch (message.LocationInputContext)
            {
                case LocationInputContext.Edit:
                    {
                        var locationToUpdate = Locations.First(location => location.Id == message.Location.Id);
                        var locationToUpdateIndex = Locations.IndexOf(locationToUpdate);
                        Locations[locationToUpdateIndex] = message.Location;
                        break;
                    }
                case LocationInputContext.Delete:
                    {
                        var locationToDelete = Locations.First(location => location.Id == message.Location.Id);
                        var locationToDeleteIndex = Locations.IndexOf(locationToDelete);
                        Locations.RemoveAt(locationToDeleteIndex);
                        if (Locations.Count == 0)
                        {
                            _gpsLocationService.SelectedLocation = null;
                        }
                        break;
                    }
            }
        }
    }
}