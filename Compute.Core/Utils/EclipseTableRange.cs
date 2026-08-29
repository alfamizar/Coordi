using Compute.Astro;

namespace Compute.Core.Utils
{
    /// <summary>
    /// The window the eclipse screens list. Kept in one place so the solar and lunar
    /// tables always cover the same span.
    /// </summary>
    public static class EclipseTableRange
    {
        /// <summary>
        /// Julian Day bounds of the whole century containing <paramref name="date"/> — e.g. any
        /// date in 2026 yields 2000-01-01 through 2099-12-31. This is the span the screens
        /// have always shown.
        /// </summary>
        public static (double StartJd, double EndJd) ForCenturyOf(DateTime date)
        {
            var firstYear = date.Year / 100 * 100;
            return (
                AstroTime.JulianDay(firstYear, 1, 1),
                AstroTime.JulianDay(firstYear + 99, 12, 31, 23, 59, 59));
        }
    }
}
