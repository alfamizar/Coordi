using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services;
using Compute.Core.Common.Exceptions.Location;
using JustCompute.Presentation.Tasks;
using Polly.Retry;

namespace JustCompute.Services.LocationService
{
    /// <summary>
    /// Where the device is: one-shot fixes, and the continuous stream a trip records.
    ///
    /// Deliberately nothing else. Which place the app computes from used to be decided in here
    /// too, which put a domain policy inside a MAUI class that no test could reach and wrote a
    /// user's choice into static settings as a side effect of a property setter. That policy is
    /// LocationSelectionService in Compute.Core now, and listens to the fixes this class reports.
    /// </summary>
    public partial class GPSLocationService(ILocationService locationService, AsyncRetryPolicy retryPolicy)
        : IDeviceLocationProvider, IDeviceLocationTracker
    {
        private readonly WeakEventManager _eventManager = new();
        private readonly ILocationService _locationService = locationService;
        private readonly AsyncRetryPolicy _retryPolicy = retryPolicy;

        private CancellationTokenSource? _cancelTokenSource;
        private int _listenerRefCount;
        private bool _backgroundCapable;

        /// <summary>Private: callers wait on the request, they do not get to finish it.</summary>
        private TaskCompletionSource<bool>? _pendingFix;

        /// <summary>
        /// The one fix request out at a time. Requests used to run side by side and overwrite each
        /// other's pending task and cancellation source: the first to finish declared nothing in
        /// flight while the second was still out, and released its waiters early.
        /// </summary>
        private readonly SharedRequest<(Result<Location, FaultCode> Result, bool Announced)> _fixRequest = new();

        /// <summary>
        /// Whether anyone sharing the run wants its fix announced. Read when the fix lands, so it
        /// is announced before the request settles and a screen waiting on it finds it in place.
        /// </summary>
        private volatile bool _announceRequested;

        public bool IsGettingDeviceLocation { get; private set; }

        private Location? _deviceLocation;

        public Location? DeviceLocation => _deviceLocation;

        public Task WaitForPendingFixAsync() =>
            IsGettingDeviceLocation && _pendingFix is { } pending ? pending.Task : Task.CompletedTask;

        public void KeepListInstance(Location listInstance)
        {
            ArgumentNullException.ThrowIfNull(listInstance);

            // Only the device's own row may stand for the device. Anything else here would make
            // a saved place, or the placeholder, silently become "where I am".
            if (!LocationIdentity.IsDeviceSlot(listInstance))
            {
                throw new ArgumentException("Only the device's own row can stand for its position.", nameof(listInstance));
            }

            SetDeviceLocation(listInstance);
        }

        private void SetDeviceLocation(Location next)
        {
            if (ReferenceEquals(_deviceLocation, next)) return;

            var previous = _deviceLocation;
            _deviceLocation = next;
            _eventManager.HandleEvent(this, new DeviceLocationChangedEventArgs(previous, next), nameof(DeviceLocationChanged));
        }

        public event EventHandler<DeviceLocationChangedEventArgs> DeviceLocationChanged
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public event EventHandler<DeviceLocationUpdate>? DeviceLocationUpdated;
        public event EventHandler<DeviceLocationListeningFailure>? DeviceLocationListeningFailed;

        public Task<Result<Location, FaultCode>> GetDeviceGeoLocation() => RequestFixAsync(announce: true);

        public Task<Result<Location, FaultCode>> FetchDeviceGeoLocationAsync() => RequestFixAsync(announce: false);

        public void PublishDeviceLocation(Location position)
        {
            ArgumentNullException.ThrowIfNull(position);

            position.IsCurrent = true;
            SetDeviceLocation(position);
        }

        private async Task<Result<Location, FaultCode>> RequestFixAsync(bool announce)
        {
            // Said before joining, so a run already out announces its fix for this caller too.
            if (announce) _announceRequested = true;

            var (result, announced) = await _fixRequest.RunAsync(RunFixRequestAsync);

            // Joined after the run had decided: announce it now. A no-op for the same object.
            if (announce && !announced && result.IsSuccessful)
            {
                SetDeviceLocation(result.Value);
            }

            return result;
        }

        private async Task<(Result<Location, FaultCode> Result, bool Announced)> RunFixRequestAsync()
        {
            try
            {
                // RunContinuationsAsynchronously, deliberately: without it TrySetResult runs
                // every awaiting continuation inline on whichever thread the platform delivered
                // the fix on. Screens awaiting this then carried on off the UI thread, and their
                // property changes stopped reaching the views — a spinner that never stopped.
                _pendingFix = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                IsGettingDeviceLocation = true;

                // Medium, not Best: the network's answer arrives in a second or two, where the GPS
                // chip can take the whole half minute indoors. Nothing here needs more — a
                // kilometre east or west moves sunrise by about three seconds. Speed and Distance
                // asks for its own, precise stream and is not affected.
                GeolocationRequest request = new(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(30));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceGeoLocation? deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token)
                    ?? throw new DeviceLocationUnavailableException("await Geolocation.Default.GetLocationAsync returned null");

                var fix = await _locationService.GetLocationFromCoordinates(deviceLocation.Latitude, deviceLocation.Longitude);

                // Marked before it is announced. It used to be set after, so every listener saw
                // the device's own position claim not to be the device's own position.
                fix.IsCurrent = true;

                // Announced before the request settles, so a screen waiting on it finds the fix
                // already in place — when anyone sharing the run asked for that. A caller deciding
                // first, the way the launch-time refresh does, gets it unannounced.
                bool announce = _announceRequested;
                if (announce)
                {
                    SetDeviceLocation(fix);
                }

                return (fix, announce);
            }
            catch (FeatureNotSupportedException)
            {
                return (new(FaultCode.FeatureNotSupported), false);
            }
            catch (FeatureNotEnabledException)
            {
                return (new(FaultCode.FeatureNotEnabled), false);
            }
            catch (PermissionException)
            {
                return (new(FaultCode.PermissionException), false);
            }
            catch (DeviceLocationUnavailableException)
            {
                return (new(FaultCode.DeviceLocationUnavailable), false);
            }
            catch (Exception)
            {
                return (new(FaultCode.GenericGetLocationException), false);
            }
            finally
            {
                _announceRequested = false;
                IsGettingDeviceLocation = false;
                _pendingFix?.TrySetResult(true);
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
