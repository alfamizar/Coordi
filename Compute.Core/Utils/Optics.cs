namespace Compute.Core.Utils
{
    /// <summary>
    /// Closed-form photographic optics: depth of field, hyperfocal distance, field of view and
    /// the longest exposure that still renders stars as points. Pure maths, no dependencies.
    ///
    /// The thin-lens / circle-of-confusion model is the standard one. All lengths are millimetres
    /// internally; results are metres (distances) and degrees (angles). Everything is exact for
    /// the given inputs — the only modelling choice is the circle of confusion, derived from the
    /// sensor diagonal (the "d/1500" Zeiss convention).
    ///
    /// Ported from AstroClaw's <c>:feature-optics</c>; the tests carry the same reference values.
    /// </summary>
    public static class Optics
    {
        /// <summary>Divisor of the sensor diagonal that yields the circle of confusion.</summary>
        public const double CircleOfConfusionDivisor = 1500.0;

        /// <summary>Diagonal of a 36×24 mm frame — the reference for crop factor.</summary>
        private static readonly double FullFrameDiagonalMm = Math.Sqrt(36.0 * 36.0 + 24.0 * 24.0);

        /// <summary>~87.4° declination: beyond this the rules stop meaning anything, so cos δ is clamped.</summary>
        private const double MinCosDeclination = 0.045;

        /// <summary>
        /// A camera sensor format. The circle of confusion comes from the diagonal, so adding a
        /// format needs only its physical dimensions.
        /// </summary>
        public sealed record SensorFormat(string Label, double WidthMm, double HeightMm)
        {
            public double DiagonalMm => Math.Sqrt(WidthMm * WidthMm + HeightMm * HeightMm);

            /// <summary>Circle of confusion (mm) for this format.</summary>
            public double CircleOfConfusionMm => DiagonalMm / CircleOfConfusionDivisor;

            public static readonly SensorFormat FullFrame = new("Full frame", 36.0, 24.0);
            public static readonly SensorFormat ApsC = new("APS-C (1.5×)", 23.6, 15.7);
            public static readonly SensorFormat ApsCCanon = new("APS-C Canon (1.6×)", 22.3, 14.9);
            public static readonly SensorFormat MicroFourThirds = new("Micro 4/3", 17.3, 13.0);
            public static readonly SensorFormat OneInch = new("1-inch", 13.2, 8.8);
            public static readonly SensorFormat MediumFormat = new("Medium format (44×33)", 44.0, 33.0);

            /// <summary>Every format, in the order the chips show them.</summary>
            public static IReadOnlyList<SensorFormat> All { get; } =
            [
                FullFrame, ApsC, ApsCCanon, MicroFourThirds, OneInch, MediumFormat
            ];
        }

        /// <summary>
        /// Depth-of-field result. Distances in metres; the nullable members are null when the
        /// far limit runs to infinity.
        /// </summary>
        public readonly record struct DepthOfField(
            double HyperfocalMeters,
            double NearLimitMeters,
            double? FarLimitMeters,
            double? TotalMeters,
            double InFrontMeters,
            double? BehindMeters);

        /// <summary>Angular coverage (degrees) and the scene it spans at the subject distance (metres).</summary>
        public readonly record struct FieldOfView(
            double HorizontalDeg,
            double VerticalDeg,
            double DiagonalDeg,
            double HorizontalCoverageMeters,
            double VerticalCoverageMeters);

        /// <summary>
        /// The longest exposure that still renders stars as points, by both common rules.
        ///
        /// Neither is a law of physics: each answers "how long until trailing becomes visible",
        /// and they disagree because they assume different viewing conditions — which is exactly
        /// why a planner should show both rather than pick one.
        /// </summary>
        public readonly record struct StarExposure(
            double Rule500Seconds,
            double NpfSeconds,
            double PixelPitchMicrons);

        /// <summary>
        /// Hyperfocal distance H (metres): focus here and everything from H/2 to infinity is
        /// acceptably sharp. H = f²/(N·c) + f.
        /// </summary>
        public static double HyperfocalMeters(double focalLengthMm, double aperture, double circleOfConfusionMm) =>
            (focalLengthMm * focalLengthMm / (aperture * circleOfConfusionMm) + focalLengthMm) / 1000.0;

        /// <summary>Depth of field for a lens focused at <paramref name="subjectDistanceMeters"/>.</summary>
        public static DepthOfField CalculateDepthOfField(
            double focalLengthMm,
            double aperture,
            double subjectDistanceMeters,
            SensorFormat format)
        {
            var f = focalLengthMm;
            var c = format.CircleOfConfusionMm;
            var h = f * f / (aperture * c) + f;      // hyperfocal, mm
            var s = subjectDistanceMeters * 1000.0;  // subject distance, mm

            var nearMm = s * (h - f) / (h + s - 2 * f);

            // The far limit runs to infinity once the subject reaches the hyperfocal distance.
            var farInfinite = s >= h - f;
            double? farMm = farInfinite ? null : s * (h - f) / (h - s);

            var nearMeters = nearMm / 1000.0;
            var farMeters = farMm / 1000.0;

            return new DepthOfField(
                HyperfocalMeters: h / 1000.0,
                NearLimitMeters: nearMeters,
                FarLimitMeters: farMeters,
                TotalMeters: farMeters - nearMeters,
                InFrontMeters: subjectDistanceMeters - nearMeters,
                BehindMeters: farMeters - subjectDistanceMeters);
        }

        /// <summary>
        /// Angular field of view and the scene it covers at <paramref name="subjectDistanceMeters"/>.
        /// Angular FoV along a sensor dimension d is 2·atan(d / 2f).
        /// </summary>
        public static FieldOfView CalculateFieldOfView(
            double focalLengthMm,
            double subjectDistanceMeters,
            SensorFormat format)
        {
            double AngleDeg(double dimensionMm) =>
                2.0 * Math.Atan(dimensionMm / (2.0 * focalLengthMm)) * 180.0 / Math.PI;

            // Linear coverage = 2·s·tan(angle/2).
            double Coverage(double angleDeg) =>
                2.0 * subjectDistanceMeters * Math.Tan(angleDeg / 2.0 * Math.PI / 180.0);

            var h = AngleDeg(format.WidthMm);
            var v = AngleDeg(format.HeightMm);
            var d = AngleDeg(format.DiagonalMm);

            return new FieldOfView(h, v, d, Coverage(h), Coverage(v));
        }

        /// <summary>
        /// Pixel pitch (µm): the size of one photosite, from the sensor width and the image width
        /// in pixels. This is what makes NPF sensor-aware — a 60 MP body trails visibly sooner
        /// than a 24 MP one through the same lens.
        /// </summary>
        public static double PixelPitchMicrons(SensorFormat format, int imageWidthPixels) =>
            format.WidthMm * 1000.0 / imageWidthPixels;

        /// <summary>
        /// Star-trail limits for a lens/body/target combination.
        ///
        /// Stars drift at 15.041″/s times cos δ, so a target near the pole may be exposed far
        /// longer than one on the celestial equator; both rules are divided by cos δ. Declination
        /// defaults to 0 — the celestial equator, the worst case, and where the Milky Way core sits.
        ///
        ///   500 rule:  t = 500 / (f · cos δ)             — f full-frame equivalent
        ///   NPF rule:  t = (35·N + 30·p) / (f · cos δ)   — N f-number, p pitch in µm
        /// </summary>
        public static StarExposure CalculateStarExposure(
            double focalLengthMm,
            double aperture,
            SensorFormat format,
            int imageWidthPixels,
            double declinationDeg = 0.0)
        {
            var pitch = PixelPitchMicrons(format, imageWidthPixels);

            // Trailing is driven by the angular field, so the 500 rule wants the full-frame
            // equivalent focal length; NPF works from the real focal length and the pitch.
            var cropFactor = FullFrameDiagonalMm / format.DiagonalMm;
            var equivalentFocal = focalLengthMm * cropFactor;

            // Clamped: cos δ → 0 at the pole would divide by zero.
            var cosDec = Math.Max(Math.Cos(declinationDeg * Math.PI / 180.0), MinCosDeclination);

            return new StarExposure(
                Rule500Seconds: 500.0 / (equivalentFocal * cosDec),
                NpfSeconds: (35.0 * aperture + 30.0 * pitch) / (focalLengthMm * cosDec),
                PixelPitchMicrons: pitch);
        }
    }
}
