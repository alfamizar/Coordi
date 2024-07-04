using GeoTimeZone;
using NodaTime;
using System.Diagnostics;

namespace Compute.Core.Utils
{
    public static class TimeZoneUtils
    {
        public static string GetTimeZoneId(double latitude, double longitude)
        {
            string timeZoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;

            if (string.IsNullOrEmpty(timeZoneId))
            {
                Debug.WriteLine("Timezone not found for the given coordinates.");
                return string.Empty;
            }

            var timeZone = DateTimeZoneProviders.Tzdb[timeZoneId];

            var now = SystemClock.Instance.GetCurrentInstant();
            var zonedDateTime = now.InZone(timeZone);

            var offset = zonedDateTime.Offset;
            //var isDaylightSaving = zonedDateTime.IsDaylightSavingTime();

            var offsetHours = offset.Seconds / 3600;

            if (offsetHours == 0)
            {
                return "UTC±0 (UTC)";
            }

            string text = ((offsetHours >= 0) ? "+" : "-") + Math.Abs(offsetHours);
            return "UTC" + text;
        }
    }
}
