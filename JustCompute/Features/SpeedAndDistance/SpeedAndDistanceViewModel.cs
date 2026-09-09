using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models.Speed;
using Compute.Core.Domain.Services;
using JustCompute.Shared.Abstractions.UI;
using Compute.Core.Utils;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Common.Results;
using Compute.Core.Domain.Errors;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.Helpers;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Globalization;
using Compute.Astro;

namespace JustCompute.Features.SpeedAndDistance
{
    public partial class SpeedAndDistanceViewModel : BaseViewModel
    {
        private const double MaxTrustedFixAccuracyMeters = 30;
        private const double MaxPlausibleGroundSpeedMps = 100;

        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly DistanceCalculator _distanceCalculator;
        private readonly DistanceFormatter _distanceFormatter;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
        private Timer? _timer;
        private DateTime _lastUpdate = DateTime.MinValue;

        // The raw Speed / CalculatedSpeed values are kept in m/s (as reported by the GPS service and
        // the distance calculator); the formatted properties below convert them to the unit chosen
        // in Settings for display.
        private SpeedType _speedType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TrackingActionLabel))]
        private bool _isRunning;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedSpeed))]
        private double _speed = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedCalculatedSpeed))]
        private double _calculatedSpeed = 0;
        [ObservableProperty]
        private double _direction = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedAccuracy))]
        private double _accuracy = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedVerticalAccuracy))]
        private double _verticalAccuracy = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedAltitude))]
        private double _altitude = -1;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedElevation))]
        private double _elevation = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedTravelledDistance))]
        [NotifyPropertyChangedFor(nameof(HasTrip))]
        private double travelledDistance = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedDirectDistance))]
        private double directDistance = 0;
        /// <summary>
        /// How long the trip has been running.
        ///
        /// Measured from a monotonic clock rather than accumulated one tick at a time: counting
        /// AddSeconds(1) per timer callback drifts whenever the system throttles the timer — a
        /// backgrounded phone can miss many — so a long trip would read short by however much
        /// the device decided to sleep.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FormattedElapsedTime))]
        private TimeSpan elapsedTime = TimeSpan.Zero;

        /// <summary>
        /// hh:mm:ss, counting hours past 24 rather than wrapping. A DateTime formatted "HH:mm:ss"
        /// silently restarted at zero on the second day of a trip, and TimeSpan's own "hh" does
        /// the same because it is the hours *component* with the days held separately.
        /// </summary>
        public string FormattedElapsedTime =>
            $"{(int)ElapsedTime.TotalHours:00}:{ElapsedTime.Minutes:00}:{ElapsedTime.Seconds:00}";

        /// <summary>Where the running trip started, on the monotonic clock.</summary>
        private long _tripStartedTicks;

        /// <summary>
        /// What the button next to it will do. Sat in the XAML as the literal words "Start" and
        /// "Stop" — the most prominent control on the screen, in English, in an app that ships
        /// sixteen other languages.
        /// </summary>
        public string TrackingActionLabel =>
            _localizer.GetString(IsRunning ? "StopTrackingLabel" : "StartTrackingLabel");

        public string FormattedSpeed => FormatSpeed(Speed);
        public string FormattedCalculatedSpeed => FormatSpeed(CalculatedSpeed);

        // Every distance on this screen used to be printed through a "{0} m" resource string, so a
        // 42 km drive read "42000 m" and someone who had chosen miles was shown metres regardless.
        // The trip totals take the chosen unit; the accuracies and heights stay short (see
        // DistanceFormatter.FormatShort), because "0.01 km" of GPS accuracy helps nobody.
        public string FormattedTravelledDistance => _distanceFormatter.Format(TravelledDistance);

        /// <summary>
        /// The trip summary, in the shape the Ruler uses for a route: what you covered, the
        /// straight line between the ends, and how much further the first is than the second.
        /// The readouts above it are instantaneous; this is the trip as a whole, which is the
        /// thing a person actually wants when they stop.
        /// </summary>
        public bool HasTrip => TravelledDistance > 0;

        /// <summary>Direct distance carrying the bearing, because the straight line back to the
        /// start is the one you would point at.</summary>
        [ObservableProperty]
        private string directSummary = string.Empty;

        [ObservableProperty]
        private string detourRatio = string.Empty;

        [ObservableProperty]
        private bool hasDetour;
        public string FormattedDirectDistance => _distanceFormatter.Format(DirectDistance);
        public string FormattedAccuracy => _distanceFormatter.FormatShort(Accuracy);
        public string FormattedVerticalAccuracy => _distanceFormatter.FormatShort(VerticalAccuracy);
        public string FormattedAltitude => _distanceFormatter.FormatShort(Altitude);
        public string FormattedElevation => _distanceFormatter.FormatShort(Elevation);

        private string FormatSpeed(double metersPerSecond)
        {
            var (value, unitKey) = _speedType switch
            {
                SpeedType.KilometersPerHour => (metersPerSecond * 3.6, "KilometersPerHourLabel"),
                SpeedType.MilesPerHour => (metersPerSecond * 2.2369362921, "MilesPerHourLabel"),
                _ => (metersPerSecond, "MetersPerSecondLabel")
            };
            return $"{Math.Round(value, 2)} {_localizer.GetString(unitKey)}";
        }

        public SpeedAndDistanceViewModel(
            ViewModelServices services,
            IToastService toastService,
            IStringLocalizer<AppStringsRes> localizer,
            DistanceFormatter distanceFormatter
            )
            : base(services)
        {
            _toastService = toastService;
            _localizer = localizer;
            _distanceFormatter = distanceFormatter;
            _distanceCalculator = new();
            _speedType = global::JustCompute.Shared.Helpers.Settings.SpeedType;
        }

        private void StartTimer()
        {
            _timer?.Dispose();
            _timer = null;

            // Anchor to the monotonic clock; the tick only decides how often to re-read it.
            _tripStartedTicks = Stopwatch.GetTimestamp();
            TimeSpan alreadyElapsed = ElapsedTime;

            _timer = new Timer(
                (_) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ElapsedTime = alreadyElapsed + Stopwatch.GetElapsedTime(_tripStartedTicks);
                    });
                },
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            );
        }

        private void StopTimer()
        {
            _timer?.Dispose();
            _timer = null;
        }

        [RelayCommand]
        private async Task ToggleLocationTracking()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (IsBusy) return;

            var willBeRunning = !IsRunning;

            // Prominent disclosure (Google Play location policy): before the app starts collecting
            // location in the background for trip tracking, the user must see a clear explanation and
            // explicitly agree. Shown once; declining cancels the start so no background collection
            // happens without consent.
            if (willBeRunning && !global::JustCompute.Shared.Helpers.Settings.HasAcceptedBackgroundLocationDisclosure)
            {
                var consent = await _dialogService.DisplayAlert(
                    _localizer.GetString("BackgroundLocationDisclosureTitle"),
                    _localizer.GetString("BackgroundLocationDisclosureMessage"),
                    _localizer.GetString("BackgroundLocationDisclosureContinue"),
                    _localizer.GetString("BackgroundLocationDisclosureCancel"));

                if (consent != DialogButton.Positive)
                {
                    return;
                }

                global::JustCompute.Shared.Helpers.Settings.HasAcceptedBackgroundLocationDisclosure = true;
            }

            IsBusy = true;

            await StopListeningLocation();
            var startedListeningLocationResult = await StartListeningLocation(backgroundCapable: willBeRunning);
            IsBusy = false;

            if (!startedListeningLocationResult.IsSuccessful)
            {
                IsRunning = false;
                StopTimer();
                return;
            }

            IsRunning = willBeRunning;
            if (IsRunning)
            {
                ResetState();
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }

        private void ResetState()
        {
            Speed = 0;
            Direction = 0;
            Accuracy = 0;
            VerticalAccuracy = 0;
            Altitude = -1;
            Elevation = 0;
            TravelledDistance = 0;
            DirectDistance = 0;
            DirectSummary = string.Empty;
            DetourRatio = string.Empty;
            HasDetour = false;
            ElapsedTime = TimeSpan.Zero;

            _distanceCalculator.Reset();
        }

        private void OnDeviceLocationUpdated(object? sender, DeviceLocationUpdate update)
        {
            var now = DateTime.Now;
            if (now - _lastUpdate < _updateInterval)
                return;

            var sinceLastUpdate = now - _lastUpdate;
            _lastUpdate = now;

            UpdateLiveReadouts(update);

            if (!IsRunning || !IsTrustedFix(update))
                return;

            var point = new GeoPoint(update.Latitude, update.Longitude);
            var fixTime = update.Timestamp.UtcDateTime;

            // Only once, and only from a fix that actually carried an altitude. Defaulting a
            // missing one to 0 made every later reading an elevation measured from sea level.
            _distanceCalculator.StartAltitude ??= update.Altitude;

            if (_distanceCalculator.LastPoint is { } previousPoint)
            {
                // Judge the jump against the gap between the fixes themselves; the gap between
                // callbacks can be stretched by anything happening on the UI thread.
                var sinceLastFix = _distanceCalculator.LastFixTimestampUtc is { } lastFix
                    ? fixTime - lastFix
                    : sinceLastUpdate;

                if (IsImplausibleJump(previousPoint, point, sinceLastFix))
                    return;
            }

            _distanceCalculator.AddFix(point, fixTime);

            // The fix's own altitude, not the rounded display value, and still nullable so a
            // fix without one leaves the elevation alone instead of inventing a drop.
            Elevation = Math.Round(_distanceCalculator.GetElevation(update.Altitude), 2);
            TravelledDistance = Math.Round(_distanceCalculator.GetCurvedDistance(TravelledDistance));
            DirectDistance = Math.Round(_distanceCalculator.GetDirectDistance());
            UpdateTripSummary();
            CalculatedSpeed = Math.Round(_distanceCalculator.GetSpeed(), 2);
        }

        /// <summary>
        /// Recomputed from the path so far rather than accumulated, so it stays correct after
        /// fixes are discarded for poor accuracy or as implausible jumps.
        /// </summary>
        private void UpdateTripSummary()
        {
            DirectSummary = _distanceCalculator.StartingPoint is { } start
                            && _distanceCalculator.LastPoint is { } last
                            && DirectDistance > 0
                ? $"{_distanceFormatter.Format(DirectDistance)}   ·   {Bearing(start, last)}°"
                : _distanceFormatter.Format(DirectDistance);

            // A ratio of 1x says nothing - it only means the path was straight. Show it once the
            // route has actually wandered, so the row appears when it has something to report.
            HasDetour = DirectDistance > 0 && TravelledDistance > DirectDistance * 1.01;
            DetourRatio = HasDetour
                ? (TravelledDistance / DirectDistance).ToString("0.##", CultureInfo.CurrentCulture) + "\u00D7"
                : string.Empty;
        }

        /// <summary>Initial bearing of the straight line from the start to where you are now.</summary>
        private static int Bearing(GeoPoint from, GeoPoint to) =>
            (int)Math.Round(Geodesy.Inverse(from.Latitude, from.Longitude, to.Latitude, to.Longitude)
                .InitialBearingDeg) % 360;

        private void UpdateLiveReadouts(DeviceLocationUpdate update)
        {
            Speed = Math.Round(update.Speed ?? 0, 2);
            Direction = Math.Round(update.Course ?? 0, 2);
            Accuracy = Math.Round(update.Accuracy ?? 0, 2);
            VerticalAccuracy = Math.Round(update.VerticalAccuracy ?? 0, 2);
            Altitude = Math.Round(update.Altitude ?? 0, 2);
        }

        private static bool IsTrustedFix(DeviceLocationUpdate update)
        {
            var accuracy = update.Accuracy ?? 0;
            return accuracy > 0 && accuracy <= MaxTrustedFixAccuracyMeters;
        }

        private static bool IsImplausibleJump(GeoPoint from, GeoPoint to, TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds <= 0)
                return false;

            var metersMoved = from.DistanceMetersTo(to);
            return metersMoved / elapsed.TotalSeconds > MaxPlausibleGroundSpeedMps;
        }

        private async void OnDeviceLocationListeningFailed(object? sender, DeviceLocationListeningFailure failure)
        {
            await _toastService.ShowToast(_localizer.GetString("ListeningLocationFailedToastMessage"));
        }

        public override async Task OnNavigatedToAsync()
        {
            // Pick up any change made on the Settings screen while we were away.
            _speedType = global::JustCompute.Shared.Helpers.Settings.SpeedType;
            OnPropertyChanged(nameof(FormattedSpeed));
            OnPropertyChanged(nameof(FormattedCalculatedSpeed));

            // The distance unit is read at format time rather than cached, so the formatted
            // values only need telling that they are stale.
            OnPropertyChanged(nameof(FormattedTravelledDistance));
            OnPropertyChanged(nameof(FormattedDirectDistance));
            OnPropertyChanged(nameof(FormattedAccuracy));
            OnPropertyChanged(nameof(FormattedVerticalAccuracy));
            OnPropertyChanged(nameof(FormattedAltitude));
            OnPropertyChanged(nameof(FormattedElevation));

            if (IsRunning)
            {
                DeviceDisplay.Current.KeepScreenOn = true;
                return;
            }

#if DEBUG
            // A finished trip for the store screenshots. The summary only exists while tracking,
            // which a deep link cannot produce, and an empty screen shows none of what the card
            // is for. Debug-only, and it never runs unless the harness was given a trip.
            if (global::JustCompute.Shared.Helpers.ScreenshotHarness.SeededTrip is { } demo)
            {
                TravelledDistance = demo.Travelled;
                DirectDistance = demo.Direct;
                ElapsedTime = TimeSpan.FromMinutes(37);

                // A trip in progress, with readouts a real fix would carry. All zeros and the
                // -1 altitude placeholder are what an untouched screen shows, and they make the
                // screen look broken rather than idle.
                IsRunning = true;
                Speed = 4.1;
                CalculatedSpeed = demo.Travelled / (37 * 60);
                Direction = demo.Bearing;
                Accuracy = 4;
                VerticalAccuracy = 3;
                Altitude = 78;
                Elevation = 24;
                DirectSummary = $"{_distanceFormatter.Format(demo.Direct)}   \u00B7   {demo.Bearing}\u00B0";
                HasDetour = demo.Travelled > demo.Direct * 1.01;
                DetourRatio = HasDetour
                    ? (demo.Travelled / demo.Direct).ToString("0.##", CultureInfo.CurrentCulture) + "\u00D7"
                    : string.Empty;
                return;
            }
#endif

            var startedListeningLocationResult = await StartListeningLocation();
            if (startedListeningLocationResult.IsSuccessful)
            {
                DeviceDisplay.Current.KeepScreenOn = true;
            }
        }

        public override async Task OnPageDisappearingAsync()
        {
            if (IsRunning) return;

            DeviceDisplay.Current.KeepScreenOn = false;
            await StopListeningLocation();
        }

        private async Task<Result<bool, FaultCode>> StartListeningLocation(bool backgroundCapable = false)
        {
            _gpsLocationService.DeviceLocationUpdated += OnDeviceLocationUpdated;
            _gpsLocationService.DeviceLocationListeningFailed += OnDeviceLocationListeningFailed;

            var result = await _gpsLocationService.StartListeningForDeviceGeoLocation(backgroundCapable);
            if (!result.IsSuccessful)
            {
                _gpsLocationService.DeviceLocationUpdated -= OnDeviceLocationUpdated;
                _gpsLocationService.DeviceLocationListeningFailed -= OnDeviceLocationListeningFailed;
                await _toastService.ShowToast(_localizer.GetString("CannotStartListeningLocationToastMessage"));
            }
            return result;
        }

        private async Task<Result<bool, FaultCode>> StopListeningLocation()
        {
            _gpsLocationService.DeviceLocationUpdated -= OnDeviceLocationUpdated;
            _gpsLocationService.DeviceLocationListeningFailed -= OnDeviceLocationListeningFailed;

            var result = _gpsLocationService.StopListeningForDeviceLocation();
            if (!result.IsSuccessful)
            {
                await _toastService.ShowToast(_localizer.GetString("CannotStopListeningLocationToastMessage"));
            }
            return result;
        }
    }
}
