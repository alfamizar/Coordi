using System;
using System.Globalization;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Which side of the equator a grid coordinate falls on.</summary>
    public enum Hemisphere
    {
        /// <summary>Northern hemisphere.</summary>
        North,

        /// <summary>Southern hemisphere.</summary>
        South,
    }

    /// <summary>
    /// A Universal Transverse Mercator coordinate on the WGS84 ellipsoid.
    ///
    /// <paramref name="ZoneNumber"/> is the 1..60 longitudinal zone; <paramref name="LatBand"/> is the
    /// MGRS latitude-band letter (C..X, omitting I and O). <paramref name="Easting"/>/<paramref name="Northing"/>
    /// are in metres (northing is the false-northing-adjusted value, so it is positive in both hemispheres).
    /// </summary>
    public sealed record Utm(
        int ZoneNumber,
        char LatBand,
        Hemisphere Hemisphere,
        double Easting,
        double Northing)
    {
        /// <summary>e.g. <c>31U 448266 5411938</c>.</summary>
        public string Format() => FormattableString.Invariant(
            $"{ZoneNumber}{LatBand} {RoundHalfUp(Easting)} {RoundHalfUp(Northing)}");
    }

    /// <summary>
    /// A Military Grid Reference System coordinate (WGS84 / "AA" lettering scheme).
    ///
    /// <paramref name="Digraph"/> is the two-letter 100 km square id; <paramref name="Easting"/>/<paramref name="Northing"/>
    /// are the metre offsets within that square (0..99999).
    /// </summary>
    public sealed record Mgrs(
        int ZoneNumber,
        char LatBand,
        string Digraph,
        int Easting,
        int Northing)
    {
        /// <summary>e.g. <c>31U DQ 48266 11938</c> (5-digit precision = 1 m).</summary>
        public string Format() => FormattableString.Invariant(
            $"{ZoneNumber}{LatBand} {Digraph} {PadZeros(Easting, 5)} {PadZeros(Northing, 5)}");
    }

    /// <summary>
    /// Map-grid conversions (UTM and MGRS) on the WGS84 ellipsoid.
    ///
    /// Clean-room from the public Transverse-Mercator formulas (J. P. Snyder, <i>Map
    /// Projections — A Working Manual</i>, USGS PP 1395, 1987) and the standard MGRS
    /// lettering scheme. No third-party source is used.
    ///
    /// Validity: the standard UTM band 80°S..84°N. Polar UPS regions (bands A/B/Y/Z) are
    /// not handled. Accuracy is sub-metre across the valid band (round-trip closes to
    /// well under 1 mm), cross-checked against the validated <see cref="Geodesy"/> meridian arc.
    /// </summary>
    public static class GridReference
    {
        private const double A = 6_378_137.0;         // WGS84 semi-major axis (m)
        private const double F = 1.0 / 298.257223563; // flattening
        private const double K0 = 0.9996;             // UTM central-meridian scale
        private static readonly double E2 = F * (2.0 - F);  // first eccentricity squared
        private static readonly double Ep2 = E2 / (1.0 - E2); // second eccentricity squared

        private const string LatBands = "CDEFGHJKLMNPQRSTUVWX";
        private const string RowLetters = "ABCDEFGHJKLMNPQRSTUV"; // 20, omits I and O

        /// <summary>Longitudinal zone for a longitude (with the Norway/Svalbard exceptions).</summary>
        public static int ZoneNumber(double latDeg, double lonDeg)
        {
            var lon = NormalizeLon(lonDeg);
            var zone = (int)Math.Floor((lon + 180.0) / 6.0) + 1;
            // Norway: band V, 3°E..12°E uses zone 32 (zone 31 is narrowed).
            if (latDeg >= 56.0 && latDeg <= 64.0 && lon >= 3.0 && lon <= 12.0) zone = 32;
            // Svalbard: band X (72°N..84°N) has widened zones.
            if (latDeg >= 72.0 && latDeg <= 84.0)
            {
                if (lon >= 0.0 && lon <= 9.0) zone = 31;
                else if (lon >= 9.0 && lon <= 21.0) zone = 33;
                else if (lon >= 21.0 && lon <= 33.0) zone = 35;
                else if (lon >= 33.0 && lon <= 42.0) zone = 37;
            }

            return zone;
        }

        /// <summary>MGRS latitude-band letter for a latitude in [-80, 84].</summary>
        public static char LatBand(double latDeg)
        {
            if (latDeg < -80.0 || latDeg > 84.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(latDeg), latDeg, "latitude is outside the UTM band (-80..84)");
            }

            var idx = Math.Min((int)((latDeg + 80.0) / 8.0), LatBands.Length - 1);
            return LatBands[idx];
        }

        /// <summary>Central meridian (degrees) of a zone.</summary>
        private static double CentralMeridianDeg(int zone) => zone * 6.0 - 183.0;

        /// <summary>Convert geographic lat/lon (degrees, WGS84) to UTM.</summary>
        public static Utm ToUtm(double latDeg, double lonDeg)
        {
            var zone = ZoneNumber(latDeg, lonDeg);
            var band = LatBand(latDeg);
            var phi = ToRadians(latDeg);
            var lambda = ToRadians(NormalizeLon(lonDeg));
            var lambda0 = ToRadians(CentralMeridianDeg(zone));

            var sinPhi = Math.Sin(phi);
            var cosPhi = Math.Cos(phi);
            var tanPhi = Math.Tan(phi);

            var n = A / Math.Sqrt(1.0 - E2 * sinPhi * sinPhi);
            var t = tanPhi * tanPhi;
            var c = Ep2 * cosPhi * cosPhi;
            var a = cosPhi * (lambda - lambda0);
            var m = MeridianArc(phi);

            var easting = K0 * n * (
                a + (1.0 - t + c) * Math.Pow(a, 3) / 6.0 +
                (5.0 - 18.0 * t + t * t + 72.0 * c - 58.0 * Ep2) * Math.Pow(a, 5) / 120.0
            ) + 500_000.0;

            var northing = K0 * (
                m + n * tanPhi * (
                    a * a / 2.0 + (5.0 - t + 9.0 * c + 4.0 * c * c) * Math.Pow(a, 4) / 24.0 +
                    (61.0 - 58.0 * t + t * t + 600.0 * c - 330.0 * Ep2) * Math.Pow(a, 6) / 720.0
                )
            );
            var hemisphere = latDeg >= 0 ? Hemisphere.North : Hemisphere.South;
            if (hemisphere == Hemisphere.South) northing += 10_000_000.0;

            return new Utm(zone, band, hemisphere, easting, northing);
        }

        /// <summary>
        /// Convert a UTM coordinate back to geographic coordinates. Returns (latitude, longitude)
        /// in degrees.
        /// </summary>
        public static (double LatDeg, double LonDeg) ToLatLon(Utm utm)
        {
            var x = utm.Easting - 500_000.0;
            var y = utm.Hemisphere == Hemisphere.South ? utm.Northing - 10_000_000.0 : utm.Northing;

            var m = y / K0;
            var mu = m / (A * (1.0 - E2 / 4.0 - 3.0 * E2 * E2 / 64.0 - 5.0 * E2 * E2 * E2 / 256.0));
            var e1 = (1.0 - Math.Sqrt(1.0 - E2)) / (1.0 + Math.Sqrt(1.0 - E2));

            var phi1 = mu +
                (3.0 * e1 / 2.0 - 27.0 * Math.Pow(e1, 3) / 32.0) * Math.Sin(2.0 * mu) +
                (21.0 * e1 * e1 / 16.0 - 55.0 * Math.Pow(e1, 4) / 32.0) * Math.Sin(4.0 * mu) +
                151.0 * Math.Pow(e1, 3) / 96.0 * Math.Sin(6.0 * mu) +
                1097.0 * Math.Pow(e1, 4) / 512.0 * Math.Sin(8.0 * mu);

            var sinPhi1 = Math.Sin(phi1);
            var cosPhi1 = Math.Cos(phi1);
            var tanPhi1 = Math.Tan(phi1);
            var n1 = A / Math.Sqrt(1.0 - E2 * sinPhi1 * sinPhi1);
            var t1 = tanPhi1 * tanPhi1;
            var c1 = Ep2 * cosPhi1 * cosPhi1;
            var r1 = A * (1.0 - E2) / Math.Pow(1.0 - E2 * sinPhi1 * sinPhi1, 1.5);
            var d = x / (n1 * K0);

            var lat = phi1 - n1 * tanPhi1 / r1 * (
                d * d / 2.0 -
                (5.0 + 3.0 * t1 + 10.0 * c1 - 4.0 * c1 * c1 - 9.0 * Ep2) * Math.Pow(d, 4) / 24.0 +
                (61.0 + 90.0 * t1 + 298.0 * c1 + 45.0 * t1 * t1 - 252.0 * Ep2 - 3.0 * c1 * c1) * Math.Pow(d, 6) / 720.0
            );
            var lon = ToRadians(CentralMeridianDeg(utm.ZoneNumber)) + (
                d - (1.0 + 2.0 * t1 + c1) * Math.Pow(d, 3) / 6.0 +
                (5.0 - 2.0 * c1 + 28.0 * t1 - 3.0 * c1 * c1 + 8.0 * Ep2 + 24.0 * t1 * t1) * Math.Pow(d, 5) / 120.0
            ) / cosPhi1;

            return (ToDegrees(lat), ToDegrees(lon));
        }

        /// <summary>Convert geographic lat/lon (degrees, WGS84) to MGRS.</summary>
        public static Mgrs ToMgrs(double latDeg, double lonDeg) => MgrsFromUtm(ToUtm(latDeg, lonDeg));

        private static Mgrs MgrsFromUtm(Utm utm)
        {
            // Column letter: one of three 8-letter sets keyed by zone, indexed by the
            // 100 km easting band (easting is 100 000..900 000 in valid UTM).
            var colSet = ((utm.ZoneNumber - 1) % 3) switch
            {
                0 => "ABCDEFGH",
                1 => "JKLMNPQR",
                _ => "STUVWXYZ",
            };
            var colIndex = (int)Math.Floor(utm.Easting / 100_000.0) - 1;
            var colLetter = colSet[colIndex];

            // Row letter: 20-letter cycle every 2 000 000 m; even zones start at 'F' (+5).
            var rowOffset = utm.ZoneNumber % 2 == 0 ? 5 : 0;
            var rowIndex = ((int)Math.Floor(utm.Northing % 2_000_000.0 / 100_000.0) + rowOffset) % 20;
            var rowLetter = RowLetters[rowIndex];

            var e = Clamp((int)RoundHalfUp(utm.Easting % 100_000.0), 0, 99_999);
            var n = Clamp((int)RoundHalfUp(utm.Northing % 100_000.0), 0, 99_999);

            return new Mgrs(utm.ZoneNumber, utm.LatBand, new string(new[] { colLetter, rowLetter }), e, n);
        }

        /// <summary>Meridian arc length M(φ) from the equator, in metres (Snyder eq. 3-21).</summary>
        private static double MeridianArc(double phi) => A * (
            (1.0 - E2 / 4.0 - 3.0 * E2 * E2 / 64.0 - 5.0 * E2 * E2 * E2 / 256.0) * phi -
            (3.0 * E2 / 8.0 + 3.0 * E2 * E2 / 32.0 + 45.0 * E2 * E2 * E2 / 1024.0) * Math.Sin(2.0 * phi) +
            (15.0 * E2 * E2 / 256.0 + 45.0 * E2 * E2 * E2 / 1024.0) * Math.Sin(4.0 * phi) -
            35.0 * E2 * E2 * E2 / 3072.0 * Math.Sin(6.0 * phi)
        );

        private static double NormalizeLon(double lonDeg)
        {
            var l = lonDeg % 360.0;
            if (l >= 180.0) l -= 360.0;
            if (l < -180.0) l += 360.0;
            return l;
        }
    }
}
