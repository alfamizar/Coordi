using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Extensions;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Common.Sort;
using JustCompute.Shared.Popups;
using System.ComponentModel;
using Microsoft.Extensions.Localization;
using JustCompute.Resources.Strings;
using Compute.Core.Navigation;
using JustCompute.Features.InputLocation;

namespace JustCompute.Features.SearchByCity
{
    public partial class SearchByCityViewModel : BaseViewModel, IQueryParameter
    {
        private SearchLocationContext? _searchLocationContext;
        private List<Sorting> _sortingCriteria = [];
        private readonly ViewModelServices _services;
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private int _searchVersion;
        // ShowPopupAsync (CommunityToolkit.Maui v9+) drives the popup through Shell navigation,
        // so OnNavigatedFrom/To fire on this page when the popup opens and closes. This flag
        // tells those handlers to skip the clear/seed during that round-trip.
        private bool _isShowingPopup;

        [ObservableProperty]
        private List<Location> _locationsSearchResult = [];

        [ObservableProperty]
        private string? _searchTerm;

        [ObservableProperty]
        private Sorting _selectedSortCriterion = null!;

        public SearchByCityViewModel(
            ViewModelServices services,
            IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _services = services;
            _localizer = localizer;
            _selectedSortCriterion = new(SortCriterion.City, _localizer.GetString("CityLabel"));
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
                IsBusy = true;
                LocationsSearchResult = await SortLocationsInBackground(LocationsSearchResult);
                IsBusy = false;
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

            return [.. sortedLocations
                .ThenBy(location => location.City.CityName)
                .ThenBy(location => location.City.CountryName)
                .ThenBy(location => location.LatitudeDouble)
                .ThenBy(location => location.LongitudeDouble)];
        }

        private async Task OnShowSortingPopup(View? view)
        {
            var popupViewModel = new SortOptionsPopupViewModel(_services);
            popupViewModel.ApplyParameters(_sortingCriteria, SelectedSortCriterion, view);
            var popup = new SortOptionsPopup(popupViewModel);

            Page? currentPage = view?.Window?.Page
                ?? Shell.Current?.CurrentPage
                ?? Application.Current?.Windows.FirstOrDefault()?.Page;

            if (currentPage is null)
            {
                return;
            }

            var popupOptions = new PopupOptions
            {
                Shadow = null,
                Shape = null,
                CanBeDismissedByTappingOutsideOfPopup = true
            };

            _isShowingPopup = true;
            try
            {
                var popupResult = await currentPage.ShowPopupAsync<SortOptionsPopupResult>(
                    popup,
                    popupOptions,
                    CancellationToken.None);

                if (!popupResult.WasDismissedByTappingOutsideOfPopup)
                {
                    HandleSortingPopupResult(popupResult.Result);
                }
            }
            finally
            {
                _isShowingPopup = false;
            }
        }

        private void HandleSortingPopupResult(SortOptionsPopupResult? result)
        {
            if (result is not null)
            {
                SelectedSortCriterion = result.SelectedSortCriterion;
                _sortingCriteria = [.. result.SortingCriteria];
            }
        }

        public override async Task OnNavigatedToAsync()
        {
            await base.OnNavigatedToAsync();
            if (_isShowingPopup) return;
            await OnPerformSearchLocation(string.Empty);
        }

        public override void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            if (_isShowingPopup) return;
            SearchTerm = string.Empty;
            LocationsSearchResult = [];
        }

        private async Task OnPerformSearchLocation(string? searchTerm)
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);

            try
            {
                IsBusy = true;
                var searchQuery = searchTerm?.Trim().RemoveAccents() ?? string.Empty;
                var unsortedLocations = await _locationService.SearchLocations(searchQuery);
                var sortedLocations = await SortLocationsInBackground(unsortedLocations ?? []);
                if (searchVersion == _searchVersion)
                {
                    LocationsSearchResult = sortedLocations;
                }
            }
            finally
            {
                if (searchVersion == _searchVersion)
                {
                    IsBusy = false;
                }
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
