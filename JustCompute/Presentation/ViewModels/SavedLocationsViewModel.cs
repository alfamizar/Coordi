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

        public SavedLocationsViewModel()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            Commands.Add("EditLocationCommand", new Command<Location>(OnEditLocationClicked));
            Commands.Add("DeleteLocationCommand", new Command<Location>(async (location) => await OnDeleteLocationClicked(location)));
            Commands.Add("GoBackCommand", new Command(() => OnBackButtonPressed()));
        }

        private async Task OnDeleteLocationClicked(Location location)
        {
            await _locationManager.DeleteLocation(location);
            SavedLocations.Remove(location);
            WeakReferenceMessenger.Default.Send(new LocationMessage(location, LocationInputContext.Delete));
        }

        private void OnEditLocationClicked(Location location)
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
            SavedLocations = new ObservableCollection<Location>(savedLocations);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.NavigateBackAsync();
            return true;
        }
    }   
}