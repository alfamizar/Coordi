using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Apparent position of the Sun at a given instant. Angles in degrees.</summary>
    /// <param name="ApparentLongitude">Apparent ecliptic longitude λ (degrees, [0,360)). Feeds the zodiac.</param>
    /// <param name="RightAscension">Apparent right ascension α (degrees, [0,360)).</param>
    /// <param name="Declination">Apparent declination δ (degrees).</param>
    /// <param name="Obliquity">True obliquity of the ecliptic ε (degrees).</param>
    public readonly record struct SunPosition(
        double ApparentLongitude,
        double RightAscension,
        double Declination,
        double Obliquity);

    /// <summary>
    /// Solar coordinates, low-precision series (≈0.01°), accurate to ~1 min for
    /// rise/set across the modern era. Clean-room from Meeus, <i>Astronomical
    /// Algorithms</i>, 2nd ed., ch. 25 (Sun) and ch. 28 (equation of time).
    ///
    /// Verified: obliquity @J2000 = 23.4392911°; δ @J2000 = −23.03°;
    /// EoT 2024-02-11 = −14.23 min, 2024-11-03 = +16.49 min.
    /// </summary>
    public static class Sun
    {
        /// <summary>Full apparent solar position for the given Julian Day.</summary>
        public static SunPosition PositionAt(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);

            // Geometric mean longitude and mean anomaly (Meeus 25.2).
            var l0 = NormalizeDegrees(280.46646 + 36000.76983 * t + 0.0003032 * t * t);
            var m = 357.52911 + 35999.05029 * t - 0.0001537 * t * t;
            var mr = ToRadians(m);

            // Sun's equation of center (Meeus, p. 164).
            var c = (1.914602 - 0.004817 * t - 0.000014 * t * t) * Math.Sin(mr) +
                    (0.019993 - 0.000101 * t) * Math.Sin(2 * mr) +
                    0.000289 * Math.Sin(3 * mr);

            var trueLongitude = l0 + c;

            // Apparent longitude: nutation + aberration (Meeus 25.8).
            var omega = 125.04 - 1934.136 * t;
            var lambda = trueLongitude - 0.00569 - 0.00478 * Math.Sin(ToRadians(omega));

            // Mean obliquity (Meeus 22.2) with the same nutation term applied.
            var eps0 = 23.0 + 26.0 / 60.0 + 21.448 / 3600.0 -
                       (46.8150 * t + 0.00059 * t * t - 0.001813 * t * t * t) / 3600.0;
            var eps = eps0 + 0.00256 * Math.Cos(ToRadians(omega));

            var lr = ToRadians(lambda);
            var er = ToRadians(eps);
            var alpha = NormalizeDegrees(ToDegrees(Math.Atan2(Math.Cos(er) * Math.Sin(lr), Math.Cos(lr))));
            var delta = ToDegrees(Math.Asin(Math.Sin(er) * Math.Sin(lr)));

            return new SunPosition(
                ApparentLongitude: NormalizeDegrees(lambda),
                RightAscension: alpha,
                Declination: delta,
                Obliquity: eps);
        }

        /// <summary>The constellation the Sun is currently in.</summary>
        public static Constellation ConstellationAt(double jd)
        {
            var pos = PositionAt(jd);
            return Constellations.Of(jd, pos.RightAscension, pos.Declination);
        }

        /// <summary>
        /// Equation of time in <b>minutes</b> (apparent − mean solar time). Meeus eq. 28.3.
        /// Positive means the true Sun is ahead of the mean Sun.
        /// </summary>
        public static double EquationOfTimeMinutes(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            var l0 = NormalizeDegrees(280.46646 + 36000.76983 * t + 0.0003032 * t * t);
            var m = 357.52911 + 35999.05029 * t - 0.0001537 * t * t;
            var e = 0.016708634 - 0.000042037 * t - 0.0000001267 * t * t;
            var eps0 = 23.0 + 26.0 / 60.0 + 21.448 / 3600.0 -
                       (46.8150 * t + 0.00059 * t * t - 0.001813 * t * t * t) / 3600.0;
            var tanHalfEps = Math.Tan(ToRadians(eps0 / 2.0));
            var y = tanHalfEps * tanHalfEps;
            var l0r = ToRadians(l0);
            var mr = ToRadians(m);
            var eRad = y * Math.Sin(2 * l0r) -
                       2 * e * Math.Sin(mr) +
                       4 * e * y * Math.Sin(mr) * Math.Cos(2 * l0r) -
                       0.5 * y * y * Math.Sin(4 * l0r) -
                       1.25 * e * e * Math.Sin(2 * mr);
            return ToDegrees(eRad) * 4.0; // 4 minutes of time per degree
        }

        /// <summary>
        /// Sun–Earth distance (radius vector) in astronomical units. Meeus eq. 25.5.
        /// Verified via Example 48.a: feeds illuminated fraction = 0.6786.
        /// </summary>
        public static double RadiusVectorAu(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            var m = 357.52911 + 35999.05029 * t - 0.0001537 * t * t;
            var e = 0.016708634 - 0.000042037 * t - 0.0000001267 * t * t;
            var mr = ToRadians(m);
            var c = (1.914602 - 0.004817 * t - 0.000014 * t * t) * Math.Sin(mr) +
                    (0.019993 - 0.000101 * t) * Math.Sin(2 * mr) +
                    0.000289 * Math.Sin(3 * mr);
            var v = m + c;
            return 1.000001018 * (1.0 - e * e) / (1.0 + e * Math.Cos(ToRadians(v)));
        }
    }
}
