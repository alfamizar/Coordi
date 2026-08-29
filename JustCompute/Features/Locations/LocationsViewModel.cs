using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Compute.Core.Common.Device;
using Compute.Core.Common.Messaging;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Weather;
using Compute.Core.Domain.Services;
using Compute.Core.Domain.Services.Weather;
using Compute.Core.Helpers;
using Compute.Core.UI;
using DotNext;
using JustCompute.Features.InputLocation;
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
        private bool _isFetchingDeviceLocation;
        private bool _initializationPending;

        // Suppresses the SelectedLocation write-back while we restore the selection ourselves.
        // Mutating Locations makes the CarouselView snap CurrentItem to index 0, which would
        // otherwise echo through the two-way binding and clobber the user's selection.
        private bool _suppressSelectionWriteBack;

        [ObservableProperty]
        private int locationsCount;

        /// <summary>
        /// True while the app is still on the placeholder location, i.e. the user has not chosen
        /// anywhere yet. Drives the onboarding card at the top of the screen.
        /// </summary>
        [ObservableProperty]
        private bool isPlaceholderLocation = !global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private RangeEnabledObservableCollection<Location> locations = [];

        [ObservableProperty]
        private Location? selectedLocation;

        [ObservableProperty]
        private CelestialSnapshot? coordinate;

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



        private void StartTimer(double offsetHours = 0)
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

        public void RestartTimer(double offsetHours = 0)
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
            if (_isFetchingDeviceLocation) return false;

            if (!await HandlePermissions()) return false;

            if (!forceRefresh && _gpsLocationService.DeviceLocation != null) return true;

            // Deliberately not IsBusy: the list is already on screen by now and the fix takes up
            // to 30s (forever, on an emulator with no provider). A blocking spinner over usable
            // content would be the only thing that ever showed.
            _isFetchingDeviceLocation = true;

            try
            {
                var locationResult = await _gpsLocationService.GetDeviceGeoLocation();

                if (!locationResult.IsSuccessful)
                {
                    return _gpsLocationService.DeviceLocation != null;
                }

                return true;
            }
            finally
            {
                _isFetchingDeviceLocation = false;
            }
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

            return Math.Abs(left.Latitude - right.Latitude) < 0.000001
                && Math.Abs(left.Longitude - right.Longitude) < 0.000001
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

                    // "Close" used to quit the app, from when a location fix was mandatory.
                    // It no longer is — there is always a placeholder — so dismissing is enough.
                    if (result == DialogButton.Negative)
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

            // A CollectionView clears SelectedItem whenever the selected row leaves the
            // collection — while the list is rebuilt, for instance. That is not the user
            // choosing "nowhere", and writing it back erased the location they had picked,
            // along with the preference that remembers it across launches.
            if (value is null) return;

            if (ReferenceEquals(value, _gpsLocationService.SelectedLocation)) return;

            _gpsLocationService.SelectedLocation = value;
            UpdateAtThisLocationInfo(value);
        }

        private void UpdateAtThisLocationInfo(Location? location)
        {
            IsPlaceholderLocation = !global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation;

            if (location is null)
            {
                Coordinate = null;
                WeatherForecast = null;
                StopTimer();
                CancelWeatherLoad();
                return;
            }

            var offsetHours = location.GetUtcOffsetHours(DateTime.UtcNow);

            Coordinate = CelestialSnapshot.For(
                location.Latitude,
                location.Longitude,
                DateTime.UtcNow.AddHours(offsetHours),
                offsetHours);

            RestartTimer(offsetHours);
            _ = LoadWeatherForecastAsync(location.Latitude, location.Longitude);
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

        [RelayCommand]
        private async Task GoToAddLocation()
        {
            Dictionary<LocationInputContext, Location?> locationAndContext = [];
            locationAndContext[LocationInputContext.Add] = null;
            await _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        [RelayCommand]
        private async Task GoToSearchByCity()
        {
            var searchLocationContext = SearchLocationContext.GoAhead;
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(searchLocationContext);
        }

        /// <summary>
        /// Edits a saved place in place of the old management screen. The edit screen works on a
        /// copy, so backing out leaves this list untouched.
        /// </summary>
        [RelayCommand]
        private async Task EditLocation(Location? location)
        {
            if (location is null || !location.IsSaved) return;

            Dictionary<LocationInputContext, Location?> locationAndContext = [];
            locationAndContext[LocationInputContext.Edit] = location;
            await _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        [RelayCommand]
        private async Task DeleteLocation(Location? location)
        {
            if (location is null || !location.IsSaved) return;

            // Removing the place every other screen is currently reading would leave the app
            // deciding where the user is on their behalf; ask them to switch away first.
            if (ReferenceEquals(location, SelectedLocation) || location.Id == SelectedLocation?.Id)
            {
                await _toastService.ShowToast(
                    _localizer.GetString("CannotDeleteCurrentLocationToastMessage"));
                return;
            }

            await _locationService.DeleteLocation(location);
            Locations.Remove(location);
        }

        [RelayCommand]
        private async Task Refresh()
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

        /// <summary>
        /// Pushes the service's notion of the current location into the UI without letting the
        /// two-way binding echo it straight back.
        /// </summary>
        private Task ApplySelection(Location? selected) =>
            MainThread.InvokeOnMainThreadAsync(() =>
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

                UpdateAtThisLocationInfo(selected);
            });

        private async Task InitViewModelAsync(bool forceRefreshDeviceLocation = false)
        {
            if (!await _initializationSemaphore.WaitAsync(0))
            {
                // An init can sit on a device fix for up to 30s. Dropping the request outright
                // meant anything that happened meanwhile — a location added, edited, deleted —
                // stayed invisible until the next navigation. Queue one re-run instead.
                _initializationPending = true;
                return;
            }

            try
            {
                // Put something on screen before the permission dance, which can block on a
                // dialog: the service always hands back at least a placeholder, and an empty
                // Locations screen is never the right answer.
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _suppressSelectionWriteBack = true;
                    EnsureKnownLocationsVisible();
                    SelectedLocation ??= _gpsLocationService.SelectedLocation;
                    _suppressSelectionWriteBack = false;
                });

                // Saved locations are a local database read, so they go up first. They used to sit
                // behind the device fix below, which meant a place the user had just added stayed
                // invisible for the 30 seconds that fix takes to give up.
                await UpdateSavedLocations();
                await _gpsLocationService.RestorePersistedSelectedLocation();

                await ApplySelection(_gpsLocationService.SelectedLocation);

                // A denied or unavailable device fix is not fatal — the app falls back to a
                // placeholder — so it runs last and only adds to what is already on screen.
                await InitDeviceLocation(forceRefreshDeviceLocation);

                await ApplySelection(_gpsLocationService.SelectedLocation);
            }
            finally
            {
                _initializationSemaphore.Release();

                if (_initializationPending)
                {
                    _initializationPending = false;
                    _ = InitViewModelAsync();
                }
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
                case LocationInputContext.Add:
                    {
                        // Adding a place is a statement of intent, so make it current rather than
                        // leaving the user to find it in the list and tap it a second time.
                        UpsertLocation(message.Location);
                        SelectedLocation = message.Location;
                        break;
                    }
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
    }
}
