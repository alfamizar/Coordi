using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Common.Messaging;
using JustCompute.Features.InputLocation;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.ViewModels.Messages;
using System.Collections.ObjectModel;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.SavedLocations
{
    public partial class SavedLocationsViewModel : BaseViewModel
    {
        private readonly IMessagingService _messagingService;

        [ObservableProperty]
        private ObservableCollection<Location> savedLocations = [];

        [ObservableProperty]
        private int locationsCount;

        public SavedLocationsViewModel(ViewModelServices services, IMessagingService messagingService)
            : base(services)
        {
            _messagingService = messagingService;
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
            Commands.Add("DeleteLocationCommand", new AsyncRelayCommand<Location>(OnDeleteLocation));
            Commands.Add("GoBackCommand", new Command(() => OnBackButtonPressed()));
        }

        private async Task OnDeleteLocation(Location? location)
        {
            if (location is null)
            {
                return;
            }

            await _locationService.DeleteLocation(location);
            SavedLocations.Remove(location);
            _messagingService.Send(new LocationMessage(location, LocationInputContext.Delete));
        }

        private void OnEditLocation(Location location)
        {
            Dictionary<LocationInputContext, Location> locationAndContext = [];
            locationAndContext[LocationInputContext.Edit] = location;
            _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
        }

        public override async Task OnPageAppearingAsync()
        {
            await base.OnPageAppearingAsync();
            await GetSavedLocations();
        }

        private async Task GetSavedLocations()
        {
            var savedLocations = await _locationService.GetSavedLocations();
            var newLocations = savedLocations
                    .Where(newLoc => !SavedLocations.Any(existingLoc => existingLoc.Id == newLoc.Id))
                    .ToList();

            newLocations.ForEach(SavedLocations.Add);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.NavigateBackAsync();
            return true;
        }
    }   
}
