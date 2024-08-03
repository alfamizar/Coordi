using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Extensions;
using JustCompute.Presentation.ViewModels.Base;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Common.Sort;
using JustCompute.Presentation.Popups.ViewModels;
using System.ComponentModel;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Compute.Core.Navigation;
using JustCompute.Presentation.ViewModels.Common;

namespace JustCompute.Presentation.ViewModels
{
    public partial class SearchByCityViewModel : BaseViewModel, IQueryParameter
    {
        private SearchLocationContext? _searchLocationContext;
        private List<Sorting> _sortingCriteria = [];
        private readonly IPopupService _popupService;
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        [ObservableProperty]
        private List<Location> _locationsSearchResult = [];

        [ObservableProperty]
        private string? _searchTerm;

        [ObservableProperty]
        private Sorting _selectedSortCriterion = new(SortCriterion.City, _localizer.GetString("CityLabel"));

        public SearchByCityViewModel(IPopupService popupService)
        {
            _popupService = popupService;
            InitializeCommands();
            InitializeSortingCriteria();
            PropertyChanged += HandlePropertyChanged;
        }

        private void InitializeCommands()
        {
            Commands.Add("PerformSearchLocationCommand", new AsyncRelayCommand<string>(OnPerformSearchLocation));
            Commands.Add("LocationSelectedCommand", new Command<Location>(OnLocationSelected));
            Commands.Add("ShowSortingPopupCommand", new AsyncRelayCommand<View>(OnShowSortingPopup));
            Commands.Add("GoBackCommand", new Command(() => OnBackButtonPressed()));
        }

        private void InitializeSortingCriteria()
        {
            _sortingCriteria =
            [
                new Sorting(SortCriterion.City, _localizer.GetString("CityLabel")),
                new Sorting(SortCriterion.Country, _localizer.GetString("CountryLabel")),
                new Sorting(SortCriterion.Population, _localizer.GetString("PopulationLabel"))
            ];
        }

        private async void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedSortCriterion))
            {
                LocationsSearchResult = await SortLocationsInBackground(LocationsSearchResult);
            }
        }

        private async Task<List<Location>> SortLocationsInBackground(List<Location> locations)
        {
            return await Task.Run(() => SortLocations(locations));
        }

        private List<Location> SortLocations(List<Location> locations)
        {
            Func<Location, IComparable> keySelector = SelectedSortCriterion.Criterion switch
            {
                SortCriterion.City => location => location.City.CityName,
                SortCriterion.Country => location => location.City.CountryName,
                SortCriterion.Population => location => location.City.Population,
                _ => throw new ArgumentOutOfRangeException($"Unexpected sort criterion: '{SelectedSortCriterion.Criterion}'")
            };

            var sortedLocations = SelectedSortCriterion.Direction == SortDirection.Ascending
                ? locations.OrderBy(keySelector)
                : locations.OrderByDescending(keySelector);

            return [.. sortedLocations];
        }

        private async Task OnShowSortingPopup(View? view)
        {
            var result = await _popupService.ShowPopupAsync<SortOptionsPopupViewModel>(
                onPresenting: viewModel => viewModel.ApplyParameters(_sortingCriteria, SelectedSortCriterion, view));

            HandleSortingPopupResult(result);
        }

        private void HandleSortingPopupResult(object? result)
        {
            if (result is Tuple<Sorting, List<Sorting>> resultTuple)
            {
                SelectedSortCriterion = resultTuple.Item1;
                _sortingCriteria = resultTuple.Item2;
            }
        }

        public override async void OnPageAppearing()
        {
            if (LocationsSearchResult.Count == 0)
            {
                await OnPerformSearchLocation(string.Empty);
            };
        }

        public override void OnPageDisappearing()
        {
            SearchTerm = string.Empty;
            base.OnPageDisappearing();
        }

        private async Task OnPerformSearchLocation(string? searchTerm)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var searchQuery = searchTerm?.RemoveAccents() ?? string.Empty;
                var unsortedLocations = await _locationService.SearchLocations(searchQuery);
                LocationsSearchResult = await SortLocationsInBackground(unsortedLocations ?? []);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnLocationSelected(Location selectedLocation)
        {
            if (_searchLocationContext == SearchLocationContext.ReturnResult)
            {
                _navigationService.NavigateBackAsync(selectedLocation);
                _searchLocationContext = null;
            }
            else
            {
                Dictionary<LocationInputContext, Location> locationAndContext = [];
                locationAndContext[LocationInputContext.Add] = selectedLocation;
                _navigationService.NavigateToAsync<InputLocationViewModel>(locationAndContext);
            }
        }

        public override bool OnBackButtonPressed()
        {
            _searchLocationContext = null;
            _navigationService.NavigateBackAsync();
            return true;
        }

        public void ApplyQueryParameter(object? parameter)
        {
            if (parameter is SearchLocationContext searchLocationContext)
            {
                _searchLocationContext = searchLocationContext;
            }
        }
    }
}
