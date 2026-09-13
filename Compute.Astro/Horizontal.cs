using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>
    /// Horizontal (alt-az) coordinates of a celestial body for one observer at one instant.
    /// <paramref name="AzimuthDeg"/> is measured from true <b>North, clockwise</b> (0° = N, 90° = E,
    /// 180° = S, 270° = W — the civil/navigation convention). <paramref name="AltitudeDeg"/> is the
    /// angle above the horizon; negative below.
    /// </summary>
    public readonly record struct Horizontal(double AzimuthDeg, double AltitudeDeg);

    /// <summary>
    /// Where in the sky the Sun or Moon is, from a given place at a given time — the
    /// building block for sun/moon direction overlays, trajectory arcs, shadow lengths,
    /// and AR alignment.
    ///
    /// Clean-room from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed.: ch. 13 (the
    /// equatorial → horizontal transform), ch. 40 (topocentric correction), ch. 16
    /// (refraction, Sæmundsson's formula). Time inputs are Julian Days in <b>UTC</b> (the
    /// same convention as <see cref="SunRiseSet"/>/<see cref="MoonRiseSet"/>); the body's
    /// position is evaluated at Dynamical Time internally via <see cref="AstroTime.DeltaTSeconds"/>.
    /// Longitude is <b>positive east</b>.
    /// </summary>
    public static class HorizontalCoordinates
    {
        /// <summary>Earth flattening for the topocentric observer reduction (WGS84-class).</summary>
        private const double Flattening = 1.0 / 298.257;

        // The galactic centre (Sgr A*), J2000: RA 17h45m40.04s, Dec −29°00′28.1″. Treated as fixed:
        // precession moves it ~50″/yr (≈0.3° over 20 years) — invisible at map-ray precision, and the
        // Milky Way core photographers aim at is degrees wide anyway.
        private const double GalacticCenterRaDeg = 266.41683;
        private const double GalacticCenterDecDeg = -29.00781;

        /// <summary>
        /// Horizontal coordinates from an hour angle H, declination δ, and observer
        /// latitude φ (all degrees). Pure spherical trig (Meeus ch. 13); azimuth is
        /// converted from Meeus's from-South convention to from-North.
        /// </summary>
        public static Horizontal FromHourAngle(double hourAngleDeg, double declinationDeg, double latitudeDeg)
        {
            var h = ToRadians(hourAngleDeg);
            var dec = ToRadians(declinationDeg);
            var phi = ToRadians(latitudeDeg);
            var altitude = Math.Asin(Math.Sin(phi) * Math.Sin(dec) + Math.Cos(phi) * Math.Cos(dec) * Math.Cos(h));
            var azimuthFromSouth = Math.Atan2(Math.Sin(h), Math.Cos(h) * Math.Sin(phi) - Math.Tan(dec) * Math.Cos(phi));
            return new Horizontal(
                AzimuthDeg: NormalizeDegrees(ToDegrees(azimuthFromSouth) + 180.0),
                AltitudeDeg: ToDegrees(altitude));
        }

        /// <summary>
        /// Local hour angle (degrees, (-180, 180]) of a body with apparent right ascension
        /// <paramref name="rightAscensionDeg"/> for an observer at <paramref name="longitudeEastDeg"/>,
        /// at UTC instant <paramref name="jdUt"/>.
        /// </summary>
        public static double HourAngleDeg(double jdUt, double rightAscensionDeg, double longitudeEastDeg) =>
            NormalizeDegreesSigned(
                Nutation.ApparentSiderealTimeDeg(jdUt) + longitudeEastDeg - rightAscensionDeg);

        /// <summary>
        /// The Sun's azimuth/altitude at UTC instant <paramref name="jdUt"/> from (lat, lonEast).
        /// Solar parallax (≤ 8.8″) is ignored. With <paramref name="applyRefraction"/> the altitude is
        /// the <i>apparent</i> one under standard atmosphere (what a camera actually sees near the
        /// horizon); default is the geometric (airless) altitude, matching <see cref="SunRiseSet"/>'s
        /// conventions.
        /// </summary>
        public static Horizontal OfSun(
            double jdUt,
            double latitudeDeg,
            double longitudeEastDeg,
            bool applyRefraction = false)
        {
            var pos = Sun.PositionAt(jdUt + DeltaTDays(jdUt));
            var h = HourAngleDeg(jdUt, pos.RightAscension, longitudeEastDeg);
            var horizontal = FromHourAngle(h, pos.Declination, latitudeDeg);
            return applyRefraction ? WithRefraction(horizontal) : horizontal;
        }

        /// <summary>
        /// The Moon's azimuth/altitude at UTC instant <paramref name="jdUt"/> from (lat, lonEast).
        ///
        /// By default the position is <b>topocentric</b> (corrected for the Moon's ~1°
        /// horizontal parallax via Meeus ch. 40, sea-level observer) — that is what an
        /// observer, camera, or AR overlay actually sees; pass <c>topocentric: false</c> for
        /// the geocentric direction used by rise/set-style calculations.
        /// </summary>
        public static Horizontal OfMoon(
            double jdUt,
            double latitudeDeg,
            double longitudeEastDeg,
            bool topocentric = true,
            bool applyRefraction = false)
        {
            var pos = Moon.PositionAt(jdUt + DeltaTDays(jdUt));
            var ra = pos.RightAscensionDeg;
            var dec = pos.DeclinationDeg;
            if (topocentric)
            {
                // Observer's geocentric coordinates ρ·sinφ' / ρ·cosφ' (Meeus ch. 11, sea level).
                var phi = ToRadians(latitudeDeg);
                var u = Math.Atan((1.0 - Flattening) * Math.Tan(phi));
                var rhoSinPhi = (1.0 - Flattening) * Math.Sin(u);
                var rhoCosPhi = Math.Cos(u);
                // Topocentric RA/Dec (Meeus eq. 40.2 / 40.3).
                var hGeo = ToRadians(HourAngleDeg(jdUt, ra, longitudeEastDeg));
                var sinPi = Math.Sin(ToRadians(pos.HorizontalParallaxDeg));
                var decR = ToRadians(dec);
                var dAlpha = Math.Atan2(
                    -rhoCosPhi * sinPi * Math.Sin(hGeo),
                    Math.Cos(decR) - rhoCosPhi * sinPi * Math.Cos(hGeo));
                var decTopo = Math.Atan2(
                    (Math.Sin(decR) - rhoSinPhi * sinPi) * Math.Cos(dAlpha),
                    Math.Cos(decR) - rhoCosPhi * sinPi * Math.Cos(hGeo));
                ra = NormalizeDegrees(ra + ToDegrees(dAlpha));
                dec = ToDegrees(decTopo);
            }

            var h = HourAngleDeg(jdUt, ra, longitudeEastDeg);
            var horizontal = FromHourAngle(h, dec, latitudeDeg);
            return applyRefraction ? WithRefraction(horizontal) : horizontal;
        }

        /// <summary>
        /// Where the <b>Milky Way core</b> (the galactic centre) is in the sky at UTC instant
        /// <paramref name="jdUt"/> from (lat, lonEast) — the direction night-sky photographers plan
        /// shots around. From mid-northern latitudes it only skims the southern horizon
        /// (max altitude 90° − |φ + 29°|); from the southern hemisphere it can pass overhead.
        /// </summary>
        public static Horizontal OfGalacticCenter(double jdUt, double latitudeDeg, double longitudeEastDeg)
        {
            var h = HourAngleDeg(jdUt, GalacticCenterRaDeg, longitudeEastDeg);
            return FromHourAngle(h, GalacticCenterDecDeg, latitudeDeg);
        }

        /// <summary>
        /// Azimuth/altitude of an arbitrary equatorial position (RA/Dec of date, degrees) at UTC
        /// instant <paramref name="jdUt"/> from (lat, lonEast) — the general path used by the sky
        /// chart for planets, constellation boundaries and other fixed points.
        /// </summary>
        public static Horizontal OfEquatorial(
            double jdUt,
            double rightAscensionDeg,
            double declinationDeg,
            double latitudeDeg,
            double longitudeEastDeg)
        {
            var h = HourAngleDeg(jdUt, rightAscensionDeg, longitudeEastDeg);
            return FromHourAngle(h, declinationDeg, latitudeDeg);
        }

        /// <summary>
        /// Atmospheric refraction R (degrees) to <b>add</b> to a true (airless) altitude to
        /// get the apparent altitude. Sæmundsson's formula (Meeus eq. 16.4), standard
        /// conditions (1010 mbar, 10 °C): ≈ 0.48° at the horizon, ~0 at the zenith.
        /// Below −1° the formula is extrapolated flat (the body is not visible anyway).
        /// </summary>
        public static double RefractionDeg(double trueAltitudeDeg)
        {
            var h = Math.Max(trueAltitudeDeg, -1.0);
            var rArcmin = 1.02 / Math.Tan(ToRadians(h + 10.3 / (h + 5.11)));
            return Math.Max(rArcmin / 60.0, 0.0);
        }

        private static Horizontal WithRefraction(Horizontal horizontal) =>
            horizontal with { AltitudeDeg = horizontal.AltitudeDeg + RefractionDeg(horizontal.AltitudeDeg) };

        /// <summary>ΔT in days for the civil date of <paramref name="jdUt"/>.</summary>
        private static double DeltaTDays(double jdUt)
        {
            var cal = AstroTime.CalendarFromJulianDay(jdUt);
            return AstroTime.DeltaTSeconds(cal.Year, cal.Month) / 86400.0;
        }
    }
}
