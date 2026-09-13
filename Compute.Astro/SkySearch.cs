using System;
using System.Collections.Generic;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Which body a sky-alignment search tracks.</summary>
    public enum SkyBody
    {
        /// <summary>The Sun.</summary>
        Sun,

        /// <summary>The Moon.</summary>
        Moon,
    }

    /// <summary>One moment when the body crosses the requested sky position.</summary>
    /// <param name="JdUt">The crossing instant, Julian Day UTC.</param>
    /// <param name="AzimuthDeg">Azimuth at the instant (≈ the requested azimuth, degrees from N, clockwise).</param>
    /// <param name="AltitudeDeg">Altitude at the instant (degrees; apparent when refraction was requested).</param>
    /// <param name="Illumination">Moon only: illuminated fraction 0..1 at the instant; null for the Sun.</param>
    public readonly record struct SkyAlignment(
        double JdUt,
        double AzimuthDeg,
        double AltitudeDeg,
        double? Illumination);

    /// <summary>
    /// "When is the Sun/Moon exactly <i>there</i>?" — the planning search behind shots like a full moon
    /// setting behind a landmark: given an observer and a target direction, find every moment in a
    /// date window when the body crosses that azimuth with its altitude inside a tolerance band.
    ///
    /// Runs on the same validated ephemeris as everything else (<see cref="HorizontalCoordinates"/>,
    /// topocentric Moon, optional refraction — what a camera actually sees near the horizon). A coarse
    /// scan detects azimuth crossings (wrapped sign change), each refined by bisection to sub-second
    /// precision, then filtered by the altitude band.
    ///
    /// Limitation: near-zenith passes (tropics, altitude ≈ 90°) sweep azimuth too fast for the
    /// coarse scan and are intentionally ignored — not a composition-alignment use case.
    /// </summary>
    public static class SkySearch
    {
        private const double CoarseStepDays = 8.0 / 1440.0; // 8 minutes

        /// <summary>Finds every crossing of the target azimuth within the altitude tolerance band.</summary>
        public static IReadOnlyList<SkyAlignment> FindAlignments(
            SkyBody body,
            double startJdUt,
            double endJdUt,
            double latitudeDeg,
            double longitudeEastDeg,
            double azimuthDeg,
            double altitudeDeg,
            double altitudeTolDeg,
            bool applyRefraction = true,
            int maxResults = 100)
        {
            Horizontal HorizontalAt(double jd) => body == SkyBody.Sun
                ? HorizontalCoordinates.OfSun(jd, latitudeDeg, longitudeEastDeg, applyRefraction)
                : HorizontalCoordinates.OfMoon(jd, latitudeDeg, longitudeEastDeg, topocentric: true, applyRefraction: applyRefraction);

            double AzOffset(double jd) => NormalizeDegreesSigned(HorizontalAt(jd).AzimuthDeg - azimuthDeg);

            var results = new List<SkyAlignment>();
            var t = startJdUt;
            var prev = AzOffset(t);
            while (t < endJdUt && results.Count < maxResults)
            {
                var tNext = t + CoarseStepDays;
                var cur = AzOffset(tNext);
                // A sign change of the wrapped offset happens at the target azimuth or at its
                // antipode (the ±180° wrap); near the target both samples are small.
                if (prev * cur <= 0.0 && Math.Abs(prev) < 90.0 && Math.Abs(cur) < 90.0 && (prev != 0.0 || cur != 0.0))
                {
                    var lo = t;
                    var hi = tNext;
                    var fLo = prev;
                    for (var i = 0; i < 30; i++)
                    {
                        var mid = (lo + hi) / 2;
                        var fMid = AzOffset(mid);
                        if (fLo * fMid <= 0.0)
                        {
                            hi = mid;
                        }
                        else
                        {
                            lo = mid;
                            fLo = fMid;
                        }
                    }

                    var jdCross = (lo + hi) / 2;
                    var at = HorizontalAt(jdCross);
                    if (Math.Abs(at.AltitudeDeg - altitudeDeg) <= altitudeTolDeg)
                    {
                        results.Add(new SkyAlignment(
                            JdUt: jdCross,
                            AzimuthDeg: at.AzimuthDeg,
                            AltitudeDeg: at.AltitudeDeg,
                            Illumination: body == SkyBody.Moon ? Moon.IlluminatedFraction(jdCross) : (double?)null));
                    }
                }

                prev = cur;
                t = tNext;
            }

            return results;
        }
    }
}
