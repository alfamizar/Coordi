using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services;
using Compute.Core.Common.Exceptions.Location;
using Polly.Retry;

namespace JustCompute.Services.LocationService
{
    public partial class GPSLocationService(ILocationService locationService, AsyncRetryPolicy retryPolicy) : IGPSLocationService
    {
        private const string SelectedLocationIdKey = "selected_location_id";

        private readonly WeakEventManager _eventManager = new();
        private readonly ILocationService _locationService = locationService;
        private readonly AsyncRetryPolicy _retryPolicy = retryPolicy;

        private CancellationTokenSource? _cancelTokenSource;
        private int _listenerRefCount;
        private bool _backgroundCapable;

        public TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; private set; }
        public bool IsGettingDeviceLocation { get; private set; }

        private Location? _selectedLocation;
        private Location? _placeholderLocation;
        private Location? _adoptedDeviceLocation;
        private Task? _restoreTask;
        private readonly Lock _restoreGate = new();

        /// <summary>
        /// Never null: until the user picks somewhere (or the device fix arrives) this returns a
        /// placeholder, so screens show real data instead of an error. Pair with
        /// <see cref="ShouldPromptForLocation"/> to tell the two apart.
        /// </summary>
        public Location? SelectedLocation
        {
            get => _selectedLocation ??= _placeholderLocation ??= Location.CreatePlaceholder();
            set
            {
                _selectedLocation = value;
                PersistSelectedLocationId(value?.Id);

                // Only count it once we are genuinely off the placeholder. Every screen assigns
                // this property during its own load, so treating any assignment as "the user
                // chose somewhere" dismissed the onboarding card on the very first run and left
                // London sitting in the list as though it were a place they had picked.
                if (value != null && !ReferenceEquals(value, _placeholderLocation))
                {
                    // Assigning is a choice even when it lands on the device's own row, so the
                    // adoption below is no longer standing in for one.
                    _adoptedDeviceLocation = null;
                    global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation = true;
                }
            }
        }

        /// <summary>
        /// Whether the selection is somewhere the user actually picked, rather than one of the two
        /// stand-ins the app fills in for them: the invented placeholder, and the device's own fix
        /// adopted by <see cref="AdoptDeviceLocation"/>. Both must give way to a persisted choice
        /// when one is restored, and neither may block that restore.
        /// </summary>
        private bool IsUserChoice =>
            LocationSelection.IsUserChoice(_selectedLocation, _placeholderLocation, _adoptedDeviceLocation);

        public bool ShouldPromptForLocation =>
            !global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation
            && (_selectedLocation is null || ReferenceEquals(_selectedLocation, _placeholderLocation));

        /// <summary>
        /// Makes the device's own fix the place the app computes from, for as long as the user has
        /// not chosen one. Without this the app knew where it was, listed it, and still reported
        /// the placeholder's London to every screen.
        /// </summary>
        /// <param name="previous">
        /// The fix being replaced. The Locations screen moves a fresh fix onto the row already in
        /// the list and hands that row back here, so a selection pointing at the old instance has
        /// to follow it across or it silently goes stale.
        /// </param>
        private void AdoptDeviceLocation(Location? previous)
        {
            if (_deviceLocation is null) return;

            if (!LocationSelection.ShouldAdoptDeviceFix(
                    _selectedLocation, _placeholderLocation, _adoptedDeviceLocation, previous))
            {
                return;
            }

            // Deliberately not through the setter: the app is filling in a blank, not recording a
            // decision. Persisting an id or setting HasUserSetLocation would dismiss the prompt to
            // save a place and make the next launch treat a passing fix as a settled choice.
            _selectedLocation = _deviceLocation;
            _adoptedDeviceLocation = _deviceLocation;
        }

        private static void PersistSelectedLocationId(int? id)
        {
            if (id is int validId && validId > 0)
            {
                Preferences.Default.Set(SelectedLocationIdKey, validId);
            }
            else
            {
                Preferences.Default.Remove(SelectedLocationIdKey);
            }
        }

        /// <summary>
        /// Restores the location the user last chose. Runs at most once per launch and is safe to
        /// await from anywhere, so every screen can gate its first read on it rather than relying
        /// on the user happening to open the Locations screen.
        /// </summary>
        public Task RestorePersistedSelectedLocation()
        {
            // Locked, not just ??=: that reads and assigns in two steps, so two screens loading
            // at once on a cold start could both find it null and both run the restore — two
            // database reads, and two answers racing to become the selected location.
            lock (_restoreGate)
            {
                return _restoreTask ??= RestorePersistedSelectedLocationCore();
            }
        }

        private async Task RestorePersistedSelectedLocationCore()
        {
            // Reading the property hands back a placeholder and caches it, so "already set" is not
            // the same as "chosen by the user" — any screen that asked first would otherwise block
            // the restore and the app would forget the user's location on every cold start. An
            // adopted device fix is a stand-in for the same reason and must not block it either.
            if (IsUserChoice)
            {
                return;
            }

            var id = Preferences.Default.Get(SelectedLocationIdKey, -1);
            if (id <= 0) return;

            var saved = await _locationService.GetSavedLocations();
            var match = saved.FirstOrDefault(l => l.Id == id);
            if (match != null)
            {
                _selectedLocation = match;
            }
        }

        private Location? _deviceLocation;
        public Location? DeviceLocation
        {
            get => _deviceLocation;
            set
            {
                if (_deviceLocation != value)
                {
                    var previous = _deviceLocation;
                    _deviceLocation = value;
                    AdoptDeviceLocation(previous);

                    if (_deviceLocation != null)
                    {
                        OnDeviceLocationChanged();
                    }
                }
            }
        }

        public event EventHandler<EventArgs> DeviceLocationChanged
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public event EventHandler<DeviceLocationUpdate>? DeviceLocationUpdated;
        public event EventHandler<DeviceLocationListeningFailure>? DeviceLocationListeningFailed;

        private void OnDeviceLocationChanged()
        {
            _eventManager.HandleEvent(this, EventArgs.Empty, nameof(DeviceLocationChanged));
        }

        public async Task<Result<Location, FaultCode>> GetDeviceGeoLocation()
        {
            try
            {
                // RunContinuationsAsynchronously, deliberately: without it TrySetResult runs
                // every awaiting continuation inline on whichever thread the platform delivered
                // the fix on. Screens awaiting this then carried on off the UI thread, and their
                // property changes stopped reaching the views — a spinner that never stopped.
                GettingDeviceLocationFinished =
                    new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                IsGettingDeviceLocation = true;

                GeolocationRequest request = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceGeoLocation? deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token)
                    ?? throw new DeviceLocationUnavailableException("await Geolocation.Default.GetLocationAsync returned null");

                DeviceLocation = await _locationService.GetLocationFromCoordinates(deviceLocation.Latitude, deviceLocation.Longitude);
                DeviceLocation.IsCurrent = true;

                // No selection to make here: the property above never returns null, so the ??=
                // that used to sit on this line could not fire. The DeviceLocation setter adopts
                // the fix instead, which also covers the fixes that arrive from elsewhere.
                return DeviceLocation;
            }
            catch (FeatureNotSupportedException)
            {
                return new(FaultCode.FeatureNotSupported);
            }
            catch (FeatureNotEnabledException)
            {
                return new(FaultCode.FeatureNotEnabled);
            }
            catch (PermissionException)
            {
                return new(FaultCode.PermissionException);
            }
            catch (DeviceLocationUnavailableException)
            {
                return new(FaultCode.DeviceLocationUnavailable);
            }
            catch (Exception)
            {
                return new(FaultCode.GenericGetLocationException);
            }
            finally
            {
                IsGettingDeviceLocation = false;
                GettingDeviceLocationFinished?.TrySetResult(true);
            }
        }

        public void CancelRequest()
        {
            if (IsGettingDeviceLocation && _cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
                _cancelTokenSource.Cancel();
        }

        public async Task<Result<bool, FaultCode>> StartListeningForDeviceGeoLocation(bool backgroundCapable = false)
        {
            if (Interlocked.Increment(ref _listenerRefCount) > 1)
            {
                // Already listening — but possibly not the way this caller needs. A trip that
                // must keep recording with the screen off cannot be served by the foreground-only
                // listener a screen started earlier, and silently returning success here left
                // tracking that quietly stopped the moment the app was backgrounded.
                if (backgroundCapable && !_backgroundCapable)
                {
                    return await UpgradeToBackgroundListening();
                }

                return true;
            }

            _backgroundCapable = backgroundCapable;
            var result = backgroundCapable
                ? await StartPlatformListening()
                : await StartForegroundOnlyListening();

            if (!result.IsSuccessful)
            {
                Interlocked.Decrement(ref _listenerRefCount);
            }
            return result;
        }

        /// <summary>
        /// Swaps a running foreground-only listener for the background-capable platform service,
        /// keeping the reference count intact — the screens already listening still are.
        /// </summary>
        private async Task<Result<bool, FaultCode>> UpgradeToBackgroundListening()
        {
            try
            {
                StopForegroundOnlyListening();
            }
            catch (Exception)
            {
                // Nothing useful to do: the platform listener is what matters, and it is next.
            }

            var result = await StartPlatformListening();
            if (result.IsSuccessful)
            {
                _backgroundCapable = true;
                return result;
            }

            // Put the foreground listener back so the callers already relying on updates keep
            // getting them, rather than being left with nothing at all.
            await StartForegroundOnlyListening();
            Interlocked.Decrement(ref _listenerRefCount);
            return result;
        }

        public Result<bool, FaultCode> StopListeningForDeviceLocation()
        {
            if (_listenerRefCount <= 0)
            {
                return true;
            }

            if (Interlocked.Decrement(ref _listenerRefCount) > 0)
            {
                return true;
            }

            try
            {
                if (_backgroundCapable)
                {
                    StopPlatformListening();
                }
                else
                {
                    StopForegroundOnlyListening();
                }
                return true;
            }
            catch (Exception)
            {
                return new(FaultCode.CouldNotStopListeningDeviceGeoLocation);
            }
        }

        private async Task<Result<bool, FaultCode>> StartForegroundOnlyListening()
        {
            try
            {
                Geolocation.LocationChanged += OnMauiLocationChanged;
                Geolocation.ListeningFailed += OnMauiListeningFailed;

                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
                var success = await _retryPolicy.ExecuteAsync(
                    async () => await Geolocation.StartListeningForegroundAsync(request));
                return success;
            }
            catch (Exception)
            {
                Geolocation.LocationChanged -= OnMauiLocationChanged;
                Geolocation.ListeningFailed -= OnMauiListeningFailed;
                return new Result<bool, FaultCode>(FaultCode.CouldNotStartListeningDeviceGeoLocation);
            }
        }

        private void StopForegroundOnlyListening()
        {
            Geolocation.LocationChanged -= OnMauiLocationChanged;
            Geolocation.ListeningFailed -= OnMauiListeningFailed;
            Geolocation.StopListeningForeground();
        }

        // Marshalled, like the background-capable path already is: MAUI raises these on a
        // platform thread, and every subscriber writes straight into properties a screen is bound
        // to. Off the UI thread those writes are dropped frames at best.
        private void OnMauiLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            var update = ToDomainUpdate(e.Location);
            MainThread.BeginInvokeOnMainThread(() => DeviceLocationUpdated?.Invoke(this, update));
        }

        private void OnMauiListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
        {
            var failure = new DeviceLocationListeningFailure(e.Error.ToString());
            MainThread.BeginInvokeOnMainThread(() => DeviceLocationListeningFailed?.Invoke(this, failure));
        }

        private partial Task<Result<bool, FaultCode>> StartPlatformListening();
        private partial void StopPlatformListening();

        private void RaiseLocationChanged(DeviceGeoLocation location)
        {
            var handler = DeviceLocationUpdated;
            if (handler == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
                handler.Invoke(this, ToDomainUpdate(location)));
        }

        private void RaiseListeningFailed(GeolocationError error)
        {
            var handler = DeviceLocationListeningFailed;
            if (handler == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
                handler.Invoke(this, new DeviceLocationListeningFailure(error.ToString())));
        }

        private static DeviceLocationUpdate ToDomainUpdate(DeviceGeoLocation l) =>
            new(l.Latitude, l.Longitude, l.Speed, l.Course, l.Accuracy, l.VerticalAccuracy, l.Altitude, l.Timestamp);
    }
}
