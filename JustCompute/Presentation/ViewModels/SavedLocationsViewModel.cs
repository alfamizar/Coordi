using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JustCompute.Presentation.ViewModels.Base;
using JustCompute.Presentation.ViewModels.Common;
using JustCompute.Presentation.ViewModels.Messages;
using System.Collections.ObjectModel;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SavedLocationsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private ObservableCollection<Location> savedLocations = [];

        [ObservableProperty]
        private int locationsCount;

        public SavedLocationsViewModel()
        {
            InitializeCommands();
            SavedLocations.CollectionChanged += SavedLocations_CollectionChanged; ;
        }

        private void SavedLocations_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LocationsCount = SavedLocations.Count;
        }

        private void InitializeCommands()
        {
            Commands.Add("EditLocationCommand", new Command<Location>(OnEditLocation));
            Commands.Add("DeleteLocationCommand", new Command<Location>(async (location) => await OnDeleteLocation(location)));
            Commands.Add("GoBackCommand", new Command(() => OnBackButtonPressed()));
        }

        private async Task OnDeleteLocation(Location location)
        {
            await _locationManager.DeleteLocation(location);
            SavedLocations.Remove(location);
            WeakReferenceMessenger.Default.Send(new LocationMessage(location, LocationInputContext.Delete));
        }

        private void OnEditLocation(Location location)
        {
            Dictionary<LocationInputContext, Location> locationAndContext = [];
            locationAndContext[LocationInputContext.Edit] = location;
            _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        public override async void OnPageAppearing()
        {
            base.OnPageAppearing();
            await GetSavedLocations();
        }

        private async Task GetSavedLocations()
        {
            var savedLocations = await _locationManager.GetSavedLocations();
            savedLocations.ForEach(SavedLocations.Add);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.NavigateBackAsync();
            return true;
        }
    }   
}