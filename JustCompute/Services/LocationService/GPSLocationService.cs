using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
using DotNext;
using Compute.Core.Domain.Errors;
using Compute.Core.Domain.Services;
using Compute.Core.Common.Exceptions.Location;
using Polly.Retry;

namespace JustCompute.Services.LocationService
{
    public class GPSLocationService(ILocationService locationService, AsyncRetryPolicy retryPolicy) : IGPSLocationService
    {
        private readonly WeakEventManager _eventManager = new();
        private readonly ILocationService _locationService = locationService;
        private readonly AsyncRetryPolicy _retryPolicy = retryPolicy;

        private CancellationTokenSource? _cancelTokenSource;

        public TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; private set; }
        public bool IsGettingDeviceLocation { get; private set; }

        public Location? SelectedLocation { get; set; }

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
                        // Notify subscribers
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

        private void OnDeviceLocationChanged()
        {
            _eventManager.HandleEvent(this, EventArgs.Empty, nameof(DeviceLocationChanged));
        }

        public async Task<Result<Location, FaultCode>> GetDeviceGeoLocation()
        {
            try
            {
                GettingDeviceLocationFinished = new TaskCompletionSource<bool>();

                IsGettingDeviceLocation = true;

                GeolocationRequest request = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceGeoLocation? deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token)
                    ?? throw new DeviceLocationUnavailableException("await Geolocation.Default.GetLocationAsync returned null");

                DeviceLocation = await _locationService.GetLocationFromCoordinates(deviceLocation.Latitude, deviceLocation.Longitude);

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

        public async Task<Result<bool, FaultCode>> OnStartListeningDeciveGeoLocation<T, U>(
            EventHandler<T> locationChangedCallback, EventHandler<U> listeningFailedCallback) where T : class where U : class
        {
            try
            {
                Geolocation.LocationChanged += locationChangedCallback as EventHandler<GeolocationLocationChangedEventArgs>;
                Geolocation.ListeningFailed += listeningFailedCallback as EventHandler<GeolocationListeningFailedEventArgs>;
                var request = new GeolocationListeningRequest((GeolocationAccuracy.Best));
                var success = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await Geolocation.StartListeningForegroundAsync(request);
                });
                return success;
            }
            catch (Exception)
            {
                Geolocation.LocationChanged -= locationChangedCallback as EventHandler<GeolocationLocationChangedEventArgs>;
                Geolocation.ListeningFailed -= listeningFailedCallback as EventHandler<GeolocationListeningFailedEventArgs>;
                return new(FaultCode.CouldNotStartListeningDeciveGeoLocation);
            }
        }

        public Result<bool, FaultCode> OnStopListeningDeciveGeoLocation<T, U>(
            EventHandler<T> locationChangedCallback, EventHandler<U> listeningFailedCallback) where T : class where U : class
{
            try
            {
                Geolocation.LocationChanged -= locationChangedCallback as EventHandler<GeolocationLocationChangedEventArgs>;
                Geolocation.ListeningFailed -= listeningFailedCallback as EventHandler<GeolocationListeningFailedEventArgs>;
                Geolocation.StopListeningForeground();
                return true;
            }
            catch (Exception)
            {
                return new(FaultCode.CouldNotStopListeningDeciveGeoLocation);
            }
        }
    }
}
