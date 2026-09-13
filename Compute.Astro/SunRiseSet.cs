using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>Standard altitudes of the centre of the body for event calculations (degrees).</summary>
    public static class SunAltitude
    {
        /// <summary>Sunrise/sunset: upper limb on the horizon incl. refraction (Meeus, p. 102).</summary>
        public const double Official = -0.8333;

        /// <summary>Civil twilight.</summary>
        public const double Civil = -6.0;

        /// <summary>Nautical twilight.</summary>
        public const double Nautical = -12.0;

        /// <summary>Astronomical twilight.</summary>
        public const double Astronomical = -18.0;
    }

    /// <summary>Whether the Sun rises and sets on a given day, or stays permanently up or down.</summary>
    public enum DayType
    {
        /// <summary>The Sun both rises and sets.</summary>
        Normal,

        /// <summary>The Sun never rises.</summary>
        PolarNight,

        /// <summary>The Sun never sets.</summary>
        MidnightSun,
    }

    /// <summary>
    /// Result of a sun-event calculation for one UTC calendar day at one location.
    /// Times are <b>minutes from 00:00 UTC</b> on that day (may be slightly &lt;0 or ≥1440
    /// near the date boundary — normalize when mapping to a wall-clock instant).
    /// </summary>
    /// <param name="Type">Whether this is a normal day, polar night, or midnight sun.</param>
    /// <param name="SunriseUtcMinutes">Sunrise, in minutes from 00:00 UTC; null when the Sun does not rise.</param>
    /// <param name="SunsetUtcMinutes">Sunset, in minutes from 00:00 UTC; null when the Sun does not set.</param>
    /// <param name="SolarNoonUtcMinutes">Solar transit, in minutes from 00:00 UTC.</param>
    /// <param name="DayLengthMinutes">Day length in minutes (0 on polar night, 1440 under the midnight sun).</param>
    public readonly record struct SunEvents(
        DayType Type,
        double? SunriseUtcMinutes,
        double? SunsetUtcMinutes,
        double SolarNoonUtcMinutes,
        double DayLengthMinutes);

    /// <summary>
    /// The six twilight boundaries of one UTC calendar day, in minutes from 00:00 UTC and in the
    /// order they occur.
    ///
    /// Any stage is null when the Sun does not reach that altitude on this day, which is a normal
    /// answer rather than a failure: above roughly 48° of latitude there are summer weeks with no
    /// astronomical twilight at all, and further north the nautical and even the civil stage go
    /// the same way. That is what a white night is, and the caller is expected to show nothing
    /// rather than invent a time.
    /// </summary>
    public readonly record struct TwilightTimes(
        double? FirstLightUtcMinutes,
        double? NauticalDawnUtcMinutes,
        double? CivilDawnUtcMinutes,
        double? CivilDuskUtcMinutes,
        double? NauticalDuskUtcMinutes,
        double? LastLightUtcMinutes);

    /// <summary>
    /// Rise/set via the NOAA hour-angle method (derived from Meeus ch. 15 + ch. 25/28).
    /// Longitude is <b>positive east</b>.
    ///
    /// Verified: London (51.5074°N, −0.1278°E) 2024-06-21 → 03:43 / 20:22 UTC.
    /// </summary>
    public static class SunRiseSet
    {
        /// <summary>Sunrise/sunset and day length for the official horizon altitude.</summary>
        public static SunEvents Events(
            int year,
            int month,
            int day,
            double latitudeDeg,
            double longitudeEastDeg) =>
            EventsAtAltitude(year, month, day, latitudeDeg, longitudeEastDeg, SunAltitude.Official);

        /// <summary>
        /// Generic event calculation at an arbitrary centre altitude (use <see cref="SunAltitude"/>
        /// constants for civil / nautical / astronomical twilight).
        /// </summary>
        public static SunEvents EventsAtAltitude(
            int year,
            int month,
            int day,
            double latitudeDeg,
            double longitudeEastDeg,
            double altitudeDeg)
        {
            var jd0 = AstroTime.JulianDay(year, month, day);
            // Evaluate the Sun near local solar noon for stable δ / EoT.
            var jdNoon = jd0 + 0.5 - longitudeEastDeg / 360.0;
            var pos = Sun.PositionAt(jdNoon);
            var eot = Sun.EquationOfTimeMinutes(jdNoon);

            var phi = ToRadians(latitudeDeg);
            var dec = ToRadians(pos.Declination);
            var transit = 720.0 - 4.0 * longitudeEastDeg - eot;

            var cosH = (Math.Sin(ToRadians(altitudeDeg)) - Math.Sin(phi) * Math.Sin(dec)) /
                       (Math.Cos(phi) * Math.Cos(dec));

            if (cosH > 1.0) return new SunEvents(DayType.PolarNight, null, null, transit, 0.0);
            if (cosH < -1.0) return new SunEvents(DayType.MidnightSun, null, null, transit, 1440.0);

            var hMinutes = 4.0 * ToDegrees(Math.Acos(cosH));
            var rise = transit - hMinutes;
            var set = transit + hMinutes;
            return new SunEvents(DayType.Normal, rise, set, transit, set - rise);
        }

        /// <summary>
        /// All three twilight stages for one day, dawn and dusk, at the standard depressions of
        /// 6°, 12° and 18° below the horizon.
        ///
        /// Each stage is solved independently, because they fail independently: on a June night in
        /// northern Europe the Sun crosses −12° but never −18°, so nautical twilight has times and
        /// astronomical twilight has none.
        /// </summary>
        public static TwilightTimes Twilight(
            int year,
            int month,
            int day,
            double latitudeDeg,
            double longitudeEastDeg)
        {
            (double? Dawn, double? Dusk) At(double altitudeDeg)
            {
                var events = EventsAtAltitude(year, month, day, latitudeDeg, longitudeEastDeg, altitudeDeg);
                return (events.SunriseUtcMinutes, events.SunsetUtcMinutes);
            }

            var (civilDawn, civilDusk) = At(SunAltitude.Civil);
            var (nauticalDawn, nauticalDusk) = At(SunAltitude.Nautical);
            var (astroDawn, astroDusk) = At(SunAltitude.Astronomical);

            return new TwilightTimes(
                FirstLightUtcMinutes: astroDawn,
                NauticalDawnUtcMinutes: nauticalDawn,
                CivilDawnUtcMinutes: civilDawn,
                CivilDuskUtcMinutes: civilDusk,
                NauticalDuskUtcMinutes: nauticalDusk,
                LastLightUtcMinutes: astroDusk);
        }
    }
}
