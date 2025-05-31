using CoordinateSharp;
using GeoTimeZone;
using NodaTime;

namespace Compute.Core.Extensions
{
    public static class CoordinateExtensions
    {
        public static ZonedDateTime? GetZonedDateTime(this Coordinate coordinate)
        {
            string timeZoneId = TimeZoneLookup.GetTimeZone(coordinate.Latitude.ToDouble(), coordinate.Longitude.ToDouble()).Result;
            if (string.IsNullOrEmpty(timeZoneId))
            {
                return null;
            }

            var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
            if (timeZone == null)
            {
                return null;
            }

            var localDateTime = new LocalDateTime(coordinate.GeoDate.Year, coordinate.GeoDate.Month, coordinate.GeoDate.Day, 0, 0, 0);

            return localDateTime.InZoneLeniently(timeZone);
        }
    }
}
