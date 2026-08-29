using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Result of an inverse geodesic computation.</summary>
    /// <param name="DistanceMeters">Ellipsoidal distance in metres.</param>
    /// <param name="InitialBearingDeg">Initial bearing from point 1, degrees [0,360).</param>
    /// <param name="FinalBearingDeg">Final bearing arriving at point 2, degrees [0,360).</param>
    public readonly record struct GeodesicResult(
        double DistanceMeters,
        double InitialBearingDeg,
        double FinalBearingDeg);

    /// <summary>
    /// Geodesic distance/bearing on the WGS84 ellipsoid via Vincenty's inverse
    /// formula. Clean-room from T. Vincenty (1975), <i>Survey Review</i> XXIII (176).
    ///
    /// Verified against known WGS84 values: 1° of longitude at the equator =
    /// 111319.4908 m; 1° of latitude near the equator = 110574.389 m.
    ///
    /// Accurate to ~0.5 mm. For near-antipodal pairs Vincenty can fail to converge;
    /// this returns the best iterate reached.
    /// </summary>
    public static class Geodesy
    {
        private const double A = 6_378_137.0;         // WGS84 semi-major axis (m)
        private const double F = 1.0 / 298.257223563; // WGS84 flattening
        private const double B = A * (1.0 - F);       // semi-minor axis (m)
        private const int MaxIter = 1000;
        private const double Eps = 1e-12;

        /// <summary>Distance and bearings between two geographic points (degrees, WGS84).</summary>
        public static GeodesicResult Inverse(
            double lat1Deg,
            double lon1Deg,
            double lat2Deg,
            double lon2Deg)
        {
            var l = ToRadians(lon2Deg - lon1Deg);
            var u1 = Math.Atan((1.0 - F) * Math.Tan(ToRadians(lat1Deg)));
            var u2 = Math.Atan((1.0 - F) * Math.Tan(ToRadians(lat2Deg)));
            var sinU1 = Math.Sin(u1);
            var cosU1 = Math.Cos(u1);
            var sinU2 = Math.Sin(u2);
            var cosU2 = Math.Cos(u2);

            var lambda = l;
            var sinSigma = 0.0;
            var cosSigma = 0.0;
            var sigma = 0.0;
            var cosSqAlpha = 0.0;
            var cos2SigmaM = 0.0;

            var iterations = 0;
            while (iterations < MaxIter)
            {
                iterations++;
                var sinLambdaIt = Math.Sin(lambda);
                var cosLambdaIt = Math.Cos(lambda);
                var termA = cosU2 * sinLambdaIt;
                var termB = cosU1 * sinU2 - sinU1 * cosU2 * cosLambdaIt;
                sinSigma = Math.Sqrt(termA * termA + termB * termB);
                if (sinSigma == 0.0)
                {
                    return new GeodesicResult(0.0, 0.0, 0.0); // coincident points
                }

                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambdaIt;
                sigma = Math.Atan2(sinSigma, cosSigma);
                var sinAlpha = cosU1 * cosU2 * sinLambdaIt / sinSigma;
                cosSqAlpha = 1.0 - sinAlpha * sinAlpha;
                cos2SigmaM = cosSqAlpha != 0.0 ? cosSigma - 2.0 * sinU1 * sinU2 / cosSqAlpha : 0.0;
                var c = F / 16.0 * cosSqAlpha * (4.0 + F * (4.0 - 3.0 * cosSqAlpha));
                var lambdaPrev = lambda;
                lambda = l + (1.0 - c) * F * sinAlpha *
                    (sigma + c * sinSigma * (cos2SigmaM + c * cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM)));
                if (Math.Abs(lambda - lambdaPrev) < Eps) break;
            }

            var uSq = cosSqAlpha * (A * A - B * B) / (B * B);
            var aCoef = 1.0 + uSq / 16384.0 *
                (4096.0 + uSq * (-768.0 + uSq * (320.0 - 175.0 * uSq)));
            var bCoef = uSq / 1024.0 *
                (256.0 + uSq * (-128.0 + uSq * (74.0 - 47.0 * uSq)));
            var deltaSigma = bCoef * sinSigma *
                (cos2SigmaM + bCoef / 4.0 *
                    (cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM) -
                     bCoef / 6.0 * cos2SigmaM * (-3.0 + 4.0 * sinSigma * sinSigma) *
                     (-3.0 + 4.0 * cos2SigmaM * cos2SigmaM)));

            var distance = B * aCoef * (sigma - deltaSigma);

            // Bearings (Vincenty, eqs. for α1, α2).
            var sinLambda = Math.Sin(lambda);
            var cosLambda = Math.Cos(lambda);
            var initial = Math.Atan2(
                cosU2 * sinLambda,
                cosU1 * sinU2 - sinU1 * cosU2 * cosLambda);
            var final = Math.Atan2(
                cosU1 * sinLambda,
                -sinU1 * cosU2 + cosU1 * sinU2 * cosLambda);

            return new GeodesicResult(
                DistanceMeters: distance,
                InitialBearingDeg: NormalizeDegrees(ToDegrees(initial)),
                FinalBearingDeg: NormalizeDegrees(ToDegrees(final)));
        }

        /// <summary>Distance only, in metres.</summary>
        public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2) =>
            Inverse(lat1, lon1, lat2, lon2).DistanceMeters;
    }
}
