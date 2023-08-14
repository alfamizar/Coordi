using CommunityToolkit.Mvvm.ComponentModel;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class TimeTravelViewModel : BaseViewModel
    {
        [ObservableProperty]
        DateTime dateNow;

        [ObservableProperty]
        Coordinate coordinate;

        [ObservableProperty]
        Location location;

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
            if (!_locationManager.GettingLocationFinished.Task.IsCompleted)
            {
                await _locationManager.GettingLocationFinished.Task;
            }
            Location = _locationManager.CurrentLocation;

            if (Location == null)
            {
                IsBusy = false;
                return;
            }

            await CalculateDateInfo(Location.Latitude, Location.Longitude);
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