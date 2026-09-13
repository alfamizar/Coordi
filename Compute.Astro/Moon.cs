using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Apparent geocentric position of the Moon. Angles in degrees, distance in km.</summary>
    /// <param name="ApparentLongitudeDeg">Apparent ecliptic longitude (geometric + nutation Δψ).</param>
    /// <param name="LatitudeDeg">Ecliptic latitude β.</param>
    /// <param name="DistanceKm">Earth–Moon centre distance (km).</param>
    /// <param name="HorizontalParallaxDeg">Equatorial horizontal parallax π (degrees).</param>
    /// <param name="RightAscensionDeg">Apparent right ascension α (degrees, [0,360)).</param>
    /// <param name="DeclinationDeg">Apparent declination δ (degrees).</param>
    public readonly record struct MoonPosition(
        double ApparentLongitudeDeg,
        double LatitudeDeg,
        double DistanceKm,
        double HorizontalParallaxDeg,
        double RightAscensionDeg,
        double DeclinationDeg);

    /// <summary>
    /// Position and illumination of the Moon.
    ///
    /// Clean-room from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 47 (the
    /// truncated ELP-2000/82 periodic series — Tables 47.A and 47.B) and ch. 48
    /// (illuminated fraction). Accuracy ≈10″ in longitude / 4″ in latitude.
    ///
    /// Verified against Example 47.a (1992-04-12): Σl/Σr/Σb match the published sums
    /// exactly (−1127527 / −16590875 / −3229126); λ=133.162655°, β=−3.229126°,
    /// Δ=368409.7 km, π=0.991990°. Illuminated fraction matches Example 48.a (0.6786).
    /// </summary>
    public static class Moon
    {
        private const double EarthRadiusKm = 6378.14;
        private const double AuKm = 149_597_870.7;

        /// <summary>Periodic terms for longitude &amp; distance, flattened: {D, M, M′, F, Σl[1e-6°], Σr[1e-3 km]} per term.</summary>
        private const int LonDistTermsStride = 6;

        private static readonly int[] LonDistTerms =
        {
            0, 0, 1, 0, 6288774, -20905355,
            2, 0, -1, 0, 1274027, -3699111,
            2, 0, 0, 0, 658314, -2955968,
            0, 0, 2, 0, 213618, -569925,
            0, 1, 0, 0, -185116, 48888,
            0, 0, 0, 2, -114332, -3149,
            2, 0, -2, 0, 58793, 246158,
            2, -1, -1, 0, 57066, -152138,
            2, 0, 1, 0, 53322, -170733,
            2, -1, 0, 0, 45758, -204586,
            0, 1, -1, 0, -40923, -129620,
            1, 0, 0, 0, -34720, 108743,
            0, 1, 1, 0, -30383, 104755,
            2, 0, 0, -2, 15327, 10321,
            0, 0, 1, 2, -12528, 0,
            0, 0, 1, -2, 10980, 79661,
            4, 0, -1, 0, 10675, -34782,
            0, 0, 3, 0, 10034, -23210,
            4, 0, -2, 0, 8548, -21636,
            2, 1, -1, 0, -7888, 24208,
            2, 1, 0, 0, -6766, 30824,
            1, 0, -1, 0, -5163, -8379,
            1, 1, 0, 0, 4987, -16675,
            2, -1, 1, 0, 4036, -12831,
            2, 0, 2, 0, 3994, -10445,
            4, 0, 0, 0, 3861, -11650,
            2, 0, -3, 0, 3665, 14403,
            0, 1, -2, 0, -2689, -7003,
            2, 0, -1, 2, -2602, 0,
            2, -1, -2, 0, 2390, 10056,
            1, 0, 1, 0, -2348, 6322,
            2, -2, 0, 0, 2236, -9884,
            0, 1, 2, 0, -2120, 5751,
            0, 2, 0, 0, -2069, 0,
            2, -2, -1, 0, 2048, -4950,
            2, 0, 1, -2, -1773, 4130,
            2, 0, 0, 2, -1595, 0,
            4, -1, -1, 0, 1215, -3958,
            0, 0, 2, 2, -1110, 0,
            3, 0, -1, 0, -892, 3258,
            2, 1, 1, 0, -810, 2616,
            4, -1, -2, 0, 759, -1897,
            0, 2, -1, 0, -713, -2117,
            2, 2, -1, 0, -700, 2354,
            2, 1, -2, 0, 691, 0,
            2, -1, 0, -2, 596, 0,
            4, 0, 1, 0, 549, -1423,
            0, 0, 4, 0, 537, -1117,
            4, -1, 0, 0, 520, -1571,
            1, 0, -2, 0, -487, -1739,
            2, 1, 0, -2, -399, 0,
            0, 0, 2, -2, -381, -4421,
            1, 1, 1, 0, 351, 0,
            3, 0, -2, 0, -340, 0,
            4, 0, -3, 0, 330, 0,
            2, -1, 2, 0, 327, 0,
            0, 2, 1, 0, -323, 1165,
            1, 1, -1, 0, 299, 0,
            2, 0, 3, 0, 294, 0,
            2, 0, -1, -2, 0, 8752,
        };

        /// <summary>Periodic terms for latitude, flattened: {D, M, M′, F, Σb[1e-6°]} per term.</summary>
        private const int LatTermsStride = 5;

        private static readonly int[] LatTerms =
        {
            0, 0, 0, 1, 5128122,
            0, 0, 1, 1, 280602,
            0, 0, 1, -1, 277693,
            2, 0, 0, -1, 173237,
            2, 0, -1, 1, 55413,
            2, 0, -1, -1, 46271,
            2, 0, 0, 1, 32573,
            0, 0, 2, 1, 17198,
            2, 0, 1, -1, 9266,
            0, 0, 2, -1, 8822,
            2, -1, 0, -1, 8216,
            2, 0, -2, -1, 4324,
            2, 0, 1, 1, 4200,
            2, 1, 0, -1, -3359,
            2, -1, -1, 1, 2463,
            2, -1, 0, 1, 2211,
            2, -1, -1, -1, 2065,
            0, 1, -1, -1, -1870,
            4, 0, -1, -1, 1828,
            0, 1, 0, 1, -1794,
            0, 0, 0, 3, -1749,
            0, 1, -1, 1, -1565,
            1, 0, 0, 1, -1491,
            0, 1, 1, 1, -1475,
            0, 1, 1, -1, -1410,
            0, 1, 0, -1, -1344,
            1, 0, 0, -1, -1335,
            0, 0, 3, 1, 1107,
            4, 0, 0, -1, 1021,
            4, 0, -1, 1, 833,
            0, 0, 1, -3, 777,
            4, 0, -2, 1, 671,
            2, 0, 0, -3, 607,
            2, 0, 2, -1, 596,
            2, -1, 1, -1, 491,
            2, 0, -2, 1, -451,
            0, 0, 3, -1, 439,
            2, 0, 2, 1, 422,
            2, 0, -3, -1, 421,
            2, 1, -1, 1, -366,
            2, 1, 0, 1, -351,
            4, 0, 0, 1, 331,
            2, -1, 1, 1, 315,
            2, -2, 0, -1, 302,
            0, 0, 1, 3, -283,
            2, 1, 1, -1, -229,
            1, 1, 0, -1, 223,
            1, 1, 0, 1, 223,
            0, 1, -2, -1, -220,
            2, 1, -1, -1, -220,
            1, 0, 1, 1, -185,
            2, -1, -2, -1, 181,
            0, 1, 2, 1, -177,
            4, 0, -2, -1, 176,
            4, -1, -1, -1, 166,
            1, 0, 1, -1, -164,
            4, 0, 1, -1, 132,
            1, 0, -1, -1, -119,
            4, -1, 0, -1, 115,
            2, -2, 0, 1, 107,
        };

        /// <summary>
        /// e raised to |M|, where the periodic tables only ever use M = 0, ±1 or ±2.
        /// Meeus's e-factor correction (ch. 47), without paying for a general power.
        /// </summary>
        private static double EccentricityFactor(int mArgument, double e, double eSquared)
        {
            var absM = mArgument < 0 ? -mArgument : mArgument;
            return absM switch
            {
                0 => 1.0,
                1 => e,
                _ => eSquared,
            };
        }

        /// <summary>Apparent geocentric position of the Moon at the given Julian Day.</summary>
        public static MoonPosition PositionAt(double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            // t² … t⁴ by multiplication: Math.Pow costs ~50 ns a call and this runs in the
            // innermost loop of every eclipse and rise/set search.
            var t2 = t * t;
            var t3 = t2 * t;
            var t4 = t3 * t;

            var lp = NormalizeDegrees(218.3164477 + 481267.88123421 * t - 0.0015786 * t2 +
                t3 / 538841.0 - t4 / 65194000.0);
            var d = NormalizeDegrees(297.8501921 + 445267.1114034 * t - 0.0018819 * t2 +
                t3 / 545868.0 - t4 / 113065000.0);
            var m = NormalizeDegrees(357.5291092 + 35999.0502909 * t - 0.0001536 * t2 +
                t3 / 24490000.0);
            var mp = NormalizeDegrees(134.9633964 + 477198.8675055 * t + 0.0087414 * t2 +
                t3 / 69699.0 - t4 / 14712000.0);
            var f = NormalizeDegrees(93.2720950 + 483202.0175233 * t - 0.0036539 * t2 -
                t3 / 3526000.0 + t4 / 863310000.0);

            var a1 = NormalizeDegrees(119.75 + 131.849 * t);
            var a2 = NormalizeDegrees(53.09 + 479264.290 * t);
            var a3 = NormalizeDegrees(313.45 + 481266.484 * t);
            var e = 1.0 - 0.002516 * t - 0.0000074 * t2;
            // The eccentricity factor is only ever e⁰, e¹ or e² — |M| never exceeds 2.
            var e2 = e * e;

            var sumL = 0.0;
            var sumR = 0.0;
            var lonTerms = LonDistTerms;
            for (var i = 0; i < lonTerms.Length; i += LonDistTermsStride)
            {
                var mArg = lonTerms[i + 1];
                var arg = ToRadians(lonTerms[i] * d + mArg * m + lonTerms[i + 2] * mp + lonTerms[i + 3] * f);
                var eFactor = EccentricityFactor(mArg, e, e2);
                // Math.SinCos was measured here and made no difference, so the plain pair stays.
                sumL += lonTerms[i + 4] * eFactor * Math.Sin(arg);
                sumR += lonTerms[i + 5] * eFactor * Math.Cos(arg);
            }

            var sumB = 0.0;
            var latTerms = LatTerms;
            for (var i = 0; i < latTerms.Length; i += LatTermsStride)
            {
                var mArg = latTerms[i + 1];
                var arg = ToRadians(latTerms[i] * d + mArg * m + latTerms[i + 2] * mp + latTerms[i + 3] * f);
                sumB += latTerms[i + 4] * EccentricityFactor(mArg, e, e2) * Math.Sin(arg);
            }

            // Additive terms (Meeus, p. 342).
            sumL += 3958.0 * Math.Sin(ToRadians(a1)) + 1962.0 * Math.Sin(ToRadians(lp - f)) + 318.0 * Math.Sin(ToRadians(a2));
            sumB += -2235.0 * Math.Sin(ToRadians(lp)) + 382.0 * Math.Sin(ToRadians(a3)) +
                    175.0 * Math.Sin(ToRadians(a1 - f)) + 175.0 * Math.Sin(ToRadians(a1 + f)) +
                    127.0 * Math.Sin(ToRadians(lp - mp)) - 115.0 * Math.Sin(ToRadians(lp + mp));

            var geomLongitude = lp + sumL / 1_000_000.0;
            var latitude = sumB / 1_000_000.0;
            var distance = 385000.56 + sumR / 1000.0;
            var parallax = ToDegrees(Math.Asin(EarthRadiusKm / distance));

            // One nutation evaluation feeds both the apparent longitude and the true obliquity;
            // calling Nutation.TrueObliquityDeg here would run the whole series a second time.
            var nutation = Nutation.At(jd);
            var apparentLongitude = geomLongitude + nutation.LongitudeDeg;
            var trueObliquity = Nutation.MeanObliquityDeg(jd) + nutation.ObliquityDeg;
            var eq = Coordinates.EclipticToEquatorial(apparentLongitude, latitude, trueObliquity);

            return new MoonPosition(
                ApparentLongitudeDeg: NormalizeDegrees(apparentLongitude),
                LatitudeDeg: latitude,
                DistanceKm: distance,
                HorizontalParallaxDeg: parallax,
                RightAscensionDeg: eq.RightAscensionDeg,
                DeclinationDeg: eq.DeclinationDeg);
        }

        /// <summary>The constellation the Moon is currently in.</summary>
        public static Constellation ConstellationAt(double jd)
        {
            var pos = PositionAt(jd);
            return Constellations.Of(jd, pos.RightAscensionDeg, pos.DeclinationDeg);
        }

        /// <summary>Phase angle of the Moon in degrees (0 = full, 180 = new). Meeus ch. 48.</summary>
        public static double PhaseAngle(double jd)
        {
            var moon = PositionAt(jd);
            var sunLongitude = Sun.PositionAt(jd).ApparentLongitude;
            var sunDistanceKm = Sun.RadiusVectorAu(jd) * AuKm;
            var psi = Math.Acos(
                Math.Cos(ToRadians(moon.LatitudeDeg)) *
                Math.Cos(ToRadians(moon.ApparentLongitudeDeg - sunLongitude)));
            var i = Math.Atan2(
                sunDistanceKm * Math.Sin(psi),
                moon.DistanceKm - sunDistanceKm * Math.Cos(psi));
            return ToDegrees(i);
        }

        /// <summary>Illuminated fraction of the Moon's disk, 0..1.</summary>
        public static double IlluminatedFraction(double jd) => (1.0 + Math.Cos(ToRadians(PhaseAngle(jd)))) / 2.0;

        /// <summary>True when the Moon is waxing (elongation east of the Sun, 0°..180°).</summary>
        public static bool IsWaxing(double jd)
        {
            var elong = NormalizeDegrees(PositionAt(jd).ApparentLongitudeDeg - Sun.PositionAt(jd).ApparentLongitude);
            return elong < 180.0;
        }
    }
}
