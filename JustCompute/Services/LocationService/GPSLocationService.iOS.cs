using Compute.Core.Domain.Errors;
using CoreLocation;
using DotNext;
using Foundation;
using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace JustCompute.Services.LocationService
{
    public partial class GPSLocationService
    {
        private CLLocationManager? _locationManager;
        private BackgroundLocationDelegate? _locationDelegate;

        private partial async Task<Result<bool, FaultCode>> StartPlatformListening()
        {
            try
            {
                // Make sure WhenInUse authorization is granted before touching CLLocationManager.
                // Configuring background updates without granted auth, or starting updates with
                // status NotDetermined, can throw an NSException.
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (permissionStatus != PermissionStatus.Granted)
                {
                    return new Result<bool, FaultCode>(FaultCode.PermissionException);
                }

                DisposeLocationManager();

                _locationManager = new CLLocationManager
                {
                    DesiredAccuracy = CLLocation.AccuracyBest,
                    DistanceFilter = CLLocationDistance.FilterNone,
                    PausesLocationUpdatesAutomatically = false,
                    ActivityType = CLActivityType.Fitness
                };

                _locationDelegate = new BackgroundLocationDelegate(this);
                _locationManager.Delegate = _locationDelegate;

                // Background updates require UIBackgroundModes=location in Info.plist AND that
                // the bundle's plist was actually rebuilt to include it. If either is missing,
                // setting AllowsBackgroundLocationUpdates throws — catch and degrade to
                // foreground-only tracking so the user still sees live values on the page.
                try
                {
                    _locationManager.AllowsBackgroundLocationUpdates = true;
                    _locationManager.ShowsBackgroundLocationIndicator = true;
                }
                catch (Exception bgEx)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GPSLocationService.iOS] background config failed (will run foreground-only): {bgEx.Message}");
                }

                _locationManager.StartUpdatingLocation();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPSLocationService.iOS] StartPlatformListening failed: {ex}");
                DisposeLocationManager();
                return new Result<bool, FaultCode>(FaultCode.CouldNotStartListeningDeciveGeoLocation);
            }
        }

        private partial void StopPlatformListening()
        {
            DisposeLocationManager();
        }

        private void DisposeLocationManager()
        {
            if (_locationManager != null)
            {
                try { _locationManager.StopUpdatingLocation(); } catch { }
                try { _locationManager.Delegate = null!; } catch { }
                try { _locationManager.Dispose(); } catch { }
                _locationManager = null;
            }
            _locationDelegate?.Dispose();
            _locationDelegate = null;
        }

        private sealed class BackgroundLocationDelegate(GPSLocationService owner) : CLLocationManagerDelegate
        {
            private readonly GPSLocationService _owner = owner;

            public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
            {
                if (locations.Length == 0) return;

                foreach (var location in locations)
                {
                    var mauiLocation = new DeviceGeoLocation
                    {
                        Latitude = location.Coordinate.Latitude,
                        Longitude = location.Coordinate.Longitude,
                        Altitude = location.Altitude,
                        Speed = location.Speed >= 0 ? location.Speed : null,
                        Course = location.Course >= 0 ? location.Course : null,
                        Accuracy = location.HorizontalAccuracy >= 0 ? location.HorizontalAccuracy : null,
                        VerticalAccuracy = location.VerticalAccuracy >= 0 ? location.VerticalAccuracy : null,
                        Timestamp = (DateTime)location.Timestamp
                    };
                    _owner.RaiseLocationChanged(mauiLocation);
                }
            }

            public override void Failed(CLLocationManager manager, NSError error)
            {
                System.Diagnostics.Debug.WriteLine($"[GPSLocationService.iOS] CLLocationManager failed: {error.LocalizedDescription}");
                _owner.RaiseListeningFailed(GeolocationError.PositionUnavailable);
            }

            public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            {
                if (status == CLAuthorizationStatus.Denied || status == CLAuthorizationStatus.Restricted)
                {
                    _owner.RaiseListeningFailed(GeolocationError.Unauthorized);
                }
            }
        }
    }
}
