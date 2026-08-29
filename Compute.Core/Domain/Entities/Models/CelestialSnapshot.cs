using Compute.Astro;
using Compute.Core.Utils;
using AstroZodiacSign = Compute.Core.Domain.Entities.Models.AstroSign.AstroZodiacSign;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// Everything the "at this location" dashboard shows for one place on one date:
    /// sun and moon events in local time, the Moon's distance and phase, both zodiac
    /// signs, and the UTM/MGRS grid references.
    ///
    /// This is the replacement for what used to be a live <c>CoordinateSharp.Coordinate</c>
    /// bound straight into the view — an immutable snapshot instead, computed once.
    /// </summary>
    public class CelestialSnapshot
    {
        /// <summary>Latitude in decimal degrees.</summary>
        public double Latitude { get; init; }

        /// <summary>Longitude in decimal degrees, positive east.</summary>
        public double Longitude { get; init; }

        /// <summary>The local calendar date this snapshot describes.</summary>
        public DateTime GeoDate { get; init; }

        /// <summary>Offset from UTC applied to the event times, in hours.</summary>
        public double OffsetHours { get; init; }

        /// <summary>Sunrise in local time; null when the Sun does not rise.</summary>
        public DateTime? SunRise { get; init; }

        /// <summary>Sunset in local time; null when the Sun does not set.</summary>
        public DateTime? SunSet { get; init; }

        /// <summary>Moonrise in local time; null when it does not occur that day.</summary>
        public DateTime? MoonRise { get; init; }

        /// <summary>Moonset in local time; null when it does not occur that day.</summary>
        public DateTime? MoonSet { get; init; }

        /// <summary>Earth–Moon centre distance in kilometres.</summary>
        public double MoonDistanceKm { get; init; }

        /// <summary>Illuminated fraction of the Moon's disk, 0..1.</summary>
        public double MoonIllumination { get; init; }

        /// <summary>The Moon's visible phase.</summary>
        public Moon.MoonPhase MoonPhase { get; init; }

        /// <summary>The Sun's tropical zodiac sign.</summary>
        public AstroZodiacSign ZodiacSign { get; init; }

        /// <summary>The Moon's tropical zodiac sign.</summary>
        public AstroZodiacSign MoonSign { get; init; }

        /// <summary>UTM grid reference; null outside the 80°S–84°N band UTM covers.</summary>
        public UtmInfo? Utm { get; init; }

        /// <summary>MGRS grid reference; null outside the 80°S–84°N band MGRS covers.</summary>
        public MgrsInfo? Mgrs { get; init; }

        /// <summary>
        /// Computes the snapshot for a place and a local date. <paramref name="offsetHours"/> is
        /// the location's offset from UTC, used to place the event times on the local clock.
        /// </summary>
        public static CelestialSnapshot For(double latitude, double longitude, DateTime date, double offsetHours)
        {
            var day = date.Date;
            var offsetMinutes = (int)Math.Round(offsetHours * 60.0);

            var sun = SunRiseSet.Events(day.Year, day.Month, day.Day, latitude, longitude);
            var moonEvents = MoonRiseSet.Events(day.Year, day.Month, day.Day, latitude, longitude, offsetMinutes);

            // Evaluate the Moon's own quantities at local noon, the middle of the day being shown.
            var jdNoon = AstroTime.JulianDay(day.Year, day.Month, day.Day, 12) - offsetHours / 24.0;
            var moonPosition = Astro.Moon.PositionAt(jdNoon);

            return new CelestialSnapshot
            {
                Latitude = latitude,
                Longitude = longitude,
                GeoDate = day,
                OffsetHours = offsetHours,
                SunRise = CelestialTimeUtils.ToLocalTime(day, sun.SunriseUtcMinutes, offsetHours),
                SunSet = CelestialTimeUtils.ToLocalTime(day, sun.SunsetUtcMinutes, offsetHours),
                MoonRise = CelestialTimeUtils.ToLocalTime(day, moonEvents.MoonriseUtcMinutes, offsetHours),
                MoonSet = CelestialTimeUtils.ToLocalTime(day, moonEvents.MoonsetUtcMinutes, offsetHours),
                MoonDistanceKm = moonPosition.DistanceKm,
                MoonIllumination = Astro.Moon.IlluminatedFraction(jdNoon),
                MoonPhase = (Moon.MoonPhase)MoonPhaseNaming.At(jdNoon),
                ZodiacSign = CelestialTimeUtils.ToAstroZodiacSign(Zodiac.OfSun(jdNoon)),
                MoonSign = CelestialTimeUtils.ToAstroZodiacSign(Zodiac.OfMoon(jdNoon)),
                Utm = TryUtm(latitude, longitude),
                Mgrs = TryMgrs(latitude, longitude),
            };
        }

        private static UtmInfo? TryUtm(double latitude, double longitude)
        {
            if (!GridReferenceIsSupported(latitude)) return null;
            return UtmInfo.From(GridReference.ToUtm(latitude, longitude));
        }

        private static MgrsInfo? TryMgrs(double latitude, double longitude)
        {
            if (!GridReferenceIsSupported(latitude)) return null;
            return MgrsInfo.From(GridReference.ToMgrs(latitude, longitude));
        }

        /// <summary>UTM/MGRS are undefined in the polar UPS regions.</summary>
        private static bool GridReferenceIsSupported(double latitude) => latitude >= -80.0 && latitude <= 84.0;
    }
}
