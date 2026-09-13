using System;
using System.Collections.Generic;
using System.Linq;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Geocentric classification of a solar eclipse.</summary>
    public enum SolarEclipseType
    {
        /// <summary>The umbra misses the Earth entirely.</summary>
        Partial,

        /// <summary>The Moon is too far to cover the Sun; a ring remains.</summary>
        Annular,

        /// <summary>The umbra reaches the surface.</summary>
        Total,

        /// <summary>Annular at the ends of the track, total in the middle.</summary>
        Hybrid,
    }

    /// <summary>Geocentric classification of a lunar eclipse.</summary>
    public enum LunarEclipseType
    {
        /// <summary>The Moon enters only the penumbra.</summary>
        Penumbral,

        /// <summary>The Moon partly enters the umbra.</summary>
        Partial,

        /// <summary>The Moon is entirely inside the umbra.</summary>
        Total,
    }

    /// <summary>A point on a solar eclipse's central line (longitude positive east).</summary>
    public readonly record struct EclipsePathPoint(double LatDeg, double LonEastDeg);

    /// <summary>
    /// Geocentric circumstances of a solar eclipse. <paramref name="JdeMaximum"/> is the time of
    /// greatest eclipse in Dynamical Time; <paramref name="Gamma"/> is the least distance of the
    /// shadow axis from Earth's centre (Earth radii); <paramref name="Magnitude"/> is meaningful for
    /// <see cref="SolarEclipseType.Partial"/> (for central eclipses it is reported as 1.0).
    /// </summary>
    public sealed record SolarEclipse(
        double JdeMaximum,
        double Gamma,
        double U,
        SolarEclipseType Type,
        double Magnitude);

    /// <summary>
    /// Reduced-scope local view of a solar eclipse: the <b>geocentric</b> global eclipse
    /// (type, magnitude, instant of greatest eclipse in UTC) plus whether it is above the
    /// horizon — i.e. potentially visible — from one location at that instant.
    ///
    /// This deliberately omits <i>local</i> contact times; for those use
    /// <see cref="Eclipse.SolarLocalCircumstances"/>.
    /// </summary>
    /// <param name="Type">Geocentric eclipse type.</param>
    /// <param name="Magnitude">Geocentric magnitude (reported as 1.0 for central eclipses).</param>
    /// <param name="Gamma">Least distance of the shadow axis from Earth's centre (Earth radii).</param>
    /// <param name="MaximumJdUtc">Instant of greatest eclipse (global), as a Julian Day in UTC.</param>
    /// <param name="SunAltitudeAtMaxDeg">Sun's altitude at the observer at greatest eclipse (degrees).</param>
    /// <param name="VisibleAtMaximum">True when the Sun is above the horizon at the observer at greatest eclipse.</param>
    public sealed record SolarEclipseLocal(
        SolarEclipseType Type,
        double Magnitude,
        double Gamma,
        double MaximumJdUtc,
        double SunAltitudeAtMaxDeg,
        bool VisibleAtMaximum);

    /// <summary>Geocentric circumstances of a lunar eclipse (Dynamical Time at greatest eclipse).</summary>
    public sealed record LunarEclipse(
        double JdeMaximum,
        double Gamma,
        double U,
        LunarEclipseType Type,
        double UmbralMagnitude,
        double PenumbralMagnitude);

    /// <summary>
    /// Contact times of a lunar eclipse as Julian Days in <b>UTC</b>.
    ///
    /// A lunar eclipse looks essentially identical for every observer on Earth's night
    /// side, so these instants are global; whether a given contact is <i>visible</i> from a
    /// place depends only on the Moon being above the horizon then (see <see cref="MoonRiseSet"/>).
    /// Phases absent for the eclipse <paramref name="Type"/> are null — a penumbral eclipse has no
    /// umbral (partial) or total contacts; a partial eclipse has no total contacts.
    /// </summary>
    /// <param name="Type">Geocentric eclipse type.</param>
    /// <param name="Gamma">Least distance of the Moon's centre from the shadow axis (Earth radii).</param>
    /// <param name="UmbralMagnitude">Umbral magnitude.</param>
    /// <param name="PenumbralMagnitude">Penumbral magnitude.</param>
    /// <param name="PenumbralBeginJdUtc">P1 — penumbra first contact.</param>
    /// <param name="PartialBeginJdUtc">U1 — umbra first contact (partial phase begins).</param>
    /// <param name="TotalBeginJdUtc">U2 — totality begins.</param>
    /// <param name="MaximumJdUtc">Greatest eclipse (mid).</param>
    /// <param name="TotalEndJdUtc">U3 — totality ends.</param>
    /// <param name="PartialEndJdUtc">U4 — umbra last contact (partial phase ends).</param>
    /// <param name="PenumbralEndJdUtc">P4 — penumbra last contact.</param>
    public sealed record LunarEclipseCircumstances(
        LunarEclipseType Type,
        double Gamma,
        double UmbralMagnitude,
        double PenumbralMagnitude,
        double? PenumbralBeginJdUtc,
        double? PartialBeginJdUtc,
        double? TotalBeginJdUtc,
        double MaximumJdUtc,
        double? TotalEndJdUtc,
        double? PartialEndJdUtc,
        double? PenumbralEndJdUtc);

    /// <summary>
    /// Solar and lunar eclipse prediction, <b>geocentric</b> (the global catalogue: when an
    /// eclipse happens, its type, magnitude, and the instant of greatest eclipse), plus
    /// location-specific reductions.
    ///
    /// Clean-room from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 54. Time of
    /// maximum is accurate to ~1–2 min.
    ///
    /// Verified: Example 54.a (1993-05-21, γ=1.1348, u=0.0097, mag=0.740, partial) plus
    /// real eclipses — 2017-08-21 &amp; 2024-04-08 total solar (γ=0.4364 / 0.3437),
    /// 2019-01-21 total lunar (umbral mag 1.193), 2024-09-18 partial lunar (0.078).
    /// </summary>
    public static class Eclipse
    {
        private readonly struct Common
        {
            public Common(double jde, double gamma, double u, double moonAnomalyDeg)
            {
                Jde = jde;
                Gamma = gamma;
                U = u;
                MoonAnomalyDeg = moonAnomalyDeg;
            }

            public double Jde { get; }

            public double Gamma { get; }

            public double U { get; }

            public double MoonAnomalyDeg { get; }
        }

        /// <summary>Returns null when no eclipse is possible at this phase (|sin F| &gt; 0.36).</summary>
        private static Common? CommonAt(double k)
        {
            var t = k / 1236.85;
            var f = NormalizeDegrees(160.7108 + 390.67050284 * k - 0.0016118 * t * t -
                0.00000227 * t * t * t + 0.000000011 * t * t * t * t);
            if (Math.Abs(Math.Sin(ToRadians(f))) > 0.36) return null;

            var mean = 2451550.09766 + 29.530588861 * k +
                       0.00015437 * t * t - 0.000000150 * t * t * t + 0.00000000073 * t * t * t * t;
            var e = 1.0 - 0.002516 * t - 0.0000074 * t * t;
            var m = NormalizeDegrees(2.5534 + 29.10535670 * k - 0.0000014 * t * t - 0.00000011 * t * t * t);
            var mp = NormalizeDegrees(201.5643 + 385.81693528 * k + 0.0107582 * t * t +
                0.00001238 * t * t * t - 0.000000058 * t * t * t * t);
            var omega = NormalizeDegrees(124.7746 - 1.56375588 * k + 0.0020672 * t * t + 0.00000215 * t * t * t);
            var f1 = NormalizeDegrees(f - 0.02665 * Math.Sin(ToRadians(omega)));
            var a1 = NormalizeDegrees(299.77 + 0.107408 * k - 0.009173 * t * t);

            double S(double x) => Math.Sin(ToRadians(x));
            double C(double x) => Math.Cos(ToRadians(x));

            var dJde = -0.4075 * S(mp) + 0.1721 * e * S(m) + 0.0161 * S(2 * mp) -
                       0.0097 * S(2 * f1) + 0.0073 * e * S(mp - m) - 0.0050 * e * S(mp + m) -
                       0.0023 * S(mp - 2 * f1) + 0.0021 * e * S(2 * m) + 0.0012 * S(mp + 2 * f1) +
                       0.0006 * e * S(2 * mp + m) - 0.0004 * S(3 * mp) - 0.0003 * e * S(m + 2 * f1) +
                       0.0003 * S(a1) - 0.0002 * e * S(m - 2 * f1) - 0.0002 * e * S(2 * mp - m) -
                       0.0002 * S(omega);

            var p = 0.2070 * e * S(m) + 0.0024 * e * S(2 * m) - 0.0392 * S(mp) +
                    0.0116 * S(2 * mp) - 0.0073 * e * S(mp + m) + 0.0067 * e * S(mp - m) +
                    0.0118 * S(2 * f1);
            var q = 5.2207 - 0.0048 * e * C(m) + 0.0020 * e * C(2 * m) - 0.3299 * C(mp) -
                    0.0060 * e * C(mp + m) + 0.0041 * e * C(mp - m);
            var w = Math.Abs(C(f1));
            var gamma = (p * C(f1) + q * S(f1)) * (1.0 - 0.0048 * w);
            var u = 0.0059 + 0.0046 * e * C(m) - 0.0182 * C(mp) + 0.0004 * C(2 * mp) -
                    0.0005 * e * C(m + mp);

            return new Common(mean + dJde, gamma, u, mp);
        }

        /// <summary>Solar eclipse at the new moon of the given lunation, or null if none.</summary>
        public static SolarEclipse? SolarNear(int lunation)
        {
            var maybe = CommonAt(lunation);
            if (maybe == null) return null;
            var cm = maybe.Value;
            var g = Math.Abs(cm.Gamma);
            if (g > 1.5433 + cm.U) return null;

            SolarEclipseType type;
            double magnitude;
            if (g < 0.9972)
            {
                if (cm.U < 0.0) type = SolarEclipseType.Total;
                else if (cm.U > 0.0047) type = SolarEclipseType.Annular;
                else if (cm.U < 0.00464 * Math.Sqrt(Math.Max(1.0 - cm.Gamma * cm.Gamma, 0.0))) type = SolarEclipseType.Hybrid;
                else type = SolarEclipseType.Annular;
                magnitude = 1.0;
            }
            else if (g < 0.9972 + Math.Abs(cm.U))
            {
                // Meeus p. 381: for 0.9972 ≤ |γ| < 0.9972 + |u| the shadow axis misses
                // Earth's centre line but the umbra still grazes the surface — a
                // NON-CENTRAL total or annular eclipse (e.g. 2014-04-29 annular).
                type = cm.U < 0.0 ? SolarEclipseType.Total : SolarEclipseType.Annular;
                magnitude = 1.0;
            }
            else
            {
                type = SolarEclipseType.Partial;
                magnitude = (1.5433 + cm.U - g) / (0.5461 + 2.0 * cm.U);
            }

            return new SolarEclipse(cm.Jde, cm.Gamma, cm.U, type, magnitude);
        }

        /// <summary>Lunar eclipse at the full moon of the given lunation, or null if none.</summary>
        public static LunarEclipse? LunarNear(int lunation)
        {
            var maybe = CommonAt(lunation + 0.5);
            if (maybe == null) return null;
            var cm = maybe.Value;
            var g = Math.Abs(cm.Gamma);
            var umbral = (1.0128 - cm.U - g) / 0.5450;
            var penumbral = (1.5573 + cm.U - g) / 0.5450;
            if (penumbral < 0.0) return null;
            var type = umbral >= 1.0
                ? LunarEclipseType.Total
                : (umbral > 0.0 ? LunarEclipseType.Partial : LunarEclipseType.Penumbral);
            return new LunarEclipse(cm.Jde, cm.Gamma, cm.U, type, umbral, penumbral);
        }

        /// <summary>
        /// Full set of UTC contact times for the lunar eclipse at the given lunation, or
        /// null if there is none. Semidurations from Meeus, ch. 54 (p. 380–382): the three
        /// shadow radii and the Moon's hourly motion factor <c>n</c>, applied symmetrically
        /// about greatest eclipse.
        /// </summary>
        public static LunarEclipseCircumstances? LunarCircumstances(int lunation)
        {
            var maybe = CommonAt(lunation + 0.5);
            if (maybe == null) return null;
            var cm = maybe.Value;
            var g = Math.Abs(cm.Gamma);
            var umbral = (1.0128 - cm.U - g) / 0.5450;
            var penumbral = (1.5573 + cm.U - g) / 0.5450;
            if (penumbral < 0.0) return null;
            var type = umbral >= 1.0
                ? LunarEclipseType.Total
                : (umbral > 0.0 ? LunarEclipseType.Partial : LunarEclipseType.Penumbral);

            var n = 0.5458 + 0.0400 * Math.Cos(ToRadians(cm.MoonAnomalyDeg));
            var penumbraRadius = 1.5573 + cm.U;
            var umbraRadius = 1.0128 - cm.U;
            var totalRadius = 0.4678 - cm.U;

            double? SemidurationDays(double radius)
            {
                var disc = radius * radius - cm.Gamma * cm.Gamma;
                if (disc <= 0.0) return null;
                return 60.0 / n * Math.Sqrt(disc) / 1440.0; // minutes → days
            }

            var maxUtc = TdToUtc(cm.Jde);
            var penSemi = SemidurationDays(penumbraRadius);
            var parSemi = type != LunarEclipseType.Penumbral ? SemidurationDays(umbraRadius) : null;
            var totSemi = type == LunarEclipseType.Total ? SemidurationDays(totalRadius) : null;

            return new LunarEclipseCircumstances(
                Type: type,
                Gamma: cm.Gamma,
                UmbralMagnitude: umbral,
                PenumbralMagnitude: penumbral,
                PenumbralBeginJdUtc: penSemi.HasValue ? maxUtc - penSemi.Value : (double?)null,
                PartialBeginJdUtc: parSemi.HasValue ? maxUtc - parSemi.Value : (double?)null,
                TotalBeginJdUtc: totSemi.HasValue ? maxUtc - totSemi.Value : (double?)null,
                MaximumJdUtc: maxUtc,
                TotalEndJdUtc: totSemi.HasValue ? maxUtc + totSemi.Value : (double?)null,
                PartialEndJdUtc: parSemi.HasValue ? maxUtc + parSemi.Value : (double?)null,
                PenumbralEndJdUtc: penSemi.HasValue ? maxUtc + penSemi.Value : (double?)null);
        }

        /// <summary>Contact times for every lunar eclipse whose maximum falls in the UTC interval.</summary>
        public static IReadOnlyList<LunarEclipseCircumstances> LunarEclipseCircumstancesBetween(double startJd, double endJd) =>
            LunationRange(startJd, endJd)
                .Select(LunarCircumstances)
                .Where(c => c != null)
                .Select(c => c!)
                .Where(c => c.MaximumJdUtc >= startJd && c.MaximumJdUtc <= endJd)
                .ToList();

        /// <summary>
        /// Reduced-scope solar eclipse for one location: the geocentric global eclipse
        /// plus the Sun's altitude / visibility at greatest eclipse from (lat, lon).
        /// Longitude is positive east. Returns null if no solar eclipse at this lunation.
        /// </summary>
        public static SolarEclipseLocal? SolarCircumstances(int lunation, double latDeg, double longitudeEastDeg)
        {
            var se = SolarNear(lunation);
            if (se == null) return null;
            var maxUtc = TdToUtc(se.JdeMaximum);
            // Sun position is a function of Dynamical Time; sidereal time of Universal Time.
            var pos = Sun.PositionAt(se.JdeMaximum);
            var hourAngle = HorizontalCoordinates.HourAngleDeg(maxUtc, pos.RightAscension, longitudeEastDeg);
            var altitude = HorizontalCoordinates.FromHourAngle(hourAngle, pos.Declination, latDeg).AltitudeDeg;
            return new SolarEclipseLocal(se.Type, se.Magnitude, se.Gamma, maxUtc, altitude, altitude > 0.0);
        }

        /// <summary>Reduced-scope solar eclipses (for a location) whose maximum falls in the UTC interval.</summary>
        public static IReadOnlyList<SolarEclipseLocal> SolarEclipseCircumstancesBetween(
            double startJd,
            double endJd,
            double latDeg,
            double longitudeEastDeg) =>
            LunationRange(startJd, endJd)
                .Select(lun => SolarCircumstances(lun, latDeg, longitudeEastDeg))
                .Where(c => c != null)
                .Select(c => c!)
                .Where(c => c.MaximumJdUtc >= startJd && c.MaximumJdUtc <= endJd)
                .ToList();

        /// <summary>
        /// <b>Full local circumstances</b> of the solar eclipse at this lunation as seen from
        /// (lat, lonEast) — location-specific contact times (partial begin/end, annular/total
        /// begin/end), local greatest eclipse, magnitude and central-phase duration. This is
        /// the full-fidelity equivalent of CoordinateSharp's <c>SolarEclipseDetails</c>; prefer it
        /// over the reduced-scope <see cref="SolarCircumstances"/>. Longitude is positive east.
        /// Returns null when there is no solar eclipse at this lunation. Contact times accurate
        /// to ~1–2 min (ephemeris-limited).
        /// </summary>
        public static SolarEclipseLocalCircumstances? SolarLocalCircumstances(
            int lunation,
            double latDeg,
            double longitudeEastDeg)
        {
            var se = SolarNear(lunation);
            if (se == null) return null;
            var cal = AstroTime.CalendarFromJulianDay(se.JdeMaximum);
            var deltaTDays = AstroTime.DeltaTSeconds(cal.Year, cal.Month) / 86400.0;
            return BesselianSolar.Reduce(se, se.JdeMaximum - deltaTDays, deltaTDays, latDeg, longitudeEastDeg);
        }

        /// <summary>
        /// Full local circumstances (for a location) of every solar eclipse whose global
        /// greatest eclipse falls within the UTC interval. Eclipses not visible from the site
        /// are still returned, with <see cref="SolarEclipseLocalCircumstances.LocalType"/> ==
        /// <see cref="SolarEclipseLocalType.None"/>.
        /// </summary>
        public static IReadOnlyList<SolarEclipseLocalCircumstances> SolarLocalCircumstancesBetween(
            double startJd,
            double endJd,
            double latDeg,
            double longitudeEastDeg) =>
            LunationRange(startJd, endJd)
                .Select(lun => SolarLocalCircumstances(lun, latDeg, longitudeEastDeg))
                .Where(c => c != null)
                .Select(c => c!)
                .Where(c => c.GlobalMaximumJdUtc >= startJd && c.GlobalMaximumJdUtc <= endJd)
                .ToList();

        /// <summary>
        /// The central line (umbral/antumbral ground track) of the solar eclipse nearest
        /// <paramref name="lunation"/>, for map overlays. Empty for a purely partial eclipse. The points
        /// are ordered in time from the shadow's first landfall to its last, so a renderer can draw
        /// them as a polyline.
        /// </summary>
        public static IReadOnlyList<EclipsePathPoint> SolarCentralPath(int lunation)
        {
            var se = SolarNear(lunation);
            if (se == null) return Array.Empty<EclipsePathPoint>();
            var cal = AstroTime.CalendarFromJulianDay(se.JdeMaximum);
            var deltaTDays = AstroTime.DeltaTSeconds(cal.Year, cal.Month) / 86400.0;
            return BesselianSolar.CentralLine(se, deltaTDays)
                .Select(p => new EclipsePathPoint(p.LatDeg, p.LonEastDeg))
                .ToList();
        }

        /// <summary><see cref="SolarCentralPath"/> for the eclipse nearest the given UTC Julian Day.</summary>
        public static IReadOnlyList<EclipsePathPoint> SolarCentralPathNear(double jdUtc) =>
            SolarCentralPath(MoonPhase.NearestLunation(jdUtc));

        /// <summary>All solar eclipses whose maximum falls within the Julian-Day interval.</summary>
        public static IReadOnlyList<SolarEclipse> SolarEclipsesBetween(double startJd, double endJd) =>
            LunationRange(startJd, endJd)
                .Select(SolarNear)
                .Where(e => e != null)
                .Select(e => e!)
                .Where(e => e.JdeMaximum >= startJd && e.JdeMaximum <= endJd)
                .ToList();

        /// <summary>All lunar eclipses whose maximum falls within the Julian-Day interval.</summary>
        public static IReadOnlyList<LunarEclipse> LunarEclipsesBetween(double startJd, double endJd) =>
            LunationRange(startJd, endJd)
                .Select(LunarNear)
                .Where(e => e != null)
                .Select(e => e!)
                .Where(e => e.JdeMaximum >= startJd && e.JdeMaximum <= endJd)
                .ToList();

        /// <summary>Dynamical Time → UTC (removes ΔT for the eclipse's month).</summary>
        private static double TdToUtc(double jde)
        {
            var cal = AstroTime.CalendarFromJulianDay(jde);
            return jde - AstroTime.DeltaTSeconds(cal.Year, cal.Month) / 86400.0;
        }

        private static IEnumerable<int> LunationRange(double startJd, double endJd)
        {
            var first = (int)Math.Floor((startJd - 2451550.09766) / 29.530588861) - 1;
            var last = (int)Math.Floor((endJd - 2451550.09766) / 29.530588861) + 1;
            for (var k = first; k <= last; k++) yield return k;
        }
    }
}
