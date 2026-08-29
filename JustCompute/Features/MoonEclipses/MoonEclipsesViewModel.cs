using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Services.Moon;
using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Shared.ViewModels;
using System.Linq;
using static Compute.Core.Helpers.GroupingHelper;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.MoonEclipses
{
    public partial class MoonEclipsesViewModel : BaseViewModel, ICompute
    {
        private readonly IMoonService _moonService;

        [ObservableProperty]
        private List<Group<string, LunarEclipseInfo>> groupedEclipseList;

        List<LunarEclipseInfo> eclipseList = [];

        /// <summary>
        /// The whole century, kept so flipping the filter re-slices what is already computed.
        /// Recomputing the table costs a few hundred milliseconds; the user is only narrowing
        /// a list they can already see.
        /// </summary>
        private List<LunarEclipseInfo> _allEclipses = [];

        /// <summary>
        /// Lives on this screen rather than in Settings: it is a property of the list you are
        /// looking at, and you want to flip it while looking at it.
        /// </summary>
        [ObservableProperty]
        private bool showOnlyVisible = global::JustCompute.Shared.Helpers.Settings.ShowOnlyVisibleEclipses;

        /// <summary>"12 / 231" — makes the filter's effect legible instead of leaving the user
        /// to wonder how much the list is hiding.</summary>
        [ObservableProperty]
        private string filterSummary = string.Empty;

        public MoonEclipsesViewModel(ViewModelServices services, IMoonService moonService)
            : base(services)
        {
            _moonService = moonService;
            GroupedEclipseList = [];
        }

        private Location? _computedLocation;

        protected override async Task GetData(Location location)
        {
            // Sun and Moon are two singleton view models sharing one preference, and a field
            // initialiser reads it only once — whichever tab was built first kept its answer and
            // the two disagreed. Re-read on every load so both tabs show the same filter.
            ShowOnlyVisible = global::JustCompute.Shared.Helpers.Settings.ShowOnlyVisibleEclipses;

            if (_allEclipses.Count > 0 &&
                _computedLocation != null &&
                _computedLocation.Latitude == location.Latitude &&
                _computedLocation.Longitude == location.Longitude &&
                _computedLocation.TimeZoneId == location.TimeZoneId)
            {
                ApplyFilter();
                return;
            }

            // Contact times come back in the location's own zone, matching the rest of the app.
            _allEclipses = await _moonService.GetMoonEclipsesAsync(location, DateTime.UtcNow);
            _computedLocation = location.Clone();

            ApplyFilter();
        }

        partial void OnShowOnlyVisibleChanged(bool value)
        {
            global::JustCompute.Shared.Helpers.Settings.ShowOnlyVisibleEclipses = value;
            ApplyFilter();
        }

        /// <summary>Narrows the catalogue already in hand — no recomputation.</summary>
        private void ApplyFilter()
        {
            eclipseList = ShowOnlyVisible
                ? [.. _allEclipses.Where(e => e.IsVisible)]
                : _allEclipses;

            FilterSummary = _allEclipses.Count == 0
                ? string.Empty
                : $"{eclipseList.Count} / {_allEclipses.Count}";

            GroupedEclipseList = GetGroupedData(eclipseList, item => item.Date.ToString("yyyy")).ToList();
        }

        protected override void ClearData()
        {
            _computedLocation = null;
            eclipseList = [];
            _allEclipses = [];
            FilterSummary = string.Empty;
            GroupedEclipseList = [];
        }

        [RelayCommand]
        private void ToggleGroup(Group<string, LunarEclipseInfo> group)
        {
            group.IsExpanded = !group.IsExpanded;

            var items = group.ToList();
            group.Clear();

            if (group.IsExpanded)
            {
                var groupKey = group.Key;
                group.InsertRange(
                    eclipseList
                    .Where(x => x.Date
                    .ToString("yyyy") == groupKey)
                    .ToList());
            }
        }
    }
}
