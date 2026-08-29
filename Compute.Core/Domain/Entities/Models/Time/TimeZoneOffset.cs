using System.Collections.ObjectModel;
using Compute.Core.Utils;

namespace Compute.Core.Domain.Entities.Models.Time
{
    /// <summary>
    /// A UTC offset together with the way it is written. Carries a <see cref="TimeSpan"/> rather
    /// than whole hours because plenty of zones are not on the hour — India is +5:30, Nepal +5:45.
    /// </summary>
    public record TimeZoneOffset(string DisplayName, TimeSpan Offset)
    {
        public override string ToString() => DisplayName;

        public static readonly TimeZoneOffset DefaultTimeZoneOffset = FromOffset(TimeSpan.Zero);

        public static TimeZoneOffset FromOffset(TimeSpan offset) =>
            new(TimeZoneUtils.FormatOffset(offset), offset);

        /// <summary>
        /// The whole-hour offsets shown in the manual picker on the Add Location screen. Real
        /// places get their offset from the tz database instead — this list exists only so a
        /// location can be pinned by hand when the lookup is wrong or the coordinates are made up.
        /// </summary>
        public static ObservableCollection<TimeZoneOffset> GetUtcOffsets() =>
        [
            .. Enumerable
                .Range(-12, 25)
                .Select(hours => FromOffset(TimeSpan.FromHours(hours)))
        ];
    }
}
