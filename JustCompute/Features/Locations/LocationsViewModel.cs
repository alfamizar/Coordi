using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Compute.Core.Common.Device;
using Compute.Core.Common.Messaging;
using Compute.Core.Domain.Entities.Models.Weather;
using Compute.Core.Domain.Services;
using Compute.Core.Domain.Services.Weather;
using Compute.Core.Helpers;
using Compute.Core.UI;
using CoordinateSharp;
using DotNext;
using JustCompute.Features.InputLocation;
using JustCompute.Features.SavedLocations;
using JustCompute.Features.SearchByCity;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Specialized;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Locations
{
    public partial class LocationsViewModel : BaseViewModel, IRecipient<LocationMessage>
    {
        private enum LocationAction
        {
            AddCustomLocation,
            SearchCity,
            ManageSavedLocations
        }

        private const int ForecastDays = 7;

        private readonly IDevicePermissionsService<PermissionStatus> _devicePermissionsService;
        private readonly IPermissionGateService _permissionGate;
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly IWeatherService _weatherService;

        private CancellationTokenSource? _timerCancellationTokenSource;
        private CancellationTokenSource? _weatherCancellationTokenSource;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _permissionDialogOpen;

        // Suppresses the SelectedLocation write-back while we restore the selection ourselves.
        // Mutating Locations makes the CarouselView snap CurrentItem to index 0, which would
        // otherwise echo through the two-way binding and clobber the user's selection.
        private bool _suppressSelectionWriteBack;

        [ObservableProperty]
        private int locationsCount;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private RangeEnabledObservableCollection<Location> locations = [];

        [ObservableProperty]
        private Location? selectedLocation;

        [ObservableProperty]
        private Coordinate? coordinate;

        [ObservableProperty]
        private DateTime currentTime = DateTime.Now;

        [ObservableProperty]
        private WeatherForecast? weatherForecast;

        public LocationsViewModel(
            ViewModelServices services,
            IStringLocalizer<AppStringsRes> localizer,
            IDevicePermissionsService<PermissionStatus> devicePermissionsService,
            IPermissionGateService permissionGate,
            IMessagingService messagerService,
            IToastService toastService,
            IWeatherService weatherService
            )
            : base(services)
        {
            _localizer = localizer;
            _devicePermissionsService = devicePermissionsService;
            _permissionGate = permissionGate;
            _toastService = toastService;
            _weatherService = weatherService;

            InitializeCommands();

            Locations.CollectionChanged += Locations_CollectionChanged;
            messagerService.Subscribe<IRecipient<LocationMessage>, LocationMessage>(this);
            _permissionGate.LocationPermissionStateChanged += OnLocationPermissionStateChanged;
        }

        private void OnLocationPermissionStateChanged(object? sender, bool isGranted)
        {
            if (isGranted)
            {
                _ = InitViewModelAsync(forceRefreshDeviceLocation: true);
            }
        }

        private void InitializeCommands()
        {
            Commands.Add("ShowLocationActionsCommand", new AsyncRelayCommand(OnShowLocationActions));
            Commands.Add("GoToAddLocationCommand", new AsyncRelayCommand(OnGoToAddLocation));
            Commands.Add("GoToSearchByCityCommand", new AsyncRelayCommand(OnGoSearchByCityLocation));
            Commands.Add("GoToSavedLocationsCommand", new AsyncRelayCommand(OnGoToSavedLocations));
            Commands.Add("RefreshCommand", new AsyncRelayCommand(OnRefresh));
        }

        private async Task OnShowLocationActions()
        {
            var addCustomLocation = _localizer.GetString("AddCustomLocationLabel");
            var searchCity = _localizer.GetString("SearchCityLabel");
            var manageSavedLocations = _localizer.GetString("ManageSavedLocationsLabel");
            var close = _localizer.GetString("Close");

            var selectedAction = await _dialogService.DisplayActionSheet(
                _localizer.GetString("AddLocationLabel"),
                close,
                string.Empty,
                new DialogAction<LocationAction>(LocationAction.AddCustomLocation, addCustomLocation),
                new DialogAction<LocationAction>(LocationAction.SearchCity, searchCity),
                new DialogAction<LocationAction>(LocationAction.ManageSavedLocations, manageSavedLocations));

            if (selectedAction == LocationAction.AddCustomLocation)
            {
                await OnGoToAddLocation();
            }
            else if (selectedAction == LocationAction.SearchCity)
            {
                await OnGoSearchByCityLocation();
            }
            else if (selectedAction == LocationAction.ManageSavedLocations)
            {
                await OnGoToSavedLocations();
            }
        }

        private void StartTimer(int offsetHours = 0)
        {
            StopTimer();

            var cancellationTokenSource = new CancellationTokenSource();
            _timerCancellationTokenSource = cancellationTokenSource;
            Application.Current?.Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    return false;
                }

                CurrentTime = DateTime.UtcNow.AddHours(offsetHours);
                return true;
            });
        }

        private void StopTimer()
        {
            _timerCancellationTokenSource?.Cancel();
            _timerCancellationTokenSource?.Dispose();
            _timerCancellationTokenSource = null;
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

        private async Task<bool> InitDeviceLocation(bool forceRefresh = false)
        {
            if (IsBusy) return false;

            if (!await HandlePermissions()) return false;

            if (!forceRefresh && _gpsLocationService.DeviceLocation != null) return true;

            IsBusy = true;

            var locationResult = await _gpsLocationService.GetDeviceGeoLocation();

            IsBusy = false;

            if (!locationResult.IsSuccessful)
            {
                return _gpsLocationService.DeviceLocation != null;
            }

            return true;
        }

        private async Task UpdateSavedLocations()
        {
            var savedLocations = await _locationService.GetSavedLocations();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var newLocations = savedLocations
                    .Where(newLoc => !Locations.Any(existingLoc => AreSameLocation(existingLoc, newLoc)))
                    .ToList();

                if (newLocations.Count > 0)
                    Locations.InsertRange(newLocations);
            });
        }

        private void EnsureKnownLocationsVisible()
        {
            if (_gpsLocationService.DeviceLocation is not null)
            {
                MarkAsCurrentDeviceLocation(_gpsLocationService.DeviceLocation);
            }

            if (_gpsLocationService.SelectedLocation is not null)
            {
                UpsertLocation(_gpsLocationService.SelectedLocation, insertAtStart: Locations.Count == 0);
            }
        }

        private void MarkAsCurrentDeviceLocation(Location deviceLocation)
        {
            var currentLocationSlot = Locations.FirstOrDefault(IsCurrentDeviceSlot);

            if (currentLocationSlot is not null && !ReferenceEquals(currentLocationSlot, deviceLocation))
            {
                RefreshCoordinatesInPlace(currentLocationSlot, deviceLocation);
                _gpsLocationService.DeviceLocation = currentLocationSlot;
                return;
            }

            deviceLocation.IsCurrent = true;
            UpsertLocation(deviceLocation, insertAtStart: true);
        }

        private static bool IsCurrentDeviceSlot(Location location) => location.IsCurrent && location.Id <= 0;

        private static void RefreshCoordinatesInPlace(Location target, Location source)
        {
            target.Name = source.Name;
            target.Latitude = source.Latitude;
            target.Longitude = source.Longitude;
            target.City = source.City;
            target.IsCurrent = true;
        }

        private void UpsertLocation(Location location, bool insertAtStart = false)
        {
            var existingLocation = Locations.FirstOrDefault(existing => AreSameLocation(existing, location));
            if (existingLocation is not null)
            {
                // Workaround: re-assigning the same instance raises a Replace event that snaps the
                // CarouselView's CurrentItem back to index 0, discarding the user's selection.
                if (ReferenceEquals(existingLocation, location)) return;

                var index = Locations.IndexOf(existingLocation);
                Locations[index] = location;
                return;
            }

            if (insertAtStart)
            {
                Locations.Insert(0, location);
            }
            else
            {
                Locations.Add(location);
            }
        }

        private static bool AreSameLocation(Location left, Location right)
        {
            if (left.Id > 0 && right.Id > 0)
            {
                return left.Id == right.Id;
            }

            if (left.IsCurrent && right.IsCurrent)
            {
                return true;
            }

            return Math.Abs(left.LatitudeDouble - right.LatitudeDouble) < 0.000001
                && Math.Abs(left.LongitudeDouble - right.LongitudeDouble) < 0.000001
                && string.Equals(left.Name, right.Name, StringComparison.Ordinal);
        }

        protected async Task<bool> HandlePermissions()
        {
            PermissionStatus permissionStatus = await _devicePermissionsService
                .CheckPermissionAndRequestIfNeeded(Permission.DeviceLocation);
            if (permissionStatus == PermissionStatus.Denied)
            {
                if (_permissionDialogOpen)
                {
                    return false;
                }

                try
                {
                    _permissionDialogOpen = true;

                    var result = await _dialogService.DisplayAlert(
                        _localizer.GetString("PermissionRequiredDialogTitle"),
                        _localizer.GetString("PermissionRequiredDialogMessage"),
                        _localizer.GetString("Close"),
                        _localizer.GetString("GoToSettings")
                        );

                    if (result == DialogButton.Positive)
                    {
                        Application.Current?.Quit();
                    }
                    else if (result == DialogButton.Negative)
                    {
                        AppInfo.Current.ShowSettingsUI();
                    }
                }
                finally
                {
                    _permissionDialogOpen = false;
                }

                return false;
            }
            return true;
        }

        public override void OnAppWindowResumed()
        {
            _ = InitViewModelAsync();
        }

        public override Task OnNavigatedToAsync()
        {
            _ = InitViewModelAsync();
            return Task.CompletedTask;
        }

        public override Task OnPageDisappearingAsync()
        {
            StopTimer();
            return Task.CompletedTask;
        }

        partial void OnSelectedLocationChanged(Location? value)
        {
            if (_suppressSelectionWriteBack) return;
            if (ReferenceEquals(value, _gpsLocationService.SelectedLocation)) return;

            _gpsLocationService.SelectedLocation = value;
            UpdateAtThisLocationInfo(value);
        }

        private void UpdateAtThisLocationInfo(Location? location)
        {
            if (location is null)
            {
                Coordinate = null;
                WeatherForecast = null;
                StopTimer();
                CancelWeatherLoad();
                return;
            }

            Coordinate = new Coordinate(location.LatitudeDouble, location.LongitudeDouble, DateTime.Now)
            {
                Offset = location.TimeZoneOffset.Hours
            };

            RestartTimer(location.TimeZoneOffset.Hours);
            _ = LoadWeatherForecastAsync(location.LatitudeDouble, location.LongitudeDouble);
        }

        private async Task LoadWeatherForecastAsync(double latitude, double longitude)
        {
            CancelWeatherLoad();
            var cts = new CancellationTokenSource();
            _weatherCancellationTokenSource = cts;

            WeatherForecast = null;

            try
            {
                var forecast = await _weatherService
                    .GetDailyForecastAsync(latitude, longitude, ForecastDays, cts.Token)
                    .ConfigureAwait(false);

                if (cts.IsCancellationRequested) return;

                await MainThread.InvokeOnMainThreadAsync(() => WeatherForecast = forecast);
            }
            catch (OperationCanceledException) { }
        }

        private void CancelWeatherLoad()
        {
            _weatherCancellationTokenSource?.Cancel();
            _weatherCancellationTokenSource?.Dispose();
            _weatherCancellationTokenSource = null;
        }

        private async Task OnGoToAddLocation()
        {
            Dictionary<LocationInputContext, Location?> locationAndContext = [];
            locationAndContext[LocationInputContext.Add] = null;
            await _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        private async Task OnGoSearchByCityLocation()
        {
            var searchLocationContext = SearchLocationContext.GoAhead;
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(searchLocationContext);
        }

        private async Task OnGoToSavedLocations()
        {
            await _navigationService.NavigateToAsync<SavedLocationsViewModel>();
        }

        private async Task OnRefresh()
        {
            try
            {
                await InitViewModelAsync(forceRefreshDeviceLocation: true);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task InitViewModelAsync(bool forceRefreshDeviceLocation = false)
        {
            if (!await _initializationSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                if (!await InitDeviceLocation(forceRefreshDeviceLocation))
                {
                    return;
                }

                await UpdateSavedLocations();

                await _gpsLocationService.RestorePersistedSelectedLocation();
                _gpsLocationService.SelectedLocation ??= _gpsLocationService.DeviceLocation ?? Locations.FirstOrDefault();

                var selected = _gpsLocationService.SelectedLocation;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _suppressSelectionWriteBack = true;
                    EnsureKnownLocationsVisible();
                    SelectedLocation = selected;

                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher is not null)
                    {
                        dispatcher.Dispatch(() => _suppressSelectionWriteBack = false);
                    }
                    else
                    {
                        _suppressSelectionWriteBack = false;
                    }
                });

                UpdateAtThisLocationInfo(selected);
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.QuitApp();
            return true;
        }

        void IRecipient<LocationMessage>.Receive(LocationMessage message)
        {
            switch (message.LocationInputContext)
            {
                case LocationInputContext.Edit:
                    {
                        var locationToUpdate = Locations.FirstOrDefault(location => location.Id == message.Location.Id);
                        if (locationToUpdate == null) break;
                        var locationToUpdateIndex = Locations.IndexOf(locationToUpdate);
                        Locations[locationToUpdateIndex] = message.Location;
                        break;
                    }
                case LocationInputContext.Delete:
                    {
                        var locationToDelete = Locations.FirstOrDefault(location => location.Id == message.Location.Id);
                        if (locationToDelete == null) break;
                        var locationToDeleteIndex = Locations.IndexOf(locationToDelete);
                        Locations.RemoveAt(locationToDeleteIndex);

                        if (_gpsLocationService.SelectedLocation?.Id == message.Location.Id)
                        {
                            SelectedLocation = Locations.FirstOrDefault();
                        }
                        break;
                    }
            }
        }

        private void Dispose()
        {
            StopTimer();
            CancelWeatherLoad();
            Locations.CollectionChanged -= Locations_CollectionChanged;
        }
    }
}
