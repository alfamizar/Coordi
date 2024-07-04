using CommunityToolkit.Mvvm.ComponentModel;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;

namespace JustCompute.Presentation.ViewModels
{
    public partial class TimeTravelViewModel : BaseViewModel
    {
        [ObservableProperty]
        private DateTime dateNow = DateTime.Now;

        [ObservableProperty]
        private Coordinate coordinate = new();

        public TimeTravelViewModel()
        {
            Commands.Add("InitDateCommand", new Command(InitDate));
        }

        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
        }

        private async void InitDate()
        {
            if (_locationManager.GettingDeviceLocationFinished?.Task != null && !_locationManager.GettingDeviceLocationFinished.Task.IsCompleted)
            {
                await _locationManager.GettingDeviceLocationFinished.Task.ConfigureAwait(false);
            }

            var location = _locationManager.SelectedLocation;
            if (location == null)
            {
                IsBusy = false;
                return;
            }

            await CalculateDateInfo(location.LatitudeDouble, location.LongitudeDouble);
        }

        private async Task CalculateDateInfo(double lat, double lng)
        {
            IsBusy = true;

            await Task.Run(() =>
            {
                TimeZoneInfo timeZoneInfo = TimeZoneInfo.Local;
                var offset = timeZoneInfo.GetUtcOffset(DateTime.Now).Hours;
                Coordinate = new(lat, lng, DateNow);
                Coordinate.Offset += offset;
            });

            IsBusy = false;
        }
    }
}