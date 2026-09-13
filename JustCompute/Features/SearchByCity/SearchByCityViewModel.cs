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
using JustCompute.Shared.Abstractions.Navigation;
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

        /// <summary>
        /// The last search could not be run. Without this a failure was indistinguishable from
        /// "no matches" and from "still loading" — all three drew an empty page.
        /// </summary>
        [ObservableProperty]
        private bool _hasSearchFailed;

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
                // async void: anything escaping here takes the process down, and a throw part-way
                // would strand the busy indicator on screen.
                try
                {
                    IsBusy = true;
                    LocationsSearchResult = await SortLocationsInBackground(LocationsSearchResult);
                }
                finally
                {
                    IsBusy = false;
                }
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
                .ThenBy(location => location.Latitude)
                .ThenBy(location => location.Longitude)];
        }

        [RelayCommand]
        private async Task ShowSortingPopup(View? view)
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

            // Leaving the screen no longer throws the results away, so coming back is instant and
            // a refresh that fails can never blank a list the user was already reading. Only seed
            // when there is genuinely nothing to show.
            if (LocationsSearchResult.Count > 0) return;

            await PerformSearchLocation(SearchTerm ?? string.Empty);
        }

        [RelayCommand]
        private void GoBack() => OnBackButtonPressed();

        [RelayCommand]
        private Task RetrySearch() => PerformSearchLocation(SearchTerm ?? string.Empty);

        [RelayCommand]
        private async Task PerformSearchLocation(string? searchTerm)
        {
            var searchVersion = Interlocked.Increment(ref _searchVersion);

            try
            {
                IsBusy = true;
                if (searchVersion == _searchVersion)
                {
                    HasSearchFailed = false;
                }

                var searchQuery = searchTerm?.Trim().RemoveAccents() ?? string.Empty;
                var unsortedLocations = await _locationService.SearchLocations(searchQuery);
                var sortedLocations = await SortLocationsInBackground(unsortedLocations ?? []);
                if (searchVersion == _searchVersion)
                {
                    LocationsSearchResult = sortedLocations;
                }
            }
            catch (Exception)
            {
                // The database can refuse a read while another screen is using it. Say so and
                // offer a retry rather than leaving an empty page and a swallowed log line.
                if (searchVersion == _searchVersion)
                {
                    HasSearchFailed = true;
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

        [RelayCommand]
        private void LocationSelected(Location selectedLocation)
        {
            if (_searchLocationContext == SearchLocationContext.ReturnResult)
            {
                _navigationService.NavigateBackAsync(selectedLocation);
                _searchLocationContext = null;
            }
            else
            {
                _navigationService.NavigateToAsync<InputLocationViewModel>(
                    new LocationEditorArgs(LocationInputContext.Add, selectedLocation));
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
