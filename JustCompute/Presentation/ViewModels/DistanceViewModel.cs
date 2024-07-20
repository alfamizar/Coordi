using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models;
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
            SubscribeToLocationChanges();
        }

        private void SubscribeToLocationChanges()
        {
            Location1.PropertyChanged += Location1_PropertyChanged;
            Location2.PropertyChanged += Location2_PropertyChanged;
        }

        private void UnsubscribeFromLocationChanges()
        {
            Location1.PropertyChanged -= Location1_PropertyChanged;
            Location2.PropertyChanged -= Location2_PropertyChanged;
        }

        private void Location1_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location.Latitude) || e.PropertyName == nameof(Location.Longitude))
            {
                Location1.City = new City();
                InitDistance();
            }
        }

        private void Location2_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location.Latitude) || e.PropertyName == nameof(Location.Longitude))
            {
                Location2.City = new City();
                InitDistance();
            }
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
                UnsubscribeFromLocationChanges();
                if (currentInitialization.Value == LocationInitialization.Point1)
                {
                    Location1 = location;
                }
                else if (currentInitialization.Value == LocationInitialization.Point2)
                {
                    Location2 = location;
                }
                SubscribeToLocationChanges();
            }
            currentInitialization = null;
        }
    }
}