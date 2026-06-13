using System.Collections.ObjectModel;

namespace Compute.Core.Domain.Entities.Models.Time
{
    public record TimeZoneOffset(string DisplayName, int Hours)
    {
        public override string ToString() => DisplayName;

        public static readonly TimeZoneOffset DefaultTimeZoneOffset = new("UTC±0 (UTC)", 0);

        public static ObservableCollection<TimeZoneOffset> GetUtcOffsets()
        {
            return
        [
            new("UTC-12", -12),
            new("UTC-11", -11),
            new("UTC-10", -10),
            new("UTC-9", -9),
            new("UTC-8", -8),
            new("UTC-7", -7),
            new("UTC-6", -6),
            new("UTC-5", -5),
            new("UTC-4", -4),
            new("UTC-3", -3),
            new("UTC-2", -2),
            new("UTC-1", -1),
            new("UTC±0 (UTC)", 0),
            new("UTC+1", 1),
            new("UTC+2", 2),
            new("UTC+3", 3),
            new("UTC+4", 4),
            new("UTC+5", 5),
            new("UTC+6", 6),
            new("UTC+7", 7),
            new("UTC+8", 8),
            new("UTC+9", 9),
            new("UTC+10", 10),
            new("UTC+11", 11),
            new("UTC+12", 12)
        ];
        }
    }
}
