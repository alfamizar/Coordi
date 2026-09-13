using System;
using System.Collections.Generic;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Local solar eclipse as seen from one site.</summary>
    public enum SolarEclipseLocalType
    {
        /// <summary>No eclipse visible from this site.</summary>
        None,

        /// <summary>Only the partial phase is visible here.</summary>
        Partial,

        /// <summary>The antumbra passes over this site.</summary>
        Annular,

        /// <summary>The umbra passes over this site.</summary>
        Total,
    }

    /// <summary>
    /// <b>Local</b> circumstances of a solar eclipse for one observer: the location-specific
    /// contact times (partial begin/end, annular/total begin/end), local greatest eclipse,
    /// magnitude, central-phase duration, and Sun altitude — the full-fidelity equivalent
    /// of CoordinateSharp's <c>SolarEclipseDetails</c>.
    ///
    /// Contact times are Julian Days in <b>UTC</b>. Fields that don't apply at this site are
    /// null: a site that sees only a partial eclipse has no <paramref name="CentralBeginJdUtc"/> /
    /// <paramref name="CentralEndJdUtc"/> / <paramref name="CentralDurationSeconds"/>; a site outside
    /// the penumbra has <paramref name="LocalType"/> == <see cref="SolarEclipseLocalType.None"/> and
    /// null contacts (only the global fields are populated).
    /// </summary>
    /// <param name="GlobalType">Global (geocentric) type of the eclipse.</param>
    /// <param name="GlobalMagnitude">Global (geocentric) magnitude.</param>
    /// <param name="GlobalMaximumJdUtc">Instant of global greatest eclipse, Julian Day in UTC. Always present.</param>
    /// <param name="LocalType">Type of eclipse actually seen from this site.</param>
    /// <param name="MagnitudeAtMax">Fraction of the Sun's diameter covered at local maximum; 0 if not visible.</param>
    /// <param name="SunAltitudeAtMaxDeg">Sun's altitude (degrees) at local maximum, or at global maximum when not visible.</param>
    /// <param name="Visible">True when at least a partial eclipse occurs with the Sun above the horizon.</param>
    /// <param name="PartialBeginJdUtc">C1 — first contact, partial phase begins.</param>
    /// <param name="MaximumJdUtc">Local greatest eclipse.</param>
    /// <param name="PartialEndJdUtc">C4 — last contact, partial phase ends.</param>
    /// <param name="CentralBeginJdUtc">C2 — annular/total (central) phase begins.</param>
    /// <param name="CentralEndJdUtc">C3 — annular/total (central) phase ends.</param>
    /// <param name="CentralDurationSeconds">Duration of the annular/total phase in seconds (C3 − C2), or null if not central here.</param>
    public sealed record SolarEclipseLocalCircumstances(
        SolarEclipseType GlobalType,
        double GlobalMagnitude,
        double GlobalMaximumJdUtc,
        SolarEclipseLocalType LocalType,
        double MagnitudeAtMax,
        double SunAltitudeAtMaxDeg,
        bool Visible,
        double? PartialBeginJdUtc,
        double? MaximumJdUtc,
        double? PartialEndJdUtc,
        double? CentralBeginJdUtc,
        double? CentralEndJdUtc,
        double? CentralDurationSeconds);

    /// <summary>
    /// Besselian-element reduction for <b>local</b> solar eclipse circumstances.
    ///
    /// Clean-room from the standard treatment (Explanatory Supplement to the Astronomical
    /// Almanac; J. Meeus, <i>Elements of Solar Eclipses</i>). The Besselian elements (axis
    /// direction a/d, fundamental-plane coordinates x/y, shadow-cone radii l1/l2 and
    /// half-angles f1/f2) are computed <i>on the fly</i> from this library's own Sun and Moon
    /// ephemeris (<see cref="Sun.PositionAt"/>, <see cref="Moon.PositionAt"/>) rather than a
    /// precomputed table, so the engine stays self-contained and works for any date the
    /// ephemeris covers.
    ///
    /// Accuracy of contact times is ephemeris-limited (~1–2 min), matching the library's
    /// geocentric eclipse timing. Validated against NASA local-circumstances pages for the
    /// 2017-08-21 and 2024-04-08 total solar eclipses.
    /// </summary>
    internal static class BesselianSolar
    {
        private const double EarthRadiusKm = 6378.14;
        private const double AuKm = 149_597_870.7;
        private const double SunRadiusKm = 696_000.0;

        /// <summary>Moon-to-Earth radius ratio k (IAU adopted value used by NASA's canon).</summary>
        private const double KMoon = 0.2725076;

        /// <summary>Earth flattening for the geocentric observer reduction (WGS84-class).</summary>
        private const double Flattening = 1.0 / 298.257;

        /// <summary>Sun radius in Earth radii (constant).</summary>
        private const double SSun = SunRadiusKm / EarthRadiusKm;

        /// <summary>Half-width of the search window around global greatest eclipse (days ≈ ±4.3 h).</summary>
        private const double SearchHalfDays = 0.18;

        private const double StepDays = 0.5 / 1440.0; // 0.5-minute grid

        /// <summary>Observer's instantaneous shadow geometry at one instant.</summary>
        private readonly struct State
        {
            public State(double m, double l1P, double l2P, double magnitude, double sunAltitudeDeg)
            {
                M = m;
                L1P = l1P;
                L2P = l2P;
                Magnitude = magnitude;
                SunAltitudeDeg = sunAltitudeDeg;
            }

            /// <summary>Distance of the observer from the shadow axis, on the fundamental plane (Earth radii).</summary>
            public double M { get; }

            /// <summary>Penumbral radius reduced to the observer (Earth radii).</summary>
            public double L1P { get; }

            /// <summary>Umbral radius reduced to the observer (signed: &lt; 0 total, &gt; 0 annular).</summary>
            public double L2P { get; }

            /// <summary>Eclipse magnitude (fraction of the Sun's diameter covered).</summary>
            public double Magnitude { get; }

            /// <summary>Sun's altitude at the observer (degrees).</summary>
            public double SunAltitudeDeg { get; }
        }

        /// <summary>A geographic point on the eclipse ground track.</summary>
        public readonly record struct GroundPoint(double LatDeg, double LonEastDeg);

        /// <summary>
        /// The geographic point where the shadow <b>axis</b> pierces the Earth at Dynamical Time
        /// <paramref name="jdTd"/> — the central-line point — or null when the axis misses the globe
        /// (no central eclipse at that instant). Spherical-Earth intersection with a
        /// geocentric→geodetic latitude conversion: accurate to a few km, ample for a
        /// country-scale path chart.
        /// </summary>
        public static GroundPoint? AxisSubPoint(double jdTd, double deltaTDays)
        {
            var sun = Sun.PositionAt(jdTd);
            var moon = Moon.PositionAt(jdTd);
            var rSun = Sun.RadiusVectorAu(jdTd) * AuKm / EarthRadiusKm;
            var rMoon = moon.DistanceKm / EarthRadiusKm;

            var sunDec = ToRadians(sun.Declination);
            var sunRa = ToRadians(sun.RightAscension);
            var xs = rSun * Math.Cos(sunDec) * Math.Cos(sunRa);
            var ys = rSun * Math.Cos(sunDec) * Math.Sin(sunRa);
            var zs = rSun * Math.Sin(sunDec);
            var moonDec = ToRadians(moon.DeclinationDeg);
            var moonRa = ToRadians(moon.RightAscensionDeg);
            var xm = rMoon * Math.Cos(moonDec) * Math.Cos(moonRa);
            var ym = rMoon * Math.Cos(moonDec) * Math.Sin(moonRa);
            var zm = rMoon * Math.Sin(moonDec);

            var dx = xs - xm;
            var dy = ys - ym;
            var dz = zs - zm;
            var g = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var aRad = Math.Atan2(dy, dx);
            var dRad = Math.Asin(dz / g);
            var sinD = Math.Sin(dRad);
            var cosD = Math.Cos(dRad);

            // Besselian x, y (Moon on the fundamental plane, Earth radii).
            var x = -xm * Math.Sin(aRad) + ym * Math.Cos(aRad);
            var y = -xm * sinD * Math.Cos(aRad) - ym * sinD * Math.Sin(aRad) + zm * cosD;

            // The axis meets the unit sphere only where x² + y² ≤ 1.
            var r2 = x * x + y * y;
            if (r2 > 1.0) return null;
            var zeta = Math.Sqrt(1.0 - r2);

            // Invert the fundamental-plane transform (ξ=x, η=y) for the surface point on the axis.
            var jdUt = jdTd - deltaTDays;
            var muDeg = Nutation.ApparentSiderealTimeDeg(jdUt) - ToDegrees(aRad);
            var b = zeta * cosD - y * sinD;
            var thetaDeg = ToDegrees(Math.Atan2(x, b));
            var lonEast = NormalizeDegreesSigned(thetaDeg - muDeg);
            var rhoCosPhi = Math.Sqrt(x * x + b * b);
            var geocentricLat = ToDegrees(Math.Atan2(y * cosD + zeta * sinD, rhoCosPhi));
            // Geocentric → geodetic latitude (Meeus ch. 11): tan φ = tan φ' / (1 − f)².
            var oneMinusF2 = (1.0 - Flattening) * (1.0 - Flattening);
            var geodeticLat = ToDegrees(Math.Atan(Math.Tan(ToRadians(geocentricLat)) / oneMinusF2));
            return new GroundPoint(geodeticLat, lonEast);
        }

        /// <summary>
        /// The central line (umbral/antumbral ground track) of a solar eclipse: the sub-axis
        /// point sampled minute-by-minute across the global eclipse window, ordered in time.
        /// Empty for a purely partial eclipse (the axis never reaches Earth).
        /// </summary>
        public static IReadOnlyList<GroundPoint> CentralLine(SolarEclipse se, double deltaTDays)
        {
            var output = new List<GroundPoint>();
            var t = se.JdeMaximum - SearchHalfDays;
            var end = se.JdeMaximum + SearchHalfDays;
            const double step = 1.0 / 1440.0; // one minute
            while (t <= end)
            {
                if (AxisSubPoint(t, deltaTDays) is { } point) output.Add(point);
                t += step;
            }

            return output;
        }

        /// <summary>
        /// Computes the observer's shadow geometry at Dynamical Time <paramref name="jdTd"/>.
        /// <paramref name="deltaTDays"/> is ΔT for the eclipse's month (TD − UT, in days); it is
        /// sensibly constant across the few-hour eclipse window so the caller computes it once.
        /// </summary>
        private static State StateAt(double jdTd, double latDeg, double lonEastDeg, double deltaTDays)
        {
            var sun = Sun.PositionAt(jdTd);
            var moon = Moon.PositionAt(jdTd);
            var rSun = Sun.RadiusVectorAu(jdTd) * AuKm / EarthRadiusKm;
            var rMoon = moon.DistanceKm / EarthRadiusKm;

            // Geocentric equatorial rectangular coordinates (Earth radii).
            var sunDec = ToRadians(sun.Declination);
            var sunRa = ToRadians(sun.RightAscension);
            var xs = rSun * Math.Cos(sunDec) * Math.Cos(sunRa);
            var ys = rSun * Math.Cos(sunDec) * Math.Sin(sunRa);
            var zs = rSun * Math.Sin(sunDec);
            var moonDec = ToRadians(moon.DeclinationDeg);
            var moonRa = ToRadians(moon.RightAscensionDeg);
            var xm = rMoon * Math.Cos(moonDec) * Math.Cos(moonRa);
            var ym = rMoon * Math.Cos(moonDec) * Math.Sin(moonRa);
            var zm = rMoon * Math.Sin(moonDec);

            // Shadow axis: directed from the Moon toward the Sun (≈ the Sun's direction).
            var dx = xs - xm;
            var dy = ys - ym;
            var dz = zs - zm;
            var g = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            var aRad = Math.Atan2(dy, dx);
            var dRad = Math.Asin(dz / g);
            var sinA = Math.Sin(aRad);
            var cosA = Math.Cos(aRad);
            var sinD = Math.Sin(dRad);
            var cosD = Math.Cos(dRad);

            // Cone half-angles: external (penumbra, f1) and internal (umbra, f2) tangents.
            var tanF1 = (SSun + KMoon) / g;
            var tanF2 = (SSun - KMoon) / g;
            var cosF1 = Math.Cos(Math.Atan(tanF1));
            var cosF2 = Math.Cos(Math.Atan(tanF2));

            // Moon on the fundamental plane (x, y) and along the axis (z), Earth radii.
            var x = -xm * sinA + ym * cosA;
            var y = -xm * sinD * cosA - ym * sinD * sinA + zm * cosD;
            var z = xm * cosD * cosA + ym * cosD * sinA + zm * sinD;
            var l1 = z * tanF1 + KMoon / cosF1;
            var l2 = z * tanF2 - KMoon / cosF2;

            // Ephemeris hour angle of the axis: μ = apparent sidereal time (UT) − a.
            var jdUt = jdTd - deltaTDays;
            var gast = Nutation.ApparentSiderealTimeDeg(jdUt);
            var mu = gast - ToDegrees(aRad);

            // Observer geocentric coordinates (Meeus ch. 11), sea level.
            var phi = ToRadians(latDeg);
            var uLat = Math.Atan((1.0 - Flattening) * Math.Tan(phi));
            var rhoSinPhi = (1.0 - Flattening) * Math.Sin(uLat);
            var rhoCosPhi = Math.Cos(uLat);

            var theta = ToRadians(NormalizeDegrees(mu + lonEastDeg));
            var xi = rhoCosPhi * Math.Sin(theta);
            var eta = rhoSinPhi * cosD - rhoCosPhi * Math.Cos(theta) * sinD;
            var zeta = rhoSinPhi * sinD + rhoCosPhi * Math.Cos(theta) * cosD;

            var du = x - xi;
            var dv = y - eta;
            var m = Math.Sqrt(du * du + dv * dv);
            var l1P = l1 - zeta * tanF1;
            var l2P = l2 - zeta * tanF2;
            var magnitude = (l1P - m) / (l1P + l2P);

            // Sun altitude at the observer (apparent, geometric — no refraction).
            var altitude = HorizontalCoordinates.FromHourAngle(
                NormalizeDegreesSigned(gast + lonEastDeg - sun.RightAscension),
                sun.Declination,
                latDeg).AltitudeDeg;

            return new State(m, l1P, l2P, magnitude, altitude);
        }

        // An eclipse is only observable while the Sun is above the horizon; gate
        // penumbra/umbra membership on that so night-side points don't register.
        private static double PenOf(State s) => s.SunAltitudeDeg > 0.0 ? s.L1P - s.M : -1.0;      // > 0 ⇒ in penumbra

        private static double UmbOf(State s) => s.SunAltitudeDeg > 0.0 ? Math.Abs(s.L2P) - s.M : -1.0; // > 0 ⇒ in umbra

        /// <summary>
        /// Reduces a geocentric <see cref="SolarEclipse"/> to local circumstances at (lat, lonEast).
        /// <paramref name="globalMaxUtc"/> is the global greatest-eclipse instant already converted to
        /// UTC, <paramref name="deltaTDays"/> is ΔT for the eclipse's month.
        /// </summary>
        public static SolarEclipseLocalCircumstances Reduce(
            SolarEclipse se,
            double globalMaxUtc,
            double deltaTDays,
            double latDeg,
            double lonEastDeg)
        {
            State At(double jdTd) => StateAt(jdTd, latDeg, lonEastDeg, deltaTDays);
            double ToUtc(double jdTd) => jdTd - deltaTDays;

            var start = se.JdeMaximum - SearchHalfDays;
            var end = se.JdeMaximum + SearchHalfDays;

            // Sample magnitude / penumbra & umbra membership across the window.
            var maxMagT = se.JdeMaximum;
            var maxMag = double.NegativeInfinity;
            double? penBeginT = null;
            double? penEndT = null;
            double? umbBeginT = null;
            double? umbEndT = null;

            var prev = start;
            var prevPen = PenOf(At(start));
            var prevUmb = UmbOf(At(start));
            var t = start + StepDays;
            while (t <= end)
            {
                var s = At(t);
                if (s.SunAltitudeDeg > 0.0 && s.Magnitude > maxMag)
                {
                    maxMag = s.Magnitude;
                    maxMagT = t;
                }

                var pen = PenOf(s);
                if (prevPen <= 0.0 && pen > 0.0) penBeginT = Bisect(prev, t, latDeg, lonEastDeg, deltaTDays, PenOf);
                if (prevPen > 0.0 && pen <= 0.0) penEndT = Bisect(prev, t, latDeg, lonEastDeg, deltaTDays, PenOf);
                var umb = UmbOf(s);
                if (prevUmb <= 0.0 && umb > 0.0) umbBeginT = Bisect(prev, t, latDeg, lonEastDeg, deltaTDays, UmbOf);
                if (prevUmb > 0.0 && umb <= 0.0) umbEndT = Bisect(prev, t, latDeg, lonEastDeg, deltaTDays, UmbOf);
                prev = t;
                prevPen = pen;
                prevUmb = umb;
                t += StepDays;
            }

            // Not even a partial eclipse from here (or it never clears the horizon).
            if (penBeginT == null && penEndT == null && maxMag <= 0.0)
            {
                var gs = At(se.JdeMaximum);
                return new SolarEclipseLocalCircumstances(
                    GlobalType: se.Type,
                    GlobalMagnitude: se.Magnitude,
                    GlobalMaximumJdUtc: globalMaxUtc,
                    LocalType: SolarEclipseLocalType.None,
                    MagnitudeAtMax: 0.0,
                    SunAltitudeAtMaxDeg: gs.SunAltitudeDeg,
                    Visible: false,
                    PartialBeginJdUtc: null,
                    MaximumJdUtc: null,
                    PartialEndJdUtc: null,
                    CentralBeginJdUtc: null,
                    CentralEndJdUtc: null,
                    CentralDurationSeconds: null);
            }

            // Refine the local maximum (minimum distance from the axis) by ternary search.
            var localMaxT = RefineMaximum(
                Math.Max(maxMagT - StepDays, start),
                Math.Min(maxMagT + StepDays, end),
                latDeg, lonEastDeg, deltaTDays);
            var maxState = At(localMaxT);
            var central = umbBeginT != null || umbEndT != null;
            var localType = central
                ? (maxState.L2P < 0.0 ? SolarEclipseLocalType.Total : SolarEclipseLocalType.Annular)
                : SolarEclipseLocalType.Partial;
            var centralDuration = umbBeginT != null && umbEndT != null
                ? (umbEndT.Value - umbBeginT.Value) * 86400.0
                : (double?)null;

            return new SolarEclipseLocalCircumstances(
                GlobalType: se.Type,
                GlobalMagnitude: se.Magnitude,
                GlobalMaximumJdUtc: globalMaxUtc,
                LocalType: localType,
                MagnitudeAtMax: Math.Max(maxState.Magnitude, 0.0),
                SunAltitudeAtMaxDeg: maxState.SunAltitudeDeg,
                // Visible if any instant of the partial phase occurred with the Sun up — even
                // when the Sun then set mid-eclipse (local maximum clamped to the horizon).
                Visible: maxMag > 0.0,
                PartialBeginJdUtc: penBeginT.HasValue ? ToUtc(penBeginT.Value) : (double?)null,
                MaximumJdUtc: ToUtc(localMaxT),
                PartialEndJdUtc: penEndT.HasValue ? ToUtc(penEndT.Value) : (double?)null,
                CentralBeginJdUtc: umbBeginT.HasValue ? ToUtc(umbBeginT.Value) : (double?)null,
                CentralEndJdUtc: umbEndT.HasValue ? ToUtc(umbEndT.Value) : (double?)null,
                CentralDurationSeconds: centralDuration);
        }

        /// <summary>Bisects [lo, hi] for the root of <paramref name="f"/> (which changes sign across the interval).</summary>
        private static double Bisect(
            double lo,
            double hi,
            double latDeg,
            double lonEastDeg,
            double deltaTDays,
            Func<State, double> f)
        {
            var a = lo;
            var b = hi;
            var fa = f(StateAt(a, latDeg, lonEastDeg, deltaTDays));
            for (var i = 0; i < 40; i++)
            {
                var mid = (a + b) / 2.0;
                var fm = f(StateAt(mid, latDeg, lonEastDeg, deltaTDays));
                if ((fa <= 0.0) == (fm <= 0.0))
                {
                    a = mid;
                    fa = fm;
                }
                else
                {
                    b = mid;
                }
            }

            return (a + b) / 2.0;
        }

        /// <summary>
        /// Ternary search for the instant of greatest <i>visible</i> eclipse. The magnitude is gated
        /// on the Sun being above the horizon so that, when the Sun sets mid-eclipse, the local
        /// maximum is clamped to the horizon (matching NASA's local circumstances) instead of
        /// diving below it to the geometric — but unobservable — closest approach.
        /// </summary>
        private static double RefineMaximum(double lo, double hi, double latDeg, double lonEastDeg, double deltaTDays)
        {
            double GatedMag(double jd)
            {
                var s = StateAt(jd, latDeg, lonEastDeg, deltaTDays);
                return s.SunAltitudeDeg > 0.0 ? s.Magnitude : double.NegativeInfinity;
            }

            var a = lo;
            var b = hi;
            for (var i = 0; i < 60; i++)
            {
                var m1 = a + (b - a) / 3.0;
                var m2 = b - (b - a) / 3.0;
                if (GatedMag(m1) < GatedMag(m2)) a = m1;
                else b = m2;
            }

            return (a + b) / 2.0;
        }
    }
}
