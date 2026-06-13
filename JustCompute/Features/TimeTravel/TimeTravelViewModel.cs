using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoordinateSharp;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.TimeTravel
{
    public partial class TimeTravelViewModel : BaseViewModel
    {
        [ObservableProperty]
        private DateTime dateNow = DateTime.Now;

        [ObservableProperty]
        private Coordinate coordinate = new();

        public TimeTravelViewModel(ViewModelServices services)
            : base(services)
        {
            Commands.Add("InitDateCommand", new AsyncRelayCommand(InitDate));
        }

        public override Task OnNavigatedToAsync()
        {
            return base.OnNavigatedToAsync();
        }

        private async Task InitDate()
        {
            if (_gpsLocationService.GettingDeviceLocationFinished?.Task != null && !_gpsLocationService.GettingDeviceLocationFinished.Task.IsCompleted)
            {
                await _gpsLocationService.GettingDeviceLocationFinished.Task.ConfigureAwait(false);
            }

            var location = _gpsLocationService.SelectedLocation;
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
