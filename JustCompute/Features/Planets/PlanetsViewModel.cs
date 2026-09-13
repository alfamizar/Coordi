using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Resources.Strings;
using JustCompute.Shared.Helpers;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using AppSettings = JustCompute.Shared.Helpers.Settings;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Planets
{
    /// <summary>
    /// Where the seven other planets are from here, and the crossing each makes today.
    ///
    /// A reference table, not a recommendation: every planet is listed in order out from the Sun
    /// whether or not it is up, because which of them is worth the trouble tonight depends on the
    /// reader's sky, their glass and their patience, none of which this knows.
    ///
    /// The engine has had the ephemeris since the Compute.Astro port and nothing surfaced it.
    /// </summary>
    public partial class PlanetsViewModel : BaseViewModel, ICompute
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly LocationClock _clock;

        /// <summary>The place the table describes, kept so the clock tick can recompute against it.</summary>
        private Location? _location;
        private double _offsetHours;

        public PlanetsViewModel(ViewModelServices services, IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _localizer = localizer;
            _clock = new LocationClock(_ => Refresh());
        }

        /// <summary>
        /// Created once and updated in place. Replacing the collection on every tick would make
        /// the bindable layout rebuild all seven cards a second, which throws away the reader's
        /// scroll position — and this is a screen you scroll.
        /// </summary>
        public ObservableCollection<PlanetRow> Planets { get; } = [];

        protected override Task GetData(Location location)
        {
            _location = location;
            _offsetHours = location.GetUtcOffsetHours(DateTime.UtcNow);

            Refresh();

            // Azimuth moves about a degree every four minutes, so a table computed once and left
            // alone is quietly wrong by the time anyone has read it.
            _clock.Start(_offsetHours);
            return Task.CompletedTask;
        }

        protected override void ClearData()
        {
            _clock.Stop();
            _location = null;
            Planets.Clear();
        }

        public override Task OnPageDisappearingAsync()
        {
            _clock.Stop();
            return base.OnPageDisappearingAsync();
        }

        private void Refresh()
        {
            if (_location is null) return;

            var utcNow = DateTime.UtcNow;
            var snapshots = PlanetSnapshot.AllFor(
                _location.Latitude,
                _location.Longitude,
                utcNow,
                utcNow.AddHours(_offsetHours).Date,
                _offsetHours);

            // The order is fixed, so the rows line up with the snapshots by index and each one
            // is written over rather than replaced.
            while (Planets.Count < snapshots.Count)
            {
                Planets.Add(new PlanetRow());
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                Fill(Planets[i], snapshots[i]);
            }
        }

        private void Fill(PlanetRow row, PlanetSnapshot snapshot)
        {
            row.Name = _localizer[NameKey(snapshot.Planet)];
            row.Altitude = Degrees(snapshot.AltitudeDeg);
            row.Azimuth = Degrees(snapshot.AzimuthDeg);
            row.Rise = snapshot.Rise is DateTime rise ? TimeWithinDayOf(rise, snapshot.Transit) : string.Empty;
            row.Set = snapshot.Set is DateTime set ? TimeWithinDayOf(set, snapshot.Transit) : string.Empty;
            row.Highest = $"{Time(snapshot.Transit)} · {Degrees(snapshot.TransitAltitudeDeg)}";
            row.Distance = string.Format(
                CultureInfo.CurrentCulture,
                _localizer["SkyDistanceAuLabel"],
                snapshot.DistanceAu.ToString("0.00", CultureInfo.CurrentCulture));
            row.HasRiseAndSet = snapshot is { Rise: not null, Set: not null };
            row.AllDayNote = snapshot switch
            {
                { Circumpolar: true } => _localizer["SkyAlwaysUpLabel"],
                { NeverRises: true } => _localizer["SkyNeverUpLabel"],
                _ => string.Empty,
            };
            row.IsUp = snapshot.IsUp;
        }

        private static string NameKey(Planet planet) => $"Planet{planet}Label";

        /// <summary>
        /// Whole degrees with a real minus sign, not a hyphen: these sit in a column of numbers
        /// and the typographic one is what lines up and what a screen reader says correctly.
        /// </summary>
        private static string Degrees(double value) =>
            $"{Math.Round(value).ToString("0", CultureInfo.CurrentCulture).Replace("-", "−")}°";

        private static string Time(DateTime local) =>
            local.ToString(AppSettings.Is24HourTimeFormat ? "H:mm" : "h:mm tt", CultureInfo.CurrentCulture);

        /// <summary>
        /// The time, carrying its date when that is not the day of <paramref name="reference"/>.
        ///
        /// Saturn near opposition rises before midnight and sets the following morning, so the
        /// card would otherwise read "Rises 20:11, Sets 8:42" and leave the reader to work out
        /// which day each belongs to. The transit is always inside the day asked for — it is
        /// chosen as the crossing nearest local noon — so it is the thing to measure against.
        /// </summary>
        private static string TimeWithinDayOf(DateTime local, DateTime reference) =>
            local.Date == reference.Date
                ? Time(local)
                : $"{Time(local)}, {local.ToString("d MMM", CultureInfo.CurrentCulture)}";
    }
}
