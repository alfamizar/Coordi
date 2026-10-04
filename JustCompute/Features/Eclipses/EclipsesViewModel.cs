using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using Compute.Core.Domain.Entities.Models.Eclipses;
using JustCompute.Presentation.Collections;
using JustCompute.Resources.Strings;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using static JustCompute.Presentation.Collections.GroupingHelper;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Eclipses
{
    /// <summary>
    /// A century of eclipses, optionally narrowed to the ones visible from here: everything
    /// already over folded into one closed group at the top, and the rest grouped by year.
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
        /// The solar table is the slowest thing the app computes: about 0.3 s on a desktop, so
        /// one to three seconds on a phone (the lunar one is under a millisecond). The user is
        /// only narrowing a list they can already see.
        /// </summary>
        private List<TEclipse> _all = [];

        private Location? _computedLocation;

        private readonly IStringLocalizer<AppStringsRes> _localizer;

        /// <summary>
        /// Groups the reader has opened or closed, and which way. ApplyFilter rebuilds the groups
        /// from scratch, so without this every flip of the filter silently undid what they had
        /// folded away — or, for the past, shut it again while they were reading it.
        /// </summary>
        private readonly Dictionary<string, bool> _expandedByReader = [];

        /// <summary>
        /// Today at the place the table was computed for, which is the zone every eclipse date is
        /// in. Fixed for one build of the groups so a tap re-fills a group with the same members
        /// it was built with, even across midnight.
        /// </summary>
        private DateTime _today;

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

        protected EclipsesViewModel(ViewModelServices services, IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _localizer = localizer;
        }

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

            _today = TodayAtComputedPlace();
            var groups = GetGroupedData(_shown, GroupKeyOf).ToList();

            foreach (var group in groups)
            {
                if (group.Key == TimelineGroups.PastKey)
                {
                    // Counted before the group is closed and emptied: the number is what tells
                    // the reader there is something behind a header with nothing under it.
                    group.Title = string.Format(
                        CultureInfo.CurrentCulture, _localizer["PastEclipsesHeader"], group.Count);
                }

                if (!IsExpanded(group.Key))
                {
                    group.IsExpanded = false;
                    group.Clear();
                }
            }

            GroupedEclipseList = groups;

            HasNoLocation = _all.Count == 0;
            HasNoVisibleMatches = _all.Count > 0 && groups.Count == 0;
        }

        [RelayCommand]
        private void ToggleGroup(Group<string, TEclipse> group)
        {
            group.IsExpanded = !group.IsExpanded;
            _expandedByReader[group.Key] = group.IsExpanded;

            group.Clear();

            if (group.IsExpanded)
            {
                var key = group.Key;
                group.InsertRange([.. _shown.Where(e => GroupKeyOf(e) == key)]);
            }
        }

        /// <summary>The past starts closed and every year starts open, until the reader says otherwise.</summary>
        private bool IsExpanded(string key) =>
            _expandedByReader.TryGetValue(key, out var expanded) ? expanded : key != TimelineGroups.PastKey;

        private string GroupKeyOf(TEclipse eclipse) => TimelineGroups.KeyOf(eclipse.Date, _today);

        private DateTime TodayAtComputedPlace()
        {
            var utcNow = DateTime.UtcNow;
            return _computedLocation is { } place
                ? utcNow.AddHours(place.GetUtcOffsetHours(utcNow)).Date
                : utcNow.Date;
        }
    }
}
