using Compute.Astro;
using AstroZodiacSign = Compute.Core.Domain.Entities.Models.AstroSign.AstroZodiacSign;

namespace Compute.Core.Utils
{
    /// <summary>
    /// Bridges the astronomy library's conventions (minutes from 00:00 UTC, Julian Days,
    /// zero-based enums) to the ones the app's models and views expect.
    ///
    /// Note the two different "absent" conventions, which the views rely on:
    /// rise/set times are <see langword="null"/> when the event does not occur (the sun-path
    /// chart and the duration converter both test for null), while eclipse contact times use
    /// <see langword="default"/> — <c>IsDateTimeSetConverter</c> treats null as *set*, so a
    /// null there would show an empty row.
    /// </summary>
    public static class CelestialTimeUtils
    {
        /// <summary>
        /// Turns "minutes from 00:00 UTC on <paramref name="utcDay"/>" into a local wall-clock
        /// <see cref="DateTime"/>, or null when the event does not occur that day.
        /// </summary>
        public static DateTime? ToLocalTime(DateTime utcDay, double? minutesFromUtcMidnight, double offsetHours)
        {
            if (minutesFromUtcMidnight is null) return null;
            return utcDay.Date
                .AddMinutes(minutesFromUtcMidnight.Value)
                .AddHours(offsetHours);
        }

        /// <summary>
        /// Converts a Julian Day in UTC to a local wall-clock <see cref="DateTime"/>, falling back
        /// to <see langword="default"/> (0001-01-01) when the instant is absent — the "did not
        /// occur" marker the eclipse rows are bound against.
        /// </summary>
        public static DateTime ToLocalTimeOrDefault(double? julianDayUtc, double offsetHours)
        {
            if (julianDayUtc is null) return default;
            return AstroTime.DateTimeFromJulianDay(julianDayUtc.Value).AddHours(offsetHours);
        }

        /// <summary>
        /// Maps the library's zero-based <see cref="ZodiacSign"/> onto the app's one-based
        /// <c>AstroZodiacSign</c>, whose zero slot is <c>None</c>.
        /// </summary>
        public static AstroZodiacSign ToAstroZodiacSign(ZodiacSign sign) => (AstroZodiacSign)((int)sign + 1);
    }
}
