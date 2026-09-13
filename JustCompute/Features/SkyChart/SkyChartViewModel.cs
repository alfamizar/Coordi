using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Astro;
using JustCompute.Resources.Strings;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using AppSettings = JustCompute.Shared.Helpers.Settings;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.SkyChart
{
    /// <summary>
    /// The sky over the selected place, scrubbable by a day either side of now.
    ///
    /// Every figure on the chart already existed in Compute.Astro and none of it was on screen:
    /// the bright-star catalogue, the stick figures and the IAU boundaries had no reader at all.
    /// This is the screen that shows the engine doing what it can do.
    /// </summary>
    public partial class SkyChartViewModel : BaseViewModel, ICompute
    {
        /// <summary>
        /// How far the slider reaches either way. A day covers tonight and last night, which is
        /// the span anyone actually asks about; further than that and dragging a phone-width
        /// slider stops being able to land on an hour.
        /// </summary>
        private const double RangeHours = 24.0;

        private readonly IStringLocalizer<AppStringsRes> _localizer;

        /// <summary>UTC at the moment the screen settled on "now", so the slider has a fixed anchor.</summary>
        private DateTime _anchorUtc = DateTime.UtcNow;
        private double _offsetHours;

        public SkyChartViewModel(ViewModelServices services, IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _localizer = localizer;
        }

        /// <summary>The compass letters, in the order the chart draws them: N, E, S, W.</summary>
        public string[] Cardinals =>
        [
            _localizer["CompassNorthLabel"],
            _localizer["CompassEastLabel"],
            _localizer["CompassSouthLabel"],
            _localizer["CompassWestLabel"],
        ];

        /// <summary>The planet names the chart labels its markers with, outward from the Sun.</summary>
        public string[] PlanetNames =>
        [
            _localizer["PlanetMercuryLabel"],
            _localizer["PlanetVenusLabel"],
            _localizer["PlanetMarsLabel"],
            _localizer["PlanetJupiterLabel"],
            _localizer["PlanetSaturnLabel"],
            _localizer["PlanetUranusLabel"],
            _localizer["PlanetNeptuneLabel"],
        ];

        [ObservableProperty]
        private double latitude;

        [ObservableProperty]
        private double longitude;

        [ObservableProperty]
        private double jdUtc = AstroTime.J2000;

        /// <summary>Hours from the anchor, which is what the slider is bound to.</summary>
        [ObservableProperty]
        private double hoursFromNow;

        [ObservableProperty]
        private string timeLabel = string.Empty;

        [ObservableProperty]
        private bool showStickFigures = true;

        [ObservableProperty]
        private bool showBorders;

        public double MinimumHours => -RangeHours;

        public double MaximumHours => RangeHours;

        protected override Task GetData(Location location)
        {
            Latitude = location.Latitude;
            Longitude = location.Longitude;

            // Re-anchored on every load, so coming back to the screen shows the sky as it is
            // rather than as it was when it was last opened.
            _anchorUtc = DateTime.UtcNow;
            _offsetHours = location.GetUtcOffsetHours(_anchorUtc);
            HoursFromNow = OpeningOffsetHours();

            Apply();
            return Task.CompletedTask;
        }

        partial void OnHoursFromNowChanged(double value) => Apply();

        /// <summary>
        /// Normally zero — the chart opens on now. The screenshot harness can ask for a
        /// particular local hour instead, because a capture that happens to run at five in the
        /// afternoon produces a blue disc with no stars on it, which is the one thing this
        /// screen exists to show. Compiled out of Release with the rest of the harness.
        /// </summary>
        private double OpeningOffsetHours()
        {
#if DEBUG
            if (global::JustCompute.Shared.Helpers.ScreenshotHarness.SeededSkyHour is int hour)
            {
                // Hours to the next time the local clock reads that hour, inside the slider's
                // own range so the thumb lands somewhere the reader could have dragged it.
                var localNow = _anchorUtc.AddHours(_offsetHours);
                var wanted = localNow.Date.AddHours(hour);
                if (wanted <= localNow) wanted = wanted.AddDays(1);

                return Math.Clamp((wanted - localNow).TotalHours, -RangeHours, RangeHours);
            }
#endif
            return 0.0;
        }

        [RelayCommand]
        private void Now() => HoursFromNow = 0.0;

        private void Apply()
        {
            var utc = _anchorUtc.AddHours(HoursFromNow);
            JdUtc = AstroTime.JulianDay(DateTime.SpecifyKind(utc, DateTimeKind.Utc));

            // The observer's own clock: a chart of their sky answered in anyone else's time
            // would be a puzzle rather than an answer.
            var local = utc.AddHours(_offsetHours);
            TimeLabel = local.ToString(
                AppSettings.Is24HourTimeFormat ? "d MMM, H:mm" : "d MMM, h:mm tt",
                System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
