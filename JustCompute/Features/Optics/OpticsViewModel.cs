using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Optics;
using JustCompute.Shared.ViewModels;
using AppSettings = JustCompute.Shared.Helpers.Settings;
using OpticsMath = Compute.Core.Utils.Optics;

namespace JustCompute.Features.Optics
{
    /// <summary>
    /// Depth of field, field of view and star-trail limits for a lens and body — the shot-planning
    /// calculator ported from AstroClaw. Pure local state: no location, no network, so it works
    /// identically everywhere and the defaults (50 mm, f/8, 5 m, full frame) show a meaningful
    /// answer the moment the screen opens.
    /// </summary>
    public partial class OpticsViewModel : BaseViewModel
    {
        /// <summary>A 24 MP full-frame body, so the star figures stay sensible while the field is empty.</summary>
        private const int DefaultImageWidthPixels = 6000;

        /// <summary>
        /// Suppresses the write-back while the constructor seeds the fields. Without it, merely
        /// opening the screen would persist the defaults over what the user had kept.
        /// </summary>
        private readonly bool _loaded;

        public OpticsViewModel(ViewModelServices services) : base(services)
        {
            var remembered = AppSettings.RememberOpticsInputs;

            focalLengthMm = remembered ? AppSettings.OpticsFocalLengthMm : "50";
            aperture = remembered ? AppSettings.OpticsAperture : "8";
            subjectDistanceMeters = remembered ? AppSettings.OpticsSubjectDistanceMeters : "5";
            imageWidthPixels = remembered ? AppSettings.OpticsImageWidthPixels : "6000";
            declinationDeg = remembered ? AppSettings.OpticsDeclinationDeg : "0";
            rememberInputs = remembered;

            var storedFormat = remembered ? AppSettings.OpticsSensorFormat : null;
            selectedFormat = OpticsMath.SensorFormat.All.FirstOrDefault(f => f.Label == storedFormat)
                             ?? OpticsMath.SensorFormat.FullFrame;

            _loaded = true;
            Recalculate();
        }

        public IReadOnlyList<OpticsMath.SensorFormat> Formats { get; } = OpticsMath.SensorFormat.All;

        public ObservableCollection<CameraBody> Bodies { get; } = [.. CameraBodies.All];

        [ObservableProperty]
        private string focalLengthMm;

        [ObservableProperty]
        private string aperture;

        [ObservableProperty]
        private string subjectDistanceMeters;

        [ObservableProperty]
        private string imageWidthPixels;

        [ObservableProperty]
        private string declinationDeg;

        [ObservableProperty]
        private OpticsMath.SensorFormat selectedFormat;

        /// <summary>
        /// The body chosen from the picker. Setting it writes both of the numbers it stands for —
        /// the picker is a shortcut into the fields, not a mode, and everything stays editable.
        /// </summary>
        [ObservableProperty]
        private CameraBody? selectedBody;

        [ObservableProperty]
        private bool rememberInputs;

        [ObservableProperty]
        private bool hasFocalLengthError;

        [ObservableProperty]
        private bool hasApertureError;

        [ObservableProperty]
        private bool hasSubjectDistanceError;

        [ObservableProperty]
        private bool hasImageWidthError;

        [ObservableProperty]
        private bool hasDeclinationError;

        /// <summary>Null until every distance input parses — the results are hidden, not stale.</summary>
        [ObservableProperty]
        private OpticsMath.DepthOfField? depthOfField;

        [ObservableProperty]
        private OpticsMath.FieldOfView? fieldOfView;

        [ObservableProperty]
        private OpticsMath.StarExposure? starExposure;

        /// <summary>
        /// The name to show for the current numbers: the body that was picked while they still
        /// agree with it, otherwise the one those numbers can only be, otherwise nothing.
        ///
        /// Two rules, because neither alone is honest. The numbers alone cannot tell an α7 III
        /// from an EOS R8 — both are full frame at 6000 px, and so is the opening default, so
        /// naming the first match greets everybody with a camera they do not own. Remembering the
        /// tap alone is worse the other way: edit the pixel field afterwards and the name stays,
        /// attached to a pitch no longer in use.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBodyNameVisible))]
        private string bodyName = string.Empty;

        /// <summary>
        /// Only worth saying when the picker is not already saying it: someone who typed 7728 by
        /// hand gets told they are holding an X-T5, while someone who picked the X-T5 does not
        /// need it repeated back under the control they just used.
        /// </summary>
        public bool IsBodyNameVisible =>
            !string.IsNullOrEmpty(BodyName) && BodyName != SelectedBody?.Label;

        /// <summary>True once every distance input parses, i.e. there is something to show.</summary>
        [ObservableProperty]
        private bool hasResults;

        [ObservableProperty] private string hyperfocalText = string.Empty;
        [ObservableProperty] private string nearLimitText = string.Empty;
        [ObservableProperty] private string farLimitText = string.Empty;
        [ObservableProperty] private string totalText = string.Empty;
        [ObservableProperty] private string inFrontText = string.Empty;
        [ObservableProperty] private string behindText = string.Empty;

        [ObservableProperty] private string horizontalText = string.Empty;
        [ObservableProperty] private string verticalText = string.Empty;
        [ObservableProperty] private string diagonalText = string.Empty;

        [ObservableProperty] private string rule500Text = string.Empty;
        [ObservableProperty] private string npfText = string.Empty;
        [ObservableProperty] private string pixelPitchText = string.Empty;

        partial void OnFocalLengthMmChanged(string value) => Recalculate();
        partial void OnApertureChanged(string value) => Recalculate();
        partial void OnSubjectDistanceMetersChanged(string value) => Recalculate();
        partial void OnImageWidthPixelsChanged(string value) => Recalculate();
        partial void OnDeclinationDegChanged(string value) => Recalculate();
        partial void OnSelectedFormatChanged(OpticsMath.SensorFormat value) => Recalculate();

        partial void OnSelectedBodyChanged(CameraBody? value)
        {
            OnPropertyChanged(nameof(IsBodyNameVisible));

            if (value is null) return;

            // Picking a body is picking both of the numbers it stands for.
            SelectedFormat = value.Format;
            ImageWidthPixels = value.ImageWidthPixels.ToString(CultureInfo.InvariantCulture);
        }

        partial void OnRememberInputsChanged(bool value)
        {
            AppSettings.RememberOpticsInputs = value;

            // Turning it on records what is on screen right now — which is what somebody ticking
            // the box has just finished setting up.
            if (value) Persist();
        }

        private void Recalculate()
        {
            if (!_loaded) return;

            var focal = ParsePositiveDouble(FocalLengthMm);
            var fNumber = ParsePositiveDouble(Aperture);
            var distance = ParsePositiveDouble(SubjectDistanceMeters);
            var width = ParsePositiveInt(ImageWidthPixels);
            var declination = ParseDeclination(DeclinationDeg);

            HasFocalLengthError = focal is null;
            HasApertureError = fNumber is null;
            HasSubjectDistanceError = distance is null;
            HasImageWidthError = width is null;
            HasDeclinationError = declination is null;

            UpdateBodyName(width);

            if (focal is null || fNumber is null || distance is null)
            {
                DepthOfField = null;
                FieldOfView = null;
                StarExposure = null;
                HasResults = false;
                Persist();
                return;
            }

            DepthOfField = OpticsMath.CalculateDepthOfField(focal.Value, fNumber.Value, distance.Value, SelectedFormat);
            FieldOfView = OpticsMath.CalculateFieldOfView(focal.Value, distance.Value, SelectedFormat);

            // The star fields fall back to sane values rather than blanking depth-of-field
            // results the user may be part-way through reading.
            StarExposure = OpticsMath.CalculateStarExposure(
                focal.Value,
                fNumber.Value,
                SelectedFormat,
                width ?? DefaultImageWidthPixels,
                declination ?? 0.0);

            Format();
            HasResults = true;
            Persist();
        }

        /// <summary>Renders the results the way a photographer reads them.</summary>
        private void Format()
        {
            var dof = DepthOfField!.Value;
            HyperfocalText = Meters(dof.HyperfocalMeters);
            NearLimitText = Meters(dof.NearLimitMeters);
            FarLimitText = dof.FarLimitMeters is { } far ? Meters(far) : Infinity;
            TotalText = dof.TotalMeters is { } total ? Meters(total) : Infinity;
            InFrontText = Meters(dof.InFrontMeters);
            BehindText = dof.BehindMeters is { } behind ? Meters(behind) : Infinity;

            var fov = FieldOfView!.Value;
            HorizontalText = $"{Degrees(fov.HorizontalDeg)} \u00b7 {Meters(fov.HorizontalCoverageMeters)}";
            VerticalText = $"{Degrees(fov.VerticalDeg)} \u00b7 {Meters(fov.VerticalCoverageMeters)}";
            DiagonalText = Degrees(fov.DiagonalDeg);

            var stars = StarExposure!.Value;
            Rule500Text = $"{Seconds(stars.Rule500Seconds)} s";
            NpfText = $"{Seconds(stars.NpfSeconds)} s";
            PixelPitchText = $"{stars.PixelPitchMicrons.ToString("0.##", CultureInfo.InvariantCulture)} \u00b5m";
        }

        private const string Infinity = "\u221e";

        /// <summary>Distance with adaptive precision: cm below a metre, thinning decimals, km past 10 km.</summary>
        private static string Meters(double value) => value switch
        {
            < 1.0 => $"{Math.Round(value * 100, MidpointRounding.AwayFromZero):0} cm",
            < 10.0 => $"{value.ToString("0.##", CultureInfo.InvariantCulture)} m",
            < 100.0 => $"{value.ToString("0.#", CultureInfo.InvariantCulture)} m",
            < 10_000.0 => $"{Math.Round(value, MidpointRounding.AwayFromZero):0} m",
            _ => $"{(value / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)} km",
        };

        private static string Degrees(double value) =>
            $"{value.ToString("0.#", CultureInfo.InvariantCulture)}\u00b0";

        /// <summary>Exposure to the precision a photographer sets on the dial.</summary>
        private static string Seconds(double value) =>
            value < 10.0
                ? value.ToString("0.#", CultureInfo.InvariantCulture)
                : Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

        private void UpdateBodyName(int? width)
        {
            if (width is null)
            {
                BodyName = string.Empty;
                return;
            }

            var picked = SelectedBody;
            var stillAgrees = picked is not null
                && picked.Format == SelectedFormat
                && picked.ImageWidthPixels == width.Value;

            BodyName = stillAgrees
                ? picked!.Label
                : CameraBodies.UniqueMatch(SelectedFormat, width.Value)?.Label ?? string.Empty;
        }

        private void Persist()
        {
            if (!_loaded || !RememberInputs) return;

            AppSettings.OpticsFocalLengthMm = FocalLengthMm;
            AppSettings.OpticsAperture = Aperture;
            AppSettings.OpticsSubjectDistanceMeters = SubjectDistanceMeters;
            AppSettings.OpticsSensorFormat = SelectedFormat.Label;
            AppSettings.OpticsImageWidthPixels = ImageWidthPixels;
            AppSettings.OpticsDeclinationDeg = DeclinationDeg;
        }

        /// <summary>
        /// A positive, finite number. The finiteness is not pedantry: "1e400" overflows a double
        /// and parses to Infinity, which is greater than zero — it would pass as a focal length,
        /// draw no error, and turn every result computed from it into NaN.
        /// </summary>
        private static double? ParsePositiveDouble(string? text)
        {
            var normalised = text?.Trim().Replace(',', '.');
            if (!double.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            return value > 0.0 && double.IsFinite(value) ? value : null;
        }

        private static int? ParsePositiveInt(string? text) =>
            int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : null;

        /// <summary>Declination is only meaningful within ±90°.</summary>
        private static double? ParseDeclination(string? text)
        {
            var normalised = text?.Trim().Replace(',', '.');
            if (!double.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            return value is >= -90.0 and <= 90.0 ? value : null;
        }
    }
}
