using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>
    /// Nutation and obliquity of the ecliptic.
    ///
    /// Clean-room from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 22 (the
    /// abbreviated nutation series, accurate to ≈0.5″ in Δψ / 0.1″ in Δε — ample for
    /// civil rise/set and phase work) and ch. 12 (apparent sidereal time).
    /// </summary>
    public static class Nutation
    {
        /// <summary>Δψ (nutation in longitude) and Δε (nutation in obliquity), both in degrees.</summary>
        public readonly record struct Result(double LongitudeDeg, double ObliquityDeg);

        /// <summary>Nutation in longitude and obliquity at the given Julian Day.</summary>
        public static Result At(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            var omega = ToRadians(NormalizeDegrees(125.04452 - 1934.136261 * t)); // Moon's ascending node
            var l = ToRadians(NormalizeDegrees(280.4665 + 36000.7698 * t));       // Sun mean longitude
            var lp = ToRadians(NormalizeDegrees(218.3165 + 481267.8813 * t));     // Moon mean longitude

            var dPsiArcsec = -17.20 * Math.Sin(omega) - 1.32 * Math.Sin(2 * l) -
                             0.23 * Math.Sin(2 * lp) + 0.21 * Math.Sin(2 * omega);
            var dEpsArcsec = 9.20 * Math.Cos(omega) + 0.57 * Math.Cos(2 * l) +
                             0.10 * Math.Cos(2 * lp) - 0.09 * Math.Cos(2 * omega);

            return new Result(dPsiArcsec / 3600.0, dEpsArcsec / 3600.0);
        }

        /// <summary>Mean obliquity ε₀ in degrees (Meeus eq. 22.2).</summary>
        public static double MeanObliquityDeg(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            return 23.0 + 26.0 / 60.0 + 21.448 / 3600.0 -
                   (46.8150 * t + 0.00059 * t * t - 0.001813 * t * t * t) / 3600.0;
        }

        /// <summary>True obliquity ε = ε₀ + Δε, in degrees.</summary>
        public static double TrueObliquityDeg(double jd) => MeanObliquityDeg(jd) + At(jd).ObliquityDeg;

        /// <summary>
        /// Apparent Greenwich sidereal time in degrees [0,360):
        /// GMST + the equation of the equinoxes (Δψ · cos ε).
        /// </summary>
        public static double ApparentSiderealTimeDeg(double jd)
        {
            // One evaluation feeds both terms: TrueObliquityDeg(jd) would run the series again.
            var nutation = At(jd);
            var eps = MeanObliquityDeg(jd) + nutation.ObliquityDeg;
            var eqEquinox = nutation.LongitudeDeg * Math.Cos(ToRadians(eps));
            return NormalizeDegrees(AstroTime.GreenwichMeanSiderealTimeDeg(jd) + eqEquinox);
        }
    }
}
