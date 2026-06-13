using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Util;
using AndroidX.Core.App;
using AndroidLocation = Android.Locations.Location;

namespace JustCompute.Platforms.Android.Services;

[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeLocation)]
public class LocationForegroundService : Service, ILocationListener
{
    private const string Tag = "CoordiLocationFgSvc";
    public const string ChannelId = "coordi.location.tracking";
    public const int NotificationId = 4242;
    public const string ActionStart = "coordi.action.START_LOCATION_TRACKING";
    public const string ActionStop = "coordi.action.STOP_LOCATION_TRACKING";

    private const long MinUpdateIntervalMs = 1000;
    private const float MinUpdateDistanceMeters = 0f;

    private LocationManager? _locationManager;
    private bool _tracking;

    public static event EventHandler<AndroidLocation>? LocationUpdated;
    public static event EventHandler<string>? LocationError;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        EnsureNotificationChannel();
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Call StartForeground unconditionally and as early as possible. The system kills the
        // process with ForegroundServiceDidNotStartInTimeException if startForeground() isn't
        // called within ~5 seconds of startForegroundService(), and Android requires it on every
        // OnStartCommand for a service started with startForegroundService() (including restart
        // re-deliveries where intent is null).
        try
        {
            var notification = BuildNotification();
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }
        }
        catch (Exception ex)
        {
            Log.Error(Tag, "StartForeground failed: " + ex);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (intent?.Action == ActionStop)
        {
            StopTracking();
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CA1422 // StopForeground(bool) is deprecated on 24+, only reached below 24
                StopForeground(removeNotification: true);
#pragma warning restore CA1422
            }
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (!_tracking)
        {
            StartTracking();
            _tracking = true;
        }

        return StartCommandResult.Sticky;
    }

    private void StartTracking()
    {
        _locationManager = (LocationManager?)GetSystemService(LocationService);
        if (_locationManager == null)
        {
            LocationError?.Invoke(this, "LocationManager unavailable");
            return;
        }

        var provider = SelectTrackingProvider(_locationManager);
        if (provider == null)
        {
            LocationError?.Invoke(this, "No enabled location providers");
            return;
        }

        try
        {
            _locationManager.RequestLocationUpdates(
                provider,
                MinUpdateIntervalMs,
                MinUpdateDistanceMeters,
                this,
                Looper.MainLooper);
        }
        catch (Java.Lang.SecurityException ex)
        {
            LocationError?.Invoke(this, ex.Message ?? "Location permission denied");
        }
    }

    // Track from a single provider, preferring GPS. The coarse network provider is only a
    // fallback when GPS is off: mixing the two makes the trajectory zig-zag and inflates distance.
    private static string? SelectTrackingProvider(LocationManager locationManager)
    {
        var enabledProviders = locationManager.GetProviders(enabledOnly: true);

        if (enabledProviders.Contains(LocationManager.GpsProvider))
        {
            return LocationManager.GpsProvider;
        }

        foreach (var provider in enabledProviders)
        {
            if (provider != LocationManager.PassiveProvider)
            {
                return provider;
            }
        }

        return null;
    }

    private void StopTracking()
    {
        try { _locationManager?.RemoveUpdates(this); }
        catch { }
        _locationManager = null;
        _tracking = false;
    }

    private void EnsureNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager == null) return;
        if (manager.GetNotificationChannel(ChannelId) != null) return;

        var channel = new NotificationChannel(
            ChannelId,
            "Trip tracking",
            NotificationImportance.Low)
        {
            Description = "Keeps speed and distance tracking running while the app is in the background."
        };
        channel.SetShowBadge(false);
        manager.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        PendingIntent? contentIntent = null;
        try
        {
            var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
            if (launchIntent != null)
            {
                launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
                // PendingIntentFlags.Immutable requires API 23+; below that, omit it.
                var pendingIntentFlags = OperatingSystem.IsAndroidVersionAtLeast(23)
                    ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
                    : PendingIntentFlags.UpdateCurrent;
                contentIntent = PendingIntent.GetActivity(
                    this,
                    0,
                    launchIntent,
                    pendingIntentFlags);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, "Failed to create launch PendingIntent: " + ex.Message);
        }

        // IcMenuMylocation is a system-provided drawable that always exists on every Android
        // version. Using a guaranteed resource avoids the timeout-exception failure mode where
        // a missing/zero app-icon resource id causes notification building to throw before
        // StartForeground() is reached.
        var smallIcon = global::Android.Resource.Drawable.IcMenuMyLocation;

        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetContentTitle("Coordi is tracking your trip");
        builder.SetContentText("Speed, distance, and elevation are being recorded.");
        builder.SetSmallIcon(smallIcon);
        builder.SetOngoing(true);
        builder.SetSilent(true);
        builder.SetCategory(NotificationCompat.CategoryService);
        builder.SetVisibility(NotificationCompat.VisibilityPublic);

        if (contentIntent != null)
        {
            builder.SetContentIntent(contentIntent);
        }

        return builder.Build()!;
    }

    public void OnLocationChanged(AndroidLocation location)
    {
        LocationUpdated?.Invoke(this, location);
    }

    public void OnProviderEnabled(string provider) { }

    public void OnProviderDisabled(string provider)
    {
        LocationError?.Invoke(this, $"Provider disabled: {provider}");
    }

    public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras) { }

    public override void OnDestroy()
    {
        StopTracking();
        base.OnDestroy();
    }
}
