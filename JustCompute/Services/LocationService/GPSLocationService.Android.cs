using Android.Content;
using Compute.Core.Domain.Errors;
using DotNext;
using JustCompute.Platforms.Android.Services;
using AndroidApp = Android.App.Application;
using AndroidLocation = Android.Locations.Location;
using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace JustCompute.Services.LocationService
{
    public partial class GPSLocationService
    {
        private bool _serviceSubscribed;

        private partial async Task<Result<bool, FaultCode>> StartPlatformListening()
        {
            try
            {
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (permissionStatus != PermissionStatus.Granted)
                {
                    return new Result<bool, FaultCode>(FaultCode.PermissionException);
                }

                var context = AndroidApp.Context;

                if (!_serviceSubscribed)
                {
                    LocationForegroundService.LocationUpdated += OnAndroidLocationUpdated;
                    LocationForegroundService.LocationError += OnAndroidLocationError;
                    _serviceSubscribed = true;
                }

                var intent = new Intent(context, typeof(LocationForegroundService));
                intent.SetAction(LocationForegroundService.ActionStart);

                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    context.StartForegroundService(intent);
                }
                else
                {
                    context.StartService(intent);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPSLocationService.Android] StartPlatformListening failed: {ex}");
                UnsubscribeFromService();
                return new Result<bool, FaultCode>(FaultCode.CouldNotStartListeningDeciveGeoLocation);
            }
        }

        private partial void StopPlatformListening()
        {
            try
            {
                var context = AndroidApp.Context;
                var intent = new Intent(context, typeof(LocationForegroundService));
                intent.SetAction(LocationForegroundService.ActionStop);
                context.StartService(intent);
            }
            catch { }
            finally
            {
                UnsubscribeFromService();
            }
        }

        private void UnsubscribeFromService()
        {
            if (!_serviceSubscribed) return;
            LocationForegroundService.LocationUpdated -= OnAndroidLocationUpdated;
            LocationForegroundService.LocationError -= OnAndroidLocationError;
            _serviceSubscribed = false;
        }

        private void OnAndroidLocationUpdated(object? sender, AndroidLocation location)
        {
            var mauiLocation = new DeviceGeoLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Altitude = location.HasAltitude ? location.Altitude : null,
                Speed = location.HasSpeed ? location.Speed : null,
                Course = location.HasBearing ? location.Bearing : null,
                Accuracy = location.HasAccuracy ? location.Accuracy : null,
                VerticalAccuracy = OperatingSystem.IsAndroidVersionAtLeast(26) && location.HasVerticalAccuracy ? location.VerticalAccuracyMeters : null,
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time)
            };
            RaiseLocationChanged(mauiLocation);
        }

        private void OnAndroidLocationError(object? sender, string reason)
        {
            RaiseListeningFailed(GeolocationError.PositionUnavailable);
        }
    }
}
