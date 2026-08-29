using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
using DotNext;
using Compute.Core.Domain.Errors;
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
        private Task? _restoreTask;

        /// <summary>
        /// Never null: until the user picks somewhere (or the device fix arrives) this returns a
        /// placeholder, so screens show real data instead of an error. Pair with
        /// <c>Settings.HasUserSetLocation</c> to tell the two apart.
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
                    global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation = true;
                }
            }
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
        public Task RestorePersistedSelectedLocation() => _restoreTask ??= RestorePersistedSelectedLocationCore();

        private async Task RestorePersistedSelectedLocationCore()
        {
            // Reading the property hands back a placeholder and caches it, so "already set" is not
            // the same as "chosen by the user" — any screen that asked first would otherwise block
            // the restore and the app would forget the user's location on every cold start.
            if (_selectedLocation != null && !ReferenceEquals(_selectedLocation, _placeholderLocation))
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
                    _deviceLocation = value;
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

                SelectedLocation ??= DeviceLocation;

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
                return new(FaultCode.CouldNotStopListeningDeciveGeoLocation);
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
                return new Result<bool, FaultCode>(FaultCode.CouldNotStartListeningDeciveGeoLocation);
            }
        }

        private void StopForegroundOnlyListening()
        {
            Geolocation.LocationChanged -= OnMauiLocationChanged;
            Geolocation.ListeningFailed -= OnMauiListeningFailed;
            Geolocation.StopListeningForeground();
        }

        private void OnMauiLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            DeviceLocationUpdated?.Invoke(this, ToDomainUpdate(e.Location));
        }

        private void OnMauiListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
        {
            DeviceLocationListeningFailed?.Invoke(this, new DeviceLocationListeningFailure(e.Error.ToString()));
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
