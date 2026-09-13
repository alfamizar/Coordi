using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Equatorial coordinates: right ascension and declination, in degrees.</summary>
    public readonly record struct Equatorial(double RightAscensionDeg, double DeclinationDeg);

    /// <summary>Spherical coordinate transforms (Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 13).</summary>
    public static class Coordinates
    {
        /// <summary>Besselian epoch B1875.0.</summary>
        private const double JdB1875 = 2405889.2585;

        /// <summary>Julian epoch J2000.0 — the equinox star catalogues (e.g. <see cref="BrightStars"/>) are fixed in.</summary>
        public const double JdJ2000 = 2451545.0;

        /// <summary>
        /// Convert ecliptic longitude/latitude (degrees) to equatorial RA/Dec using the
        /// given obliquity ε (degrees). Pass the <b>apparent</b> longitude and <b>true</b>
        /// obliquity for apparent equatorial coordinates.
        /// </summary>
        public static Equatorial EclipticToEquatorial(double longitudeDeg, double latitudeDeg, double obliquityDeg)
        {
            var lam = ToRadians(longitudeDeg);
            var beta = ToRadians(latitudeDeg);
            var eps = ToRadians(obliquityDeg);
            var ra = Math.Atan2(
                Math.Sin(lam) * Math.Cos(eps) - Math.Tan(beta) * Math.Sin(eps),
                Math.Cos(lam));
            var dec = Math.Asin(Math.Sin(beta) * Math.Cos(eps) + Math.Cos(beta) * Math.Sin(eps) * Math.Sin(lam));
            return new Equatorial(NormalizeDegrees(ToDegrees(ra)), ToDegrees(dec));
        }

        /// <summary>
        /// Precess equatorial coordinates from the epoch of the given <paramref name="jd"/> to B1875.0.
        /// Uses the rigorous formulas from Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 21.
        /// This is required for determining the constellation from the IAU boundaries, which
        /// are fixed in the B1875.0 equinox.
        /// </summary>
        public static Equatorial PrecessToB1875(double jd, double rightAscensionDeg, double declinationDeg) =>
            PrecessEquatorial(jd, JdB1875, rightAscensionDeg, declinationDeg);

        /// <summary>
        /// Precess equatorial coordinates fixed in the B1875.0 equinox (e.g. the IAU constellation
        /// boundaries) to the epoch of the given <paramref name="jd"/> — the inverse of
        /// <see cref="PrecessToB1875"/>, used when <i>drawing</i> the boundaries in today's sky
        /// rather than classifying a position against them.
        /// </summary>
        public static Equatorial PrecessFromB1875(double jd, double rightAscensionDeg, double declinationDeg) =>
            PrecessEquatorial(JdB1875, jd, rightAscensionDeg, declinationDeg);

        /// <summary>Rigorous equatorial precession between two arbitrary epochs (Meeus ch. 21).</summary>
        public static Equatorial PrecessEquatorial(double fromJd, double toJd, double rightAscensionDeg, double declinationDeg)
        {
            // T = centuries from J2000.0 to the starting epoch; t = centuries start → target.
            var tCap = (fromJd - 2451545.0) / 36525.0;
            var t = (toJd - fromJd) / 36525.0;

            var zetaArcsec = (2306.2181 + 1.39656 * tCap - 0.000139 * tCap * tCap) * t +
                             (0.30188 - 0.000344 * tCap) * t * t +
                             0.017998 * t * t * t;

            var zArcsec = (2306.2181 + 1.39656 * tCap - 0.000139 * tCap * tCap) * t +
                          (1.09468 + 0.000066 * tCap) * t * t +
                          0.018203 * t * t * t;

            var thetaArcsec = (2004.3109 - 0.85330 * tCap - 0.000217 * tCap * tCap) * t -
                              (0.42665 + 0.000217 * tCap) * t * t -
                              0.041833 * t * t * t;

            var zeta = ToRadians(zetaArcsec / 3600.0);
            var z = ToRadians(zArcsec / 3600.0);
            var theta = ToRadians(thetaArcsec / 3600.0);

            var ra = ToRadians(rightAscensionDeg);
            var dec = ToRadians(declinationDeg);

            var a = Math.Cos(dec) * Math.Sin(ra + zeta);
            var b = Math.Cos(theta) * Math.Cos(dec) * Math.Cos(ra + zeta) - Math.Sin(theta) * Math.Sin(dec);
            var c = Math.Sin(theta) * Math.Cos(dec) * Math.Cos(ra + zeta) + Math.Cos(theta) * Math.Sin(dec);

            var raNew = Math.Atan2(a, b) + z;
            var decNew = Math.Asin(c);

            return new Equatorial(NormalizeDegrees(ToDegrees(raNew)), ToDegrees(decNew));
        }
    }
}
