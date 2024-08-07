using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.UI;
using Compute.Core.Utils;
using CoordinateSharp;
using DotNext;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SpeedAndDistanceViewModel : BaseViewModel
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly DistanceCalculator _distanceCalculator;
        private Timer? _timer;

        [ObservableProperty]
        private bool _isRunning;
        [ObservableProperty]
        private double _speed = 0;
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

        public SpeedAndDistanceViewModel(
            IToastService toastService,
            IStringLocalizer<AppStringsRes> localizer
            )
        {
            _toastService = toastService;
            _localizer = localizer;
            _distanceCalculator = new();
            Commands.Add("ToggleLocationTrackingCommand", new AsyncRelayCommand(OnToggleLocationTracking));
        }

        private void StartTimer()
        {
            _timer?.Dispose();
            _timer = null;

            _timer = new Timer(
                (_) => { ElapsedTime = ElapsedTime.AddSeconds(1); },
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
            await StopListeningLocation();
            var startedListeningLocationResult = await StartListeningLocation();
            IsBusy = false;
            if (!startedListeningLocationResult.IsSuccessful)
            {
                IsRunning = false;
                StopTimer();
                return;
            }

            IsRunning = !IsRunning;
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

        private void OnDeviceLocationChangedCallback(object? sender, GeolocationLocationChangedEventArgs e)
        {
            var location = e.Location;
            _distanceCalculator.StartingPoint ??= new Coordinate(location.Latitude, location.Longitude);

            if (_distanceCalculator.LastPoint != null)
            {
                _distanceCalculator.PreviousPoint = new Coordinate(_distanceCalculator.LastPoint.Latitude.ToDouble(), _distanceCalculator.LastPoint.Longitude.ToDouble());
            }
            _distanceCalculator.LastPoint = new Coordinate(location.Latitude, location.Longitude);

            if (_distanceCalculator.StartAltitude == -1)
            {
                _distanceCalculator.StartAltitude = location.Altitude ?? 0;
            }

            Speed = Math.Round(location.Speed ?? 0, 2);
            Direction = Math.Round(location.Course ?? 0, 2);
            Accuracy = Math.Round(location.Accuracy ?? 0, 2);
            VerticalAccuracy = Math.Round(location.VerticalAccuracy ?? 0, 2);
            Altitude = Math.Round(location.Altitude ?? 0, 2);

            if (IsRunning)
            {
                Elevation = Math.Round(_distanceCalculator.GetElevation(Altitude), 2);
                TravelledDistance = Math.Round(_distanceCalculator.GetCurvedDistance(TravelledDistance));
                DirectDistance = Math.Round(_distanceCalculator.GetDirectDistance());
            }
        }

        private async void OnListeningDeviceLocationFailedCallback(object? sender, GeolocationListeningFailedEventArgs e)
        {
            await _toastService.ShowToast(_localizer.GetString("ListeningLocationFailedToastMessage"));
        }

        public override async void OnNavigatedTo()
        {
            var startedListeningLocationResult = await StartListeningLocation();
            if (startedListeningLocationResult.IsSuccessful)
            {
                DeviceDisplay.Current.KeepScreenOn = true;
            }
        }

        public override async void OnPageDisappearing()
        {
            DeviceDisplay.Current.KeepScreenOn = false;
            await StopListeningLocation();
        }

        private async Task<Result<bool>> StartListeningLocation()
        {
            var result = await _gpsLocationService
                .OnStartListeningDeciveGeoLocation<GeolocationLocationChangedEventArgs, GeolocationListeningFailedEventArgs>(
                    OnDeviceLocationChangedCallback,
                    OnListeningDeviceLocationFailedCallback);
            if (!result.IsSuccessful)
            {
                await _toastService.ShowToast(_localizer.GetString("CannotStartListeningLocationToastMessage"));
            }
            return result;
        }

        private async Task<Result<bool>> StopListeningLocation()
        {
            var result = await Task.FromResult(_gpsLocationService
                .OnStopListeningDeciveGeoLocation<GeolocationLocationChangedEventArgs, GeolocationListeningFailedEventArgs>(
                    OnDeviceLocationChangedCallback,
                    OnListeningDeviceLocationFailedCallback));
            if (!result.IsSuccessful)
            {
                await _toastService.ShowToast(_localizer.GetString("CannotStopListeningLocationToastMessage"));
            }
            return result;
        }
    }
}
