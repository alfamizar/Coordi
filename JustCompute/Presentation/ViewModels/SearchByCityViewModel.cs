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

namespace JustCompute.Presentation.ViewModels
{
    public partial class SearchByCityViewModel : BaseViewModel
    {
        private List<Sorting> _sortingCriteria;
        private readonly IPopupService _popupService;
        private static readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        [ObservableProperty]
        private List<Location>? locationsSearchResult;

        [ObservableProperty]
        private string? searchTerm;

        [ObservableProperty]
        private Sorting defaultSortCriterion = new(SortCriterion.City, _localizer.GetString("CityLabel"));

        public SearchByCityViewModel(IPopupService popupService)
        {
            _popupService = popupService;

            Commands.Add("PerformSearchLocationCommand", new AsyncRelayCommand<string>(OnPerformSearchLocation));
            Commands.Add("LocationSelectedCommand", new Command<Location>(OnLocationSelected));
            Commands.Add("ShowSortingPopupCommand", new AsyncRelayCommand<View>(OnShowSortingPopup));

            _sortingCriteria =
        [
            new Sorting(SortCriterion.City, _localizer.GetString("CityLabel")),
            new Sorting(SortCriterion.Country, _localizer.GetString("CountryLabel")),
            new Sorting(SortCriterion.Population, _localizer.GetString("PopulationLabel"))
        ];

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DefaultSortCriterion))
            {
                LocationsSearchResult = Sort(LocationsSearchResult);
            }
        }

        private List<Location>? Sort(List<Location>? locations)
        {
            Func<Location, IComparable> keySelector = DefaultSortCriterion.Criterion switch
            {
                SortCriterion.City => location => location.City.CityName,
                SortCriterion.Country => location => location.City.CountryName,
                SortCriterion.Population => location => location.City.Population,
                _ => throw new Exception($"Not existing enum value '{DefaultSortCriterion.Criterion}' for {nameof(DefaultSortCriterion.Criterion)}")
            };

            IOrderedEnumerable<Location>? sortedItems =
                DefaultSortCriterion.Direction == SortDirection.Ascending
                    ? locations?.OrderBy(keySelector)
                    : locations?.OrderByDescending(keySelector);

            return sortedItems?.ToList();
        }

        private async Task OnShowSortingPopup(View? view)
        {
            var result = await _popupService.ShowPopupAsync<SortOptionsPopupViewModel>(
                onPresenting: viewModel => viewModel.ApplyParameters(_sortingCriteria, DefaultSortCriterion, view));

            HandlePopupResult(result);
        }

        private void HandlePopupResult(object? result)
        {
            if (result is Tuple<Sorting, List<Sorting>> resultTuple)
            {
                DefaultSortCriterion = resultTuple.Item1;
                _sortingCriteria = resultTuple.Item2;
            }
        }

        public override async void OnPageAppearing()
        {
            base.OnPageAppearing();
            if (SearchTerm.IsNotNullOrEmpty())
            {
                LocationsSearchResult = null;
                SearchTerm = string.Empty;
                return;
            }
            await OnPerformSearchLocation(string.Empty);
        }

        private async Task OnPerformSearchLocation(string? searchTerm)
        {
            if (IsBusy) return;

            IsBusy = true;
            var unsortedLocations = await _locationManager.GetLocationsByCity(searchTerm?.RemoveAccents() ?? string.Empty);
            LocationsSearchResult = Sort(unsortedLocations);
            IsBusy = false;
        }

        private void OnLocationSelected(Location selectedLocation)
        {
            _navigationService.NavigateToAsync<InputLocationViewModel>(selectedLocation);
        }

        public override bool OnBackButtonPressed()
        {
            _navigationService.NavigateBackAsync();
            return true;
        }
    }
}