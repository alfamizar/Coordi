using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Navigation;
using CoordinateSharp;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using System.ComponentModel;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class DistanceViewModel : BaseViewModel, IResultHandler
    {
        private LocationInitialization? currentInitialization;

        [ObservableProperty]
        private Location location1 = new();

        [ObservableProperty]
        private Location location2 = new();

        [ObservableProperty]
        private Distance? distance;

        public DistanceViewModel()
        {
            Commands.Add("SearchLocation1Command", new Command(SearchLocation1));
            Commands.Add("SearchLocation2Command", new Command(SearchLocation2));
            Commands.Add("InitLocation1Command", new Command(InitLocation1));
            Commands.Add("InitLocation2Command", new Command(InitLocation2));
        }

        partial void OnLocation1Changed(Location value)
        {
            InitDistance();
        }

        partial void OnLocation2Changed(Location value)
        {
            InitDistance();
        }

        private void InitDistance()
        {
            var point1 = new Coordinate(Location1.LatitudeDouble, Location1.LongitudeDouble);
            var point2 = new Coordinate(Location2.LatitudeDouble, Location2.LongitudeDouble);
            Distance = new Distance(point1, point2, Shape.Ellipsoid);
        }

        private void InitLocation1()
        {
            Location1 = new Location()
            {
                Latitude = Location1.LatitudeDouble.ToString(),
                Longitude = Location1.LongitudeDouble.ToString(),
            };
        }

        private void InitLocation2()
        {
            Location2 = new Location()
            {
                Latitude = Location2.LatitudeDouble.ToString(),
                Longitude = Location2.LongitudeDouble.ToString(),
            };
        }

        private async void SearchLocation1()
        {
            currentInitialization = LocationInitialization.Point1;
            await NavigateToSearchViewModel();
        }

        private async void SearchLocation2()
        {
            currentInitialization = LocationInitialization.Point2;
            await NavigateToSearchViewModel();
        }

        private async Task NavigateToSearchViewModel()
        {
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(currentInitialization);
        }

        public void ApplyResult(object? result)
        {
            if (result is Location location && currentInitialization.HasValue)
            {
                switch (currentInitialization.Value)
                {
                    case LocationInitialization.Point1:
                        Location1 = location;
                        break;
                    case LocationInitialization.Point2:
                        Location2 = location;
                        break;
                }
            }
            currentInitialization = null;
        }
    }
}