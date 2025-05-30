using CoordinateSharp;
using GeoTimeZone;
using NodaTime;

namespace Compute.Core.Extensions
{
    public static class CoordinateExtensions
    {
        public static int CalculateOffsetHours(this Coordinate coordinate)
        {
            string timeZoneId = TimeZoneLookup.GetTimeZone(coordinate.Latitude.ToDouble(), coordinate.Longitude.ToDouble()).Result;
            if (string.IsNullOrEmpty(timeZoneId))
            {
                return 0;
            }

            var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
            if (timeZone == null)
            {
                return 0;
            }

            var localDateTime = new LocalDateTime(coordinate.GeoDate.Year, coordinate.GeoDate.Month, coordinate.GeoDate.Day, 0, 0, 0);
            var zoneDateTime = localDateTime.InZoneLeniently(timeZone);
            var offset = zoneDateTime.Offset;

            return (int)(offset.Seconds / 3600);
        }
    }
}
