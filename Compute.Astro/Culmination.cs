using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>When a fixed point of sky is highest for an observer, and what it does either side of that.</summary>
    /// <param name="TransitJdUtc">Instant of upper culmination, Julian Day UTC.</param>
    /// <param name="TransitAltitudeDeg">Altitude at transit (degrees). Negative when the object never clears the horizon.</param>
    /// <param name="RiseJdUtc">Rise, or null when the object is circumpolar or never rises.</param>
    /// <param name="SetJdUtc">Set, or null for the same two reasons.</param>
    /// <param name="Circumpolar">Above the horizon all day: there is a transit but no rise and no set.</param>
    /// <param name="NeverRises">Below it all day. <paramref name="TransitAltitudeDeg"/> is then how far below it stays at its best.</param>
    public readonly record struct SkyPassage(
        double TransitJdUtc,
        double TransitAltitudeDeg,
        double? RiseJdUtc,
        double? SetJdUtc,
        bool Circumpolar,
        bool NeverRises);

    /// <summary>
    /// Rise, transit and set for something that does not move: a galaxy, a constellation, the
    /// radiant of a meteor shower, or a planet over the hours it takes to cross the sky.
    ///
    /// Solved rather than searched. A fixed object transits at the moment its hour angle is zero,
    /// and the hour angle runs at a constant 360.9856°/day, so the answer is one division — where
    /// a body that moves against the stars needs the iteration <see cref="MoonRiseSet"/> and
    /// <see cref="SunRiseSet"/> do. Sampling a fixed object at some interval and keeping the
    /// highest sample, which is what a screen would otherwise do, is both slower and wrong by
    /// half the interval.
    ///
    /// Positions are apparent right ascension and declination of date. For catalogue coordinates,
    /// precess them first (<see cref="Coordinates.PrecessEquatorial"/>) or a J2000 position will
    /// transit some minutes off by the end of the century.
    /// </summary>
    public static class Culmination
    {
        /// <summary>
        /// Degrees of hour angle per day of UT: a sidereal day is shorter than a solar one, which
        /// is why a star rises four minutes earlier each night.
        /// </summary>
        private const double HourAnglePerDay = 360.98564736629;

        /// <summary>
        /// The horizon a star is taken to rise at: refraction lifts it about 34′ when it is on the
        /// true horizon, so it appears while still geometrically below. Half the Sun's disc is not
        /// subtracted, because a star has none.
        /// </summary>
        public const double StarHorizonDeg = -0.5667;

        /// <summary>
        /// The passage of (<paramref name="raDeg"/>, <paramref name="decDeg"/>) across the sky of
        /// (<paramref name="latDeg"/>, <paramref name="lonEastDeg"/>) around <paramref name="jdUtc"/> —
        /// the transit nearest that instant, and the rise and set bracketing it.
        /// </summary>
        public static SkyPassage Near(
            double jdUtc,
            double raDeg,
            double decDeg,
            double latDeg,
            double lonEastDeg,
            double horizonDeg = StarHorizonDeg)
        {
            var transit = TransitNear(jdUtc, raDeg, lonEastDeg);
            var altitude = TransitAltitudeDeg(decDeg, latDeg);
            var h = HourAngleAtHorizon(decDeg, latDeg, horizonDeg);

            return new SkyPassage(
                TransitJdUtc: transit,
                TransitAltitudeDeg: altitude,
                RiseJdUtc: h is double rise ? transit - rise / HourAnglePerDay : null,
                SetJdUtc: h is double set ? transit + set / HourAnglePerDay : null,
                Circumpolar: h is null && altitude > horizonDeg,
                NeverRises: h is null && altitude <= horizonDeg);
        }

        /// <summary>The upper culmination nearest <paramref name="jdUtc"/> — within half a sidereal day either way.</summary>
        public static double TransitNear(double jdUtc, double raDeg, double lonEastDeg)
        {
            // How far the object already is past the meridian, signed, then undo it at the rate
            // the hour angle runs. One step is exact: for a fixed object that rate is a constant.
            var hourAngle = HorizontalCoordinates.HourAngleDeg(jdUtc, raDeg, lonEastDeg);
            return jdUtc - hourAngle / HourAnglePerDay;
        }

        /// <summary>
        /// Altitude at upper culmination: 90° minus how far the declination is from the zenith.
        ///
        /// Which is the same for an object that passes north of the zenith as for one that passes
        /// south — the absolute value is doing that work, and is why this holds at every latitude
        /// including the poles.
        /// </summary>
        public static double TransitAltitudeDeg(double decDeg, double latDeg) =>
            90.0 - Math.Abs(latDeg - decDeg);

        /// <summary>
        /// Hour angle at which the object stands at <paramref name="horizonDeg"/>, or null when it
        /// never does — it is either up all day or down all day, and <see cref="Near"/> says which.
        /// </summary>
        public static double? HourAngleAtHorizon(double decDeg, double latDeg, double horizonDeg)
        {
            var lat = ToRadians(latDeg);
            var dec = ToRadians(decDeg);

            // At the pole, and for an object at the celestial equator seen from it, the formula
            // divides by a cosine that is zero. Neither has a rise: the sky there turns about the
            // zenith and nothing crosses the horizon at all.
            var denominator = Math.Cos(lat) * Math.Cos(dec);
            if (Math.Abs(denominator) < 1e-12) return null;

            var cosH = (Math.Sin(ToRadians(horizonDeg)) - Math.Sin(lat) * Math.Sin(dec)) / denominator;
            if (cosH < -1.0 || cosH > 1.0) return null;

            return ToDegrees(Math.Acos(cosH));
        }

        /// <summary>
        /// Altitude of the fixed position at <paramref name="jdUtc"/>, for drawing the curve of a night.
        ///
        /// Geometric: no refraction, because the shape of the curve is the point and a third of a
        /// degree at the ends of it is not.
        /// </summary>
        public static double AltitudeDegAt(
            double jdUtc,
            double raDeg,
            double decDeg,
            double latDeg,
            double lonEastDeg) =>
            HorizontalCoordinates.FromHourAngle(
                HorizontalCoordinates.HourAngleDeg(jdUtc, raDeg, lonEastDeg), decDeg, latDeg).AltitudeDeg;
    }
}
