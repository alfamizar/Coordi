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
            double lon2Deg) =>
            Inverse(lat1Deg, lon1Deg, lat2Deg, lon2Deg, allowAntipodalFallback: true);

        /// <summary>
        /// Vincenty's inverse. <paramref name="allowAntipodalFallback"/> is false when this is
        /// solving one leg of <see cref="AntipodalInverse"/>. Those legs span roughly a quarter of
        /// the globe and so are never themselves antipodal, and refusing the fallback there makes
        /// the absence of recursion structural rather than an assumption about the inputs.
        /// </summary>
        private static GeodesicResult Inverse(
            double lat1Deg,
            double lon1Deg,
            double lat2Deg,
            double lon2Deg,
            bool allowAntipodalFallback)
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

            var converged = false;
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
                if (Math.Abs(lambda - lambdaPrev) < Eps)
                {
                    converged = true;
                    break;
                }
            }

            // Vincenty's inverse does not converge for nearly antipodal points: the iteration
            // oscillates and the loop leaves on its cap holding a poor lambda. The distance that
            // falls out of it is not merely imprecise — for equatorial antipodes it came out
            // ~100 km SHORTER than the shortest path that exists on this planet.
            if (!converged && allowAntipodalFallback)
            {
                return AntipodalInverse(lat1Deg, lon1Deg, lat2Deg, lon2Deg);
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

        /// <summary>
        /// The geodesic between nearly antipodal points, found by splitting it in two.
        ///
        /// Every geodesic from P1 to its (near) antipode passes through a point roughly a quarter
        /// of the way around the globe from P1, and which one it passes through is exactly what
        /// Vincenty cannot decide here. So this asks the question the other way round: sweep that
        /// waypoint around the circle of candidates, measure P1→M and M→P2 — each an ordinary
        /// ~10,000 km line the formula solves without complaint — and keep the shortest total.
        ///
        /// The minimum over that sweep is the geodesic, because a shortest path's own midpoint
        /// lies on the locus being swept. A coarse pass finds the basin; golden-section narrows
        /// it. Both legs are converged solutions, so the sum carries their accuracy.
        /// </summary>
        private static GeodesicResult AntipodalInverse(
            double lat1Deg, double lon1Deg, double lat2Deg, double lon2Deg)
        {
            // Total length of the two legs through the waypoint at azimuth t from P1.
            double Through(double azimuthDeg)
            {
                var (mLat, mLon) = QuarterWayPoint(lat1Deg, lon1Deg, azimuthDeg);
                return LegMeters(lat1Deg, lon1Deg, mLat, mLon)
                     + LegMeters(mLat, mLon, lat2Deg, lon2Deg);
            }

            // Coarse sweep: 5° steps around the full circle of candidate waypoints.
            var bestAzimuth = 0.0;
            var best = double.MaxValue;
            for (var azimuth = 0.0; azimuth < 360.0; azimuth += 5.0)
            {
                var total = Through(azimuth);
                if (total < best)
                {
                    best = total;
                    bestAzimuth = azimuth;
                }
            }

            // Golden-section refinement within the bracketing 10° window.
            var low = bestAzimuth - 5.0;
            var high = bestAzimuth + 5.0;
            const double Phi = 0.618033988749895;
            var c = high - Phi * (high - low);
            var d = low + Phi * (high - low);
            var fc = Through(c);
            var fd = Through(d);

            for (var i = 0; i < 60 && high - low > 1e-9; i++)
            {
                if (fc < fd)
                {
                    high = d; d = c; fd = fc;
                    c = high - Phi * (high - low);
                    fc = Through(c);
                }
                else
                {
                    low = c; c = d; fc = fd;
                    d = low + Phi * (high - low);
                    fd = Through(d);
                }
            }

            var azimuthOfGeodesic = (low + high) / 2.0;
            var (waypointLat, waypointLon) = QuarterWayPoint(lat1Deg, lon1Deg, azimuthOfGeodesic);

            var first = Inverse(lat1Deg, lon1Deg, waypointLat, waypointLon, allowAntipodalFallback: false);
            var second = Inverse(waypointLat, waypointLon, lat2Deg, lon2Deg, allowAntipodalFallback: false);

            // Bearings are those of the legs: leaving P1 on the first, arriving at P2 on the last.
            return new GeodesicResult(
                DistanceMeters: first.DistanceMeters + second.DistanceMeters,
                InitialBearingDeg: first.InitialBearingDeg,
                FinalBearingDeg: second.FinalBearingDeg);
        }

        /// <summary>
        /// A point a quarter of the way around a sphere from the given one, on the given azimuth.
        /// Spherical is enough: it only has to place the waypoint in the right neighbourhood, and
        /// the two ellipsoidal legs measured through it carry the accuracy.
        /// </summary>
        private static (double Lat, double Lon) QuarterWayPoint(double latDeg, double lonDeg, double azimuthDeg)
        {
            var lat = ToRadians(latDeg);
            var azimuth = ToRadians(azimuthDeg);
            const double QuarterCircle = Math.PI / 2.0;

            var destLat = Math.Asin(
                Math.Sin(lat) * Math.Cos(QuarterCircle) +
                Math.Cos(lat) * Math.Sin(QuarterCircle) * Math.Cos(azimuth));

            var destLon = ToRadians(lonDeg) + Math.Atan2(
                Math.Sin(azimuth) * Math.Sin(QuarterCircle) * Math.Cos(lat),
                Math.Cos(QuarterCircle) - Math.Sin(lat) * Math.Sin(destLat));

            return (ToDegrees(destLat), NormalizeDegrees(ToDegrees(destLon) + 180.0) - 180.0);
        }

        /// <summary>One leg of the split, in metres.</summary>
        private static double LegMeters(double lat1, double lon1, double lat2, double lon2) =>
            Inverse(lat1, lon1, lat2, lon2, allowAntipodalFallback: false).DistanceMeters;

        /// <summary>Distance only, in metres.</summary>
        public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2) =>
            Inverse(lat1, lon1, lat2, lon2).DistanceMeters;
    }
}
