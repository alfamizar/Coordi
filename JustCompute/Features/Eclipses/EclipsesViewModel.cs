using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models.Eclipses;
using JustCompute.Shared.ViewModels;
using static Compute.Core.Helpers.GroupingHelper;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Eclipses
{
    /// <summary>
    /// A century of eclipses, grouped by year, optionally narrowed to the ones visible from here.
    ///
    /// The Sun and Moon screens differ in exactly two things: the kind of eclipse they list and
    /// the service that computes it. Everything else — the cache, the filter, the year grouping,
    /// which years the reader has folded away — was duplicated line for line in both, which meant
    /// every fix had to be made twice and the pair drifted whenever one was missed.
    /// </summary>
    public abstract partial class EclipsesViewModel<TEclipse> : BaseViewModel, ICompute
        where TEclipse : IEclipseInfo
    {
        [ObservableProperty]
        private List<Group<string, TEclipse>> groupedEclipseList = [];

        /// <summary>What the list currently shows: the whole catalogue, or only the visible ones.</summary>
        private List<TEclipse> _shown = [];

        /// <summary>
        /// The whole century, kept so flipping the filter re-slices what is already computed.
        /// Recomputing the table costs a few hundred milliseconds; the user is only narrowing
        /// a list they can already see.
        /// </summary>
        private List<TEclipse> _all = [];

        private Location? _computedLocation;

        /// <summary>
        /// Years the reader has collapsed. ApplyFilter rebuilds the groups from scratch, so
        /// without this every flip of the filter silently re-expanded whatever they had folded away.
        /// </summary>
        private readonly HashSet<string> _collapsedYears = [];

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

        /// <summary>
        /// Nothing was computed at all — no location, so nothing can be said about eclipses.
        /// </summary>
        [ObservableProperty]
        private bool hasNoLocation = true;

        /// <summary>
        /// The century was computed, and the filter hid every one of them. A different thing
        /// from having no location, and it used to be reported as "cannot get current location" —
        /// telling the reader their GPS had failed when in fact the answer was simply "none".
        /// </summary>
        [ObservableProperty]
        private bool hasNoVisibleMatches;

        protected EclipsesViewModel(ViewModelServices services) : base(services) { }

        /// <summary>The century of eclipses for this place, in its own time zone.</summary>
        protected abstract Task<List<TEclipse>> ComputeEclipsesAsync(Location location, DateTime utcNow);

        protected override async Task GetData(Location location)
        {
            // Sun and Moon are two view models sharing one preference, and a field initialiser
            // reads it only once — whichever tab was built first kept its answer and the two
            // disagreed. Re-read on every load so both tabs show the same filter.
            ShowOnlyVisible = global::JustCompute.Shared.Helpers.Settings.ShowOnlyVisibleEclipses;

            if (_all.Count > 0 && IsSamePlaceAs(location))
            {
                ApplyFilter();
                return;
            }

            _all = await ComputeEclipsesAsync(location, DateTime.UtcNow);
            _computedLocation = location.Clone();

            ApplyFilter();
        }

        /// <summary>
        /// The zone counts as much as the coordinates: the same place on a different zone shows
        /// every contact time an hour out.
        /// </summary>
        private bool IsSamePlaceAs(Location location) =>
            _computedLocation is { } computed
            && computed.Latitude == location.Latitude
            && computed.Longitude == location.Longitude
            && computed.TimeZoneId == location.TimeZoneId;

        partial void OnShowOnlyVisibleChanged(bool value)
        {
            global::JustCompute.Shared.Helpers.Settings.ShowOnlyVisibleEclipses = value;
            ApplyFilter();
        }

        /// <summary>Narrows the catalogue already in hand — no recomputation.</summary>
        private void ApplyFilter()
        {
            _shown = ShowOnlyVisible ? [.. _all.Where(e => e.IsVisible)] : _all;

            FilterSummary = _all.Count == 0 ? string.Empty : $"{_shown.Count} / {_all.Count}";

            var groups = GetGroupedData(_shown, YearOf).ToList();

            foreach (var group in groups.Where(g => _collapsedYears.Contains(g.Key)))
            {
                group.IsExpanded = false;
                group.Clear();
            }

            GroupedEclipseList = groups;

            HasNoLocation = _all.Count == 0;
            HasNoVisibleMatches = _all.Count > 0 && groups.Count == 0;
        }

        protected override void ClearData()
        {
            _computedLocation = null;
            _shown = [];
            _all = [];
            FilterSummary = string.Empty;
            GroupedEclipseList = [];
        }

        [RelayCommand]
        private void ToggleGroup(Group<string, TEclipse> group)
        {
            group.IsExpanded = !group.IsExpanded;

            if (group.IsExpanded) _collapsedYears.Remove(group.Key);
            else _collapsedYears.Add(group.Key);

            group.Clear();

            if (group.IsExpanded)
            {
                var year = group.Key;
                group.InsertRange([.. _shown.Where(e => YearOf(e) == year)]);
            }
        }

        private static string YearOf(TEclipse eclipse) => eclipse.Date.ToString("yyyy");
    }
}
