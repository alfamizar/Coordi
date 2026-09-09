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
using JustCompute.Shared.Abstractions.UI;
using Compute.Core.Common.Results;
using JustCompute.Features.InputLocation;
using JustCompute.Features.SearchByCity;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Specialized;
using Location = Compute.Core.Domain.Entities.Models.Location;
using JustCompute.Shared.Helpers;

namespace JustCompute.Features.Locations
{
    public partial class LocationsViewModel : BaseViewModel, IRecipient<LocationMessage>
    {

        private readonly IDevicePermissionsService<PermissionStatus> _devicePermissionsService;
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly LocationClock _clock;

        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _permissionDialogOpen;
        private bool _isFetchingDeviceLocation;
        private bool _initializationPending;
        private bool _pendingUserInitiated;

        // Suppresses the SelectedLocation write-back while we restore the selection ourselves.
        // Mutating Locations makes the CarouselView snap CurrentItem to index 0, which would
        // otherwise echo through the two-way binding and clobber the user's selection.
        private bool _suppressSelectionWriteBack;

        [ObservableProperty]
        private int locationsCount;

        /// <summary>
        /// True while the app has nowhere real to compute from — no chosen place and no device
        /// fix. Drives the onboarding card at the top of the screen, and its absence reveals the
        /// "Add a location" card at the bottom, which offers the same two buttons as a suggestion
        /// rather than as something standing between the user and the app.
        /// </summary>
        [ObservableProperty]
        private bool isPlaceholderLocation;

        /// <summary>
        /// Whether to offer the button that asks for the device's position. Only while the
        /// permission is missing: once it is granted the fix arrives on its own, and a button
        /// asking for what the app already has would be noise.
        /// </summary>
        [ObservableProperty]
        private bool canRequestDeviceLocation;

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


        public LocationsViewModel(
            ViewModelServices services,
            IStringLocalizer<AppStringsRes> localizer,
            IDevicePermissionsService<PermissionStatus> devicePermissionsService,
            IMessagingService messagerService,
            IToastService toastService)
            : base(services)
        {
            _localizer = localizer;
            _clock = new LocationClock(time => CurrentTime = time);
            _devicePermissionsService = devicePermissionsService;
            _toastService = toastService;

            IsPlaceholderLocation = _gpsLocationService.ShouldPromptForLocation;
            CanRequestDeviceLocation = _permissionGate.LastKnownLocationPermissionGranted != true;

            Locations.CollectionChanged += Locations_CollectionChanged;
            messagerService.Subscribe<IRecipient<LocationMessage>, LocationMessage>(this);
            _permissionGate.LocationPermissionStateChanged += OnLocationPermissionStateChanged;
        }

        private void OnLocationPermissionStateChanged(object? sender, bool isGranted)
        {
            CanRequestDeviceLocation = !isGranted;

            if (isGranted)
            {
                _ = InitViewModelAsync(forceRefreshDeviceLocation: true);
            }
        }



        /// <summary>Restarts the ticking clock for the place currently on screen.</summary>
        public void RestartTimer(double offsetHours = 0) => _clock.Start(offsetHours);

        private void Locations_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            LocationsCount = Locations.Count;
        }

        private async Task<bool> InitDeviceLocation(bool forceRefresh, bool userInitiated)
        {
            if (_isFetchingDeviceLocation) return false;

            // Before the permission check, not after: with a fix already in hand and no refresh
            // asked for there is nothing here to need permission for.
            if (!forceRefresh && _gpsLocationService.DeviceLocation != null) return true;

            if (!await HandlePermissions(userInitiated)) return false;

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
                var newLocations = LocationList.MissingFrom(savedLocations, Locations);

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

            if (_gpsLocationService.SelectedLocation is { } selected)
            {
                // The placeholder is a fallback so the other screens have data before anywhere is
                // picked - not a place the user chose - so it earns a row only while there is
                // nothing else to show. Listing it unconditionally left an undeletable London
                // sitting beside the real entries: with no persisted id it draws no edit or delete
                // affordance, and DeleteLocation refuses unsaved rows outright, so tapping it did
                // nothing at all. Determining the device position did not help either, because the
                // fix is added as its own row and SelectedLocation stays on the placeholder until
                // something is chosen.
                bool isPlaceholder = LocationIdentity.IsPlaceholder(selected);

                if (!isPlaceholder || Locations.Count == 0)
                {
                    UpsertLocation(selected, insertAtStart: Locations.Count == 0);
                }
            }

            DropPlaceholderOnceSomethingRealExists();
        }

        /// <summary>
        /// Removes a placeholder left over from an earlier pass. It can be added before the device
        /// fix or the saved rows arrive, and without this it would simply stay.
        /// </summary>
        private void DropPlaceholderOnceSomethingRealExists()
        {
            if (Locations.Count < 2) return;

            var placeholder = Locations.FirstOrDefault(LocationIdentity.IsPlaceholder);
            if (placeholder is not null)
            {
                Locations.Remove(placeholder);
            }
        }

        private void MarkAsCurrentDeviceLocation(Location deviceLocation)
        {
            var deviceSlot = Locations.FirstOrDefault(LocationIdentity.IsDeviceSlot);

            if (deviceSlot is not null && !ReferenceEquals(deviceSlot, deviceLocation))
            {
                // Move the fix onto the entry already in the list: the carousel is bound to that
                // instance, so replacing it would lose the user's place in it.
                LocationList.CopyPositionInto(deviceSlot, deviceLocation);
                _gpsLocationService.DeviceLocation = deviceSlot;
                return;
            }

            deviceLocation.IsCurrent = true;
            UpsertLocation(deviceLocation, insertAtStart: true);
        }

        private void UpsertLocation(Location location, bool insertAtStart = false) =>
            LocationList.Upsert(Locations, location, insertAtStart);

        /// <summary>
        /// Gets the location permission, if the moment is right to ask for it.
        /// </summary>
        /// <param name="userInitiated">
        /// True when the user asked for their position outright. Opening this screen is not such
        /// a request: the app works without the permission — search for a city, or type
        /// coordinates — and it used to raise a modal on every single visit for someone who had
        /// declined, on a screen whose own dialog told them the permission was optional.
        /// </param>
        protected async Task<bool> HandlePermissions(bool userInitiated)
        {
            if (!userInitiated)
            {
                // One automatic request, ever, so someone who would have granted it is not left
                // hunting for the button. From then on the offer lives on the button alone.
                if (global::JustCompute.Shared.Helpers.Settings.HasAskedForLocationPermission)
                {
                    bool granted = await _permissionGate.RefreshLocationPermissionState();
                    CanRequestDeviceLocation = !granted;
                    return granted;
                }

                global::JustCompute.Shared.Helpers.Settings.HasAskedForLocationPermission = true;
            }

            PermissionStatus permissionStatus = await _devicePermissionsService
                .CheckPermissionAndRequestIfNeeded(Permission.DeviceLocation);

            CanRequestDeviceLocation = !await _permissionGate.RefreshLocationPermissionState();

            if (permissionStatus == PermissionStatus.Denied)
            {
                // Declining the request the app made of its own accord is an answer, not a
                // problem to be solved with a second dialog. Only somebody who came looking for
                // their location is owed an explanation of why they did not get it.
                if (!userInitiated || _permissionDialogOpen)
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
            _clock.Stop();
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
            IsPlaceholderLocation = _gpsLocationService.ShouldPromptForLocation;

            if (location is null)
            {
                Coordinate = null;
                _clock.Stop();
                return;
            }

            var offsetHours = location.GetUtcOffsetHours(DateTime.UtcNow);

            Coordinate = CelestialSnapshot.For(
                location.Latitude,
                location.Longitude,
                DateTime.UtcNow.AddHours(offsetHours),
                offsetHours);

            RestartTimer(offsetHours);
        }

        /// <summary>
        /// The one place the app asks for the location permission on purpose. Everything else
        /// makes do with whatever has already been granted.
        /// </summary>
        [RelayCommand]
        private Task UseMyLocation() =>
            InitViewModelAsync(forceRefreshDeviceLocation: true, userInitiated: true);

        [RelayCommand]
        private async Task GoToAddLocation()
        {
            await _navigationService.NavigateToAsync<InputLocationViewModel>(
                new LocationEditorArgs(LocationInputContext.Add, null));
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

            await _navigationService.NavigateToAsync<InputLocationViewModel>(
                new LocationEditorArgs(LocationInputContext.Edit, location));
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

        private async Task InitViewModelAsync(bool forceRefreshDeviceLocation = false, bool userInitiated = false)
        {
            if (!await _initializationSemaphore.WaitAsync(0))
            {
                // An init can sit on a device fix for up to 30s. Dropping the request outright
                // meant anything that happened meanwhile — a location added, edited, deleted —
                // stayed invisible until the next navigation. Queue one re-run instead, keeping
                // the intent: a tap on "Use my location" must still ask when its turn comes.
                _initializationPending = true;
                _pendingUserInitiated |= userInitiated;
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
                await InitDeviceLocation(forceRefreshDeviceLocation, userInitiated);

                await ApplySelection(_gpsLocationService.SelectedLocation);
            }
            finally
            {
                _initializationSemaphore.Release();

                if (_initializationPending)
                {
                    _initializationPending = false;
                    bool wasUserInitiated = _pendingUserInitiated;
                    _pendingUserInitiated = false;
                    _ = InitViewModelAsync(wasUserInitiated, wasUserInitiated);
                }
            }
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
