using System.Collections.ObjectModel;

namespace Compute.Core.Domain.Entities.Models
{
    public class TimeZoneOffset
    {
        public string DisplayName { get; private set; }
        public int Hours { get; private set; }

        public override string ToString()
        {
            return DisplayName;
        }

        public TimeZoneOffset(string displayName, int hours)
        {
            DisplayName = displayName;
            Hours = hours;
        }

        public static ObservableCollection<TimeZoneOffset> GetUtcOffsets()
        {
            return new ObservableCollection<TimeZoneOffset>
        {
            new TimeZoneOffset("UTC-12", -12),
            new TimeZoneOffset("UTC-11", -11),
            new TimeZoneOffset("UTC-10", -10),
            new TimeZoneOffset("UTC-9", -9),
            new TimeZoneOffset("UTC-8", -8),
            new TimeZoneOffset("UTC-7", -7),
            new TimeZoneOffset("UTC-6", -6),
            new TimeZoneOffset("UTC-5", -5),
            new TimeZoneOffset("UTC-4", -4),
            new TimeZoneOffset("UTC-3", -3),
            new TimeZoneOffset("UTC-2", -2),
            new TimeZoneOffset("UTC-1", -1),
            new TimeZoneOffset("UTC±0 (UTC)", 0),
            new TimeZoneOffset("UTC+1", 1),
            new TimeZoneOffset("UTC+2", 2),
            new TimeZoneOffset("UTC+3", 3),
            new TimeZoneOffset("UTC+4", 4),
            new TimeZoneOffset("UTC+5", 5),
            new TimeZoneOffset("UTC+6", 6),
            new TimeZoneOffset("UTC+7", 7),
            new TimeZoneOffset("UTC+8", 8),
            new TimeZoneOffset("UTC+9", 9),
            new TimeZoneOffset("UTC+10", 10),
            new TimeZoneOffset("UTC+11", 11),
            new TimeZoneOffset("UTC+12", 12)
        };
        }
    }
}
