using System.Globalization;

namespace JustCompute.Presentation.Collections
{
    /// <summary>
    /// Group keys for a chronological list that folds everything before today into one group and
    /// keeps the rest by year. A century table that starts in 2000 otherwise opens a quarter of a
    /// century before anything the reader can still go and see.
    /// </summary>
    public static class TimelineGroups
    {
        /// <summary>The one group for everything already over. Never a year, so never confused with one.</summary>
        public const string PastKey = "past";

        /// <summary>
        /// Something on today's date is not past: an eclipse this evening is the one the reader
        /// most wants to see. Both dates are compared as calendar days in the same zone, which is
        /// the caller's to arrange.
        /// </summary>
        public static string KeyOf(DateTime date, DateTime today) =>
            date.Date < today.Date
                ? PastKey
                : date.ToString("yyyy", CultureInfo.InvariantCulture);
    }
}
