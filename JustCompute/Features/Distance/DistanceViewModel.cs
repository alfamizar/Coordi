using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Navigation;
using Compute.Astro;
using JustCompute.Features.SearchByCity;
using JustCompute.Shared.ViewModels;
using System.ComponentModel;
using DistanceModel = Compute.Core.Domain.Entities.Models.Distance.Distance;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Distance
{
    public partial class DistanceViewModel : BaseViewModel, IResultHandler
    {
        private LocationInitialization? currentInitialization;

        [ObservableProperty]
        private Location location1 = new();

        [ObservableProperty]
        private Location location2 = new();

        [ObservableProperty]
        private DistanceModel? distance;

        public DistanceViewModel(ViewModelServices services)
            : base(services)
        {
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
            var meters = Geodesy.DistanceMeters(
                Location1.Latitude, Location1.Longitude,
                Location2.Latitude, Location2.Longitude);
            Distance = new DistanceModel(meters / 1000.0);
        }

        [RelayCommand]
        private async Task SearchLocation1()
        {
            currentInitialization = LocationInitialization.Point1;
            await NavigateToSearchViewModel();
        }

        [RelayCommand]
        private async Task SearchLocation2()
        {
            currentInitialization = LocationInitialization.Point2;
            await NavigateToSearchViewModel();
        }

        private async Task NavigateToSearchViewModel()
        {
            var searchLocationContext = SearchLocationContext.ReturnResult;
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(searchLocationContext);
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

        public override Task OnNavigatedToAsync()
        {
            InitDistance();
            return Task.CompletedTask;
        }
    }
}
