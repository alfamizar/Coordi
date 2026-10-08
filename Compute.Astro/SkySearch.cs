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
    /// Everything a <see cref="SkySearch.Search"/> learned: the moments it was asked for, and how near
    /// it came when there are none.
    /// </summary>
    /// <param name="Alignments">Every crossing inside the altitude band, in order; at most <c>maxResults</c> of them.</param>
    /// <param name="Closest">
    /// Of every crossing of the azimuth the search went through, the one whose altitude came nearest
    /// the one asked for, inside the band or not; null when the body never crossed the azimuth.
    ///
    /// What turns an empty answer into a useful one. "No matches" leaves the reader guessing whether
    /// the band was a hair too narrow or the Sun never gets within thirty degrees of it; the nearest
    /// crossing says which, and when.
    /// </param>
    /// <param name="Truncated">True when the search stopped at <c>maxResults</c> with part of the window still unsearched.</param>
    public sealed record SkySearchResult(
        IReadOnlyList<SkyAlignment> Alignments,
        SkyAlignment? Closest,
        bool Truncated);

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

        /// <summary>The moments of a <see cref="Search"/>, for a caller that has no use for the rest of its answer.</summary>
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
            int maxResults = 100) =>
            Search(
                body, startJdUt, endJdUt, latitudeDeg, longitudeEastDeg,
                azimuthDeg, altitudeDeg, altitudeTolDeg, applyRefraction, maxResults).Alignments;

        /// <summary>
        /// Every crossing of <paramref name="azimuthDeg"/> from <paramref name="startJdUt"/> to
        /// <paramref name="endJdUt"/>, each refined, and the ones with an altitude within
        /// <paramref name="altitudeTolDeg"/> of <paramref name="altitudeDeg"/> kept.
        ///
        /// <paramref name="maxResults"/> ends the scan once that many are kept. It is not what the
        /// search costs: every crossing is refined before its altitude is looked at, so a caller that
        /// wants a whole window counted pays nothing extra for asking for all of it.
        /// </summary>
        public static SkySearchResult Search(
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

            SkyAlignment AlignmentAt(double jd, Horizontal at) => new SkyAlignment(
                JdUt: jd,
                AzimuthDeg: at.AzimuthDeg,
                AltitudeDeg: at.AltitudeDeg,
                Illumination: body == SkyBody.Moon ? Moon.IlluminatedFraction(jd) : (double?)null);

            var results = new List<SkyAlignment>();
            var closestJd = 0.0;
            Horizontal? closestAt = null;
            var t = startJdUt;
            var prev = AzOffset(t);
            while (t < endJdUt && results.Count < maxResults)
            {
                // The last step stops at the end of the window, so nothing past it is found.
                var tNext = Math.Min(t + CoarseStepDays, endJdUt);
                var cur = AzOffset(tNext);
                if (CrossesTarget(prev, cur))
                {
                    var jdCross = Refine(t, tNext, prev, AzOffset);
                    var at = HorizontalAt(jdCross);
                    var miss = Math.Abs(at.AltitudeDeg - altitudeDeg);
                    if (closestAt is null || miss < Math.Abs(closestAt.Value.AltitudeDeg - altitudeDeg))
                    {
                        closestJd = jdCross;
                        closestAt = at;
                    }

                    if (miss <= altitudeTolDeg)
                    {
                        results.Add(AlignmentAt(jdCross, at));
                    }
                }

                prev = cur;
                t = tNext;
            }

            return new SkySearchResult(
                Alignments: results,
                Closest: closestAt is { } closest ? AlignmentAt(closestJd, closest) : (SkyAlignment?)null,
                Truncated: t < endJdUt);
        }

        /// <summary>
        /// Whether the wrapped offset from the target azimuth changed sign between two samples at the
        /// target itself. It also changes sign at the antipode, where it wraps from +180° to −180°;
        /// near the target both samples are small.
        /// </summary>
        private static bool CrossesTarget(double prev, double cur) =>
            prev * cur <= 0.0 && Math.Abs(prev) < 90.0 && Math.Abs(cur) < 90.0 && (prev != 0.0 || cur != 0.0);

        /// <summary>
        /// The instant between <paramref name="lo"/> and <paramref name="hi"/> where
        /// <paramref name="offset"/> is zero, by bisection. Thirty halvings of the 8-minute step leave
        /// less than a microsecond.
        /// </summary>
        private static double Refine(double lo, double hi, double offsetAtLo, Func<double, double> offset)
        {
            var a = lo;
            var b = hi;
            var fa = offsetAtLo;
            for (var i = 0; i < 30; i++)
            {
                var mid = (a + b) / 2;
                var fMid = offset(mid);
                if (fa * fMid <= 0.0)
                {
                    b = mid;
                }
                else
                {
                    a = mid;
                    fa = fMid;
                }
            }

            return (a + b) / 2;
        }
    }
}
