using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models.Speed;
using Compute.Core.Domain.Services;
using Compute.Core.UI;
using Compute.Core.Utils;
using CoordinateSharp;
using DotNext;
using JustCompute.Shared.ViewModels;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;

namespace JustCompute.Features.SpeedAndDistance
{
    public partial class SpeedAndDistanceViewModel : BaseViewModel
    {
        private const double MaxTrustedFixAccuracyMeters = 30;
        private const double MaxPlausibleGroundSpeedMps = 100;

        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly DistanceCalculator _distanceCalculator;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
        private Timer? _timer;
        private DateTime _lastUpdate = DateTime.MinValue;

        // The raw Speed / CalculatedSpeed values are kept in m/s (as reported by the GPS service and
        // the distance calculator); the formatted properties below convert them to the unit chosen
        // in Settings for display.
        private SpeedType _speedType;

        [ObservableProperty]
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
        private double _accuracy = 0;
        [ObservableProperty]
        private double _verticalAccuracy = 0;
        [ObservableProperty]
        private double _altitude = -1;
        [ObservableProperty]
        private double _elevation = 0;
        [ObservableProperty]
        private double travelledDistance = 0;
        [ObservableProperty]
        private double directDistance = 0;
        [ObservableProperty]
        private DateTime elapsedTime = DateTime.MinValue;

        public string FormattedSpeed => FormatSpeed(Speed);
        public string FormattedCalculatedSpeed => FormatSpeed(CalculatedSpeed);

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
            IStringLocalizer<AppStringsRes> localizer
            )
            : base(services)
        {
            _toastService = toastService;
            _localizer = localizer;
            _distanceCalculator = new();
            _speedType = global::JustCompute.Shared.Helpers.Settings.SpeedType;
            Commands.Add("ToggleLocationTrackingCommand", new AsyncRelayCommand(OnToggleLocationTracking));
        }

        private void StartTimer()
        {
            _timer?.Dispose();
            _timer = null;

            _timer = new Timer(
                (_) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ElapsedTime = ElapsedTime.AddSeconds(1);
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

        private async Task OnToggleLocationTracking()
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (IsBusy) return;

            IsBusy = true;
            var willBeRunning = !IsRunning;

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
            ElapsedTime = DateTime.MinValue;

            _distanceCalculator.StartAltitude = -1;
            _distanceCalculator.StartingPoint = null;
            _distanceCalculator.PreviousPoint = null;
            _distanceCalculator.LastPoint = null;
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

            var point = new Coordinate(update.Latitude, update.Longitude);

            _distanceCalculator.StartingPoint ??= point;
            if (_distanceCalculator.StartAltitude == -1)
            {
                _distanceCalculator.StartAltitude = update.Altitude ?? 0;
            }

            if (_distanceCalculator.LastPoint is { } previousPoint)
            {
                if (IsImplausibleJump(previousPoint, point, sinceLastUpdate))
                    return;

                _distanceCalculator.PreviousPoint = previousPoint;
            }

            _distanceCalculator.LastPoint = point;

            Elevation = Math.Round(_distanceCalculator.GetElevation(Altitude), 2);
            TravelledDistance = Math.Round(_distanceCalculator.GetCurvedDistance(TravelledDistance));
            DirectDistance = Math.Round(_distanceCalculator.GetDirectDistance());
            CalculatedSpeed = Math.Round(_distanceCalculator.GetSpeed(), 2);
        }

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

        private static bool IsImplausibleJump(Coordinate from, Coordinate to, TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds <= 0)
                return false;

            var metersMoved = new CoordinateSharp.Distance(from, to).Meters;
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

            if (IsRunning)
            {
                DeviceDisplay.Current.KeepScreenOn = true;
                return;
            }

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

        private async Task<Result<bool>> StartListeningLocation(bool backgroundCapable = false)
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

        private async Task<Result<bool>> StopListeningLocation()
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
