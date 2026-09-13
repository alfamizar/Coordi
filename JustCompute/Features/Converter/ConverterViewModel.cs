using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Astro;
using JustCompute.Resources.Strings;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Converter
{
    /// <summary>
    /// One coordinate, written five ways — decimal, DMS, DDM, UTM and MGRS.
    ///
    /// Today already shows the grid references for the place the app is computing from; this is
    /// the other direction, for a coordinate that came from somewhere else. The parser takes any
    /// of the three angular forms, with a hemisphere letter or a sign, so a figure copied out of
    /// a map, a logbook or a message can go straight in without being rewritten first.
    /// </summary>
    public partial class ConverterViewModel : BaseViewModel, ICompute
    {
        /// <summary>
        /// The UTM band. Outside it the projection is not defined and the two grid rows say so
        /// rather than inventing a square: the poles are UPS territory, which this does not cover.
        /// </summary>
        private const double MinGridLatitude = -80.0;
        private const double MaxGridLatitude = 84.0;

        /// <summary>
        /// What the app last put in the fields. A location arriving after the screen opened — the
        /// device fix on a first run — may replace its own seed, but must never overwrite what
        /// someone has typed, and comparing the text is a surer test of that than a flag.
        /// </summary>
        private string _seededLatitude = string.Empty;
        private string _seededLongitude = string.Empty;

        private readonly IStringLocalizer<AppStringsRes> _localizer;

        public ConverterViewModel(ViewModelServices services, IStringLocalizer<AppStringsRes> localizer)
            : base(services)
        {
            _localizer = localizer;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLatitudeError))]
        private string latitudeText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasLongitudeError))]
        private string longitudeText = string.Empty;

        [ObservableProperty]
        private string decimalText = string.Empty;

        [ObservableProperty]
        private string dmsText = string.Empty;

        [ObservableProperty]
        private string ddmText = string.Empty;

        [ObservableProperty]
        private string utmText = string.Empty;

        [ObservableProperty]
        private string mgrsText = string.Empty;

        [ObservableProperty]
        private bool hasResults;

        /// <summary>
        /// Empty is not wrong, it is unfinished. Clearing the field to start again would
        /// otherwise paint itself red on the first keystroke removed.
        /// </summary>
        public bool HasLatitudeError => !string.IsNullOrWhiteSpace(LatitudeText) && ParseLatitude() is null;

        public bool HasLongitudeError => !string.IsNullOrWhiteSpace(LongitudeText) && ParseLongitude() is null;

        partial void OnLatitudeTextChanged(string value) => Recalculate();

        partial void OnLongitudeTextChanged(string value) => Recalculate();

        protected override Task GetData(Location location)
        {
            Seed(location);
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void UseMyLocation()
        {
            Location? here = _gpsLocationService.DeviceLocation ?? _gpsLocationService.SelectedLocation;
            if (here is null) return;

            // Unconditionally: asking for it is asking for it to replace whatever is there.
            LatitudeText = _seededLatitude = Plain(here.Latitude);
            LongitudeText = _seededLongitude = Plain(here.Longitude);
        }

        private void Seed(Location location)
        {
            if (!string.IsNullOrEmpty(LatitudeText) && LatitudeText != _seededLatitude) return;
            if (!string.IsNullOrEmpty(LongitudeText) && LongitudeText != _seededLongitude) return;

            LatitudeText = _seededLatitude = Plain(location.Latitude);
            LongitudeText = _seededLongitude = Plain(location.Longitude);
        }

        private double? ParseLatitude() => InRange(GeoFormat.ParseOrNull(LatitudeText), 90.0);

        private double? ParseLongitude() => InRange(GeoFormat.ParseOrNull(LongitudeText), 180.0);

        private static double? InRange(double? value, double limit) =>
            value is double v && Math.Abs(v) <= limit ? v : null;

        private void Recalculate()
        {
            if (ParseLatitude() is not double lat || ParseLongitude() is not double lon)
            {
                HasResults = false;
                return;
            }

            DecimalText = $"{GeoFormat.FormatDecimal(lat)}, {GeoFormat.FormatDecimal(lon)}";
            DmsText = $"{GeoFormat.FormatDms(lat, GeoFormat.Axis.Latitude)}   {GeoFormat.FormatDms(lon, GeoFormat.Axis.Longitude)}";
            DdmText = $"{GeoFormat.FormatDdm(lat, GeoFormat.Axis.Latitude)}   {GeoFormat.FormatDdm(lon, GeoFormat.Axis.Longitude)}";

            // Checked rather than caught: a latitude past the band is an ordinary thing for
            // someone to type, not an exceptional one, and GridReference throws on it.
            bool onTheGrid = lat is >= MinGridLatitude and <= MaxGridLatitude;
            string offGrid = _localizer["ConverterOutsideGridLabel"];

            UtmText = onTheGrid ? GridReference.ToUtm(lat, lon).Format() : offGrid;
            MgrsText = onTheGrid ? GridReference.ToMgrs(lat, lon).Format() : offGrid;

            HasResults = true;
        }

        /// <summary>
        /// Six decimals, about 11 cm, and no degree sign: this goes back into an editable field,
        /// where a decorated number is one more thing to delete before typing a new one.
        /// </summary>
        private static string Plain(double value) =>
            value.ToString("0.000000", CultureInfo.InvariantCulture);
    }
}
