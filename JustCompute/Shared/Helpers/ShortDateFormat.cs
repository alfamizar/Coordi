using System.Globalization;

namespace JustCompute.Shared.Helpers
{
    /// <summary>
    /// A short "day and month" label — the weather strip's column headings.
    ///
    /// A hardcoded "d MMM" is a Latin-script assumption: it puts the day first and the month
    /// second, which in Japanese renders "2 9月" and in Korean "2 9월" — the parts in the wrong
    /// order, and the day left bare. Every culture already describes its own arrangement in
    /// <see cref="DateTimeFormatInfo.MonthDayPattern"/> ("MMMM d", "d. MMMM", "M月d日",
    /// "M월 d일"), so use that and only shorten the month name, which is all "d MMM" was after.
    /// </summary>
    public static class ShortDateFormat
    {
        /// <summary>Day and month in the culture's own order, with an abbreviated month name.</summary>
        public static string DayAndMonth(DateOnly date, CultureInfo culture) =>
            date.ToString(PatternFor(culture), culture);

        private static string PatternFor(CultureInfo culture) =>
            // CJK patterns carry no "MMMM" — their month is a number plus a marker — so they
            // pass through untouched, which is exactly right.
            culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM");
    }
}
