using System.Globalization;
using GeoTimeZone;
using NodaTime;

namespace Compute.Core.Utils
{
    /// <summary>
    /// Resolves what a location's clock actually reads.
    ///
    /// A UTC offset is a function of the date, not just the place: half the world moves its
    /// clocks twice a year, and plenty of zones are not whole hours (India +5:30, Nepal +5:45,
    /// Chatham +12:45). So a location stores a *zone*, and the offset is asked for per instant.
    /// </summary>
    public static class TimeZoneUtils
    {
        private const string FixedOffsetPrefix = "UTC";

        /// <summary>
        /// The IANA zone id covering the coordinates, e.g. <c>Europe/London</c>. Empty when the
        /// lookup fails or the coordinates are out of range. Blocking — keep it off the UI thread
        /// on first use.
        /// </summary>
        /// <summary>
        /// Loads the zone dataset ahead of time, off whatever thread calls this.
        ///
        /// The lookup itself is trivial — measured at ~0.0006 ms — but the *first* one pays
        /// ~23 ms to load GeoTimeZone's embedded data, and it lands wherever the first zone is
        /// needed: usually a coordinate assignment on the UI thread, i.e. a visible hitch of
        /// about one and a half frames on the first screen that shows a time. Doing it on a
        /// background thread at startup moves that cost somewhere nobody is looking.
        ///
        /// Safe to call more than once; subsequent calls cost a lookup.
        /// </summary>
        public static void Prewarm() => GetTimeZoneId(51.5074, -0.1278);

        public static string GetTimeZoneId(double latitude, double longitude)
        {
            if (!AreValidCoordinates(latitude, longitude))
            {
                return string.Empty;
            }

            try
            {
                return TimeZoneLookup.GetTimeZone(latitude, longitude).Result ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// The UTC offset in force for <paramref name="timeZoneId"/> at <paramref name="utc"/>,
        /// daylight saving included. Accepts a fixed <c>UTC±HH:MM</c> id for locations the user
        /// pinned by hand, and falls back to the longitude approximation when nothing resolves.
        /// </summary>
        public static TimeSpan GetUtcOffset(string? timeZoneId, DateTime utc, double longitude)
        {
            if (TryParseFixedOffset(timeZoneId, out var pinned))
            {
                return pinned;
            }

            if (!string.IsNullOrEmpty(timeZoneId))
            {
                var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timeZoneId);
                if (zone is not null)
                {
                    var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
                    return zone.GetUtcOffset(instant).ToTimeSpan();
                }
            }

            return ApproximateOffsetFromLongitude(longitude);
        }

        /// <summary>
        /// Each 15° of longitude is roughly an hour. A last resort only: it ignores daylight
        /// saving and every zone border that does not follow a meridian.
        /// </summary>
        public static TimeSpan ApproximateOffsetFromLongitude(double longitude)
        {
            if (longitude < -180 || longitude > 180)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromHours(Math.Round(longitude / 15.0));
        }

        /// <summary>
        /// Encodes a hand-picked offset as a zone id, so a pinned location and a looked-up one
        /// are the same kind of thing everywhere downstream.
        /// </summary>
        public static string ToFixedOffsetId(TimeSpan offset)
        {
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var magnitude = offset.Duration();
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{FixedOffsetPrefix}{sign}{magnitude.Hours:00}:{magnitude.Minutes:00}");
        }

        /// <summary>
        /// True only for an id the offset picker produced, which is always signed
        /// (<c>UTC+05:30</c>, <c>UTC+00:00</c>).
        ///
        /// Deliberately narrower than <see cref="TryParseFixedOffset"/>, which also accepts a
        /// bare <c>UTC</c> so that id still yields a zero offset. Bare <c>UTC</c> is what a
        /// coordinate lookup returns, not something anyone chose — telling the two apart is what
        /// stops a place that once resolved to UTC from being frozen there forever.
        /// </summary>
        public static bool IsPinnedFixedOffset(string? timeZoneId) =>
            !string.IsNullOrEmpty(timeZoneId)
            && timeZoneId.Length > FixedOffsetPrefix.Length
            && timeZoneId.StartsWith(FixedOffsetPrefix, StringComparison.Ordinal)
            && timeZoneId[FixedOffsetPrefix.Length] is '+' or '-'
            && TryParseFixedOffset(timeZoneId, out _);

        /// <summary>Reads back an id produced by <see cref="ToFixedOffsetId"/>.</summary>
        public static bool TryParseFixedOffset(string? timeZoneId, out TimeSpan offset)
        {
            offset = TimeSpan.Zero;

            if (string.IsNullOrEmpty(timeZoneId)
                || !timeZoneId.StartsWith(FixedOffsetPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var remainder = timeZoneId.AsSpan(FixedOffsetPrefix.Length);
            if (remainder.Length == 0)
            {
                return true; // bare "UTC"
            }

            var sign = remainder[0] switch
            {
                '+' => 1,
                '-' => -1,
                _ => 0
            };

            if (sign == 0
                || !TimeSpan.TryParseExact(remainder[1..], @"hh\:mm", CultureInfo.InvariantCulture, out var magnitude))
            {
                return false;
            }

            offset = sign < 0 ? -magnitude : magnitude;
            return true;
        }

        /// <summary>How an offset is written in the UI: <c>UTC±0 (UTC)</c>, <c>UTC+1</c>, <c>UTC+5:30</c>.</summary>
        public static string FormatOffset(TimeSpan offset)
        {
            if (offset == TimeSpan.Zero)
            {
                return "UTC±0 (UTC)";
            }

            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var magnitude = offset.Duration();

            return magnitude.Minutes == 0
                ? string.Create(CultureInfo.InvariantCulture, $"UTC{sign}{magnitude.Hours}")
                : string.Create(CultureInfo.InvariantCulture, $"UTC{sign}{magnitude.Hours}:{magnitude.Minutes:00}");
        }

        private static bool AreValidCoordinates(double latitude, double longitude) =>
            latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }
}
