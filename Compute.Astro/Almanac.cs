using System.Collections.Generic;
using System.Linq;

namespace Compute.Astro
{
    /// <summary>
    /// Traditional full-moon names. These are a calendar/folklore convention (the
    /// North-American Farmers' Almanac names), not an astronomical quantity, so they are
    /// defined here as a clean mapping rather than derived from any third-party source.
    ///
    /// <see cref="None"/> is used for a date with no full moon; <see cref="Blue"/> is the
    /// second full moon to fall within a single calendar month.
    /// </summary>
    public enum FullMoonName
    {
        /// <summary>No full moon on this date.</summary>
        None,

        /// <summary>January.</summary>
        Wolf,

        /// <summary>February.</summary>
        Snow,

        /// <summary>March.</summary>
        Worm,

        /// <summary>April.</summary>
        Pink,

        /// <summary>May.</summary>
        Flower,

        /// <summary>June.</summary>
        Strawberry,

        /// <summary>July.</summary>
        Buck,

        /// <summary>August.</summary>
        Sturgeon,

        /// <summary>September.</summary>
        Corn,

        /// <summary>October.</summary>
        Hunters,

        /// <summary>November.</summary>
        Beaver,

        /// <summary>December.</summary>
        Cold,

        /// <summary>The second full moon within one calendar month.</summary>
        Blue,
    }

    /// <summary>The full moon that occurs on a given UTC date, with its almanac name.</summary>
    /// <param name="JdUtc">Instant of the full moon, as a Julian Day in UTC.</param>
    /// <param name="Year">UTC calendar year.</param>
    /// <param name="Month">UTC calendar month.</param>
    /// <param name="Day">UTC calendar day.</param>
    /// <param name="Name">The traditional name of this full moon.</param>
    public sealed record NamedFullMoon(double JdUtc, int Year, int Month, int Day, FullMoonName Name);

    /// <summary>
    /// Almanac full-moon naming. Full-moon instants come from <see cref="MoonPhase"/> (clean-room
    /// Meeus ch. 49); the month → name table is the traditional Northern-Hemisphere set.
    /// All dates are evaluated in <b>UTC</b>.
    /// </summary>
    public static class Almanac
    {
        /// <summary>The traditional name for the (first) full moon of a calendar month (1..12).</summary>
        public static FullMoonName TraditionalName(int month) => month switch
        {
            1 => FullMoonName.Wolf,
            2 => FullMoonName.Snow,
            3 => FullMoonName.Worm,
            4 => FullMoonName.Pink,
            5 => FullMoonName.Flower,
            6 => FullMoonName.Strawberry,
            7 => FullMoonName.Buck,
            8 => FullMoonName.Sturgeon,
            9 => FullMoonName.Corn,
            10 => FullMoonName.Hunters,
            11 => FullMoonName.Beaver,
            12 => FullMoonName.Cold,
            _ => FullMoonName.None,
        };

        /// <summary>
        /// Every full moon whose UTC date falls in the given calendar month, in time
        /// order. The first is named for the month; a second one (a "blue moon") is
        /// <see cref="FullMoonName.Blue"/>. Usually a list of one; occasionally two.
        /// </summary>
        public static IReadOnlyList<NamedFullMoon> FullMoonsInMonth(int year, int month)
        {
            var jdMid = AstroTime.JulianDay(year, month, 15);
            var kCentre = (int)AstroMath.RoundHalfUp((jdMid - 2451550.09766) / 29.530588861);
            var found = new List<NamedFullMoon>();
            for (var k = kCentre - 2; k <= kCentre + 2; k++)
            {
                var jde = MoonPhase.Jde(k, MoonPhaseType.Full);
                // JDE is Dynamical Time; convert to UTC by removing ΔT.
                var cal = AstroTime.CalendarFromJulianDay(jde);
                var deltaTDays = AstroTime.DeltaTSeconds(cal.Year, cal.Month) / 86400.0;
                var jdUtc = jde - deltaTDays;
                var c = AstroTime.CalendarFromJulianDay(jdUtc);
                if (c.Year == year && c.Month == month)
                {
                    found.Add(new NamedFullMoon(jdUtc, c.Year, c.Month, c.Day, FullMoonName.None));
                }
            }

            return found
                .OrderBy(fm => fm.JdUtc)
                .Select((fm, index) => fm with { Name = index == 0 ? TraditionalName(month) : FullMoonName.Blue })
                .ToList();
        }

        /// <summary>
        /// The almanac name of the full moon that lands on the given UTC date, or
        /// <see cref="FullMoonName.None"/> if no full moon occurs that day.
        /// </summary>
        public static FullMoonName MoonNameOn(int year, int month, int day) =>
            FullMoonsInMonth(year, month).FirstOrDefault(fm => fm.Day == day)?.Name ?? FullMoonName.None;
    }
}
