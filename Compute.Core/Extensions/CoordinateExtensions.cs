using GeoTimeZone;
using NodaTime;

namespace Compute.Core.Extensions
{
    public static class CoordinateExtensions
    {
        /// <summary>
        /// Midnight on <paramref name="date"/> in the time zone the given coordinates fall in,
        /// or null when no zone can be resolved. Used to derive both the UTC offset and whether
        /// daylight saving is in effect for that place and day.
        ///
        /// This is a synchronous call that blocks the calling thread — callers should ensure it
        /// is invoked off the UI thread (e.g. inside <c>Task.Run</c>).
        /// </summary>
        public static ZonedDateTime? GetZonedDateTime(double latitude, double longitude, DateTime date)
        {
            string timeZoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
            if (string.IsNullOrEmpty(timeZoneId))
            {
                return null;
            }

            var timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
            if (timeZone == null)
            {
                return null;
            }

            var localDateTime = new LocalDateTime(date.Year, date.Month, date.Day, 0, 0, 0);

            return localDateTime.InZoneLeniently(timeZone);
        }
    }
}