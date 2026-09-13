using System;

namespace Compute.Astro
{
    /// <summary>Whether the Moon rises/sets on a given day, or stays permanently up or down.</summary>
    public enum MoonDayType
    {
        /// <summary>At least one horizon crossing occurs in the window.</summary>
        Normal,

        /// <summary>The Moon is above the horizon for the whole window.</summary>
        AlwaysUp,

        /// <summary>The Moon is below the horizon for the whole window.</summary>
        AlwaysDown,
    }

    /// <summary>
    /// Moonrise/moonset for one UTC calendar day at one location. Times are minutes
    /// from 00:00 UTC. A null rise or set means the event does not occur that UTC day
    /// (normal for the Moon, whose events shift ~50 min/day); <paramref name="Type"/> flags
    /// the polar always-up / always-down cases.
    /// </summary>
    public readonly record struct MoonEvents(
        MoonDayType Type,
        double? MoonriseUtcMinutes,
        double? MoonsetUtcMinutes);

    /// <summary>
    /// The Moon moves too fast for a single closed-form hour angle, so this samples the
    /// geocentric altitude across the day and refines each horizon crossing by bisection.
    ///
    /// Uses the standard event altitude h₀ = 0.7275·π − 0.5667° (Meeus, p. 102), which
    /// folds the Moon's horizontal parallax and refraction into the threshold so that
    /// geocentric coordinates may be used directly. Longitude is positive east.
    ///
    /// Verified: London 2024-09-18 moonrise 18:19 UT lands at sunset for that full Moon;
    /// altitude at the returned instants equals h₀ to rounding.
    /// </summary>
    public static class MoonRiseSet
    {
        private const double StepMinutes = 10.0;
        private const int RefineIterations = 40;

        /// <summary>
        /// <paramref name="utcOffsetMinutes"/> shifts the search window to the <b>local</b> calendar
        /// day (00:00–24:00 local) rather than the UTC day, matching how weather apps report
        /// moonrise/set. Returned minutes are still "minutes from 00:00 UTC on
        /// <paramref name="year"/>-<paramref name="month"/>-<paramref name="day"/>" (so they may be
        /// slightly negative or exceed 1440 near the timezone boundary).
        /// </summary>
        public static MoonEvents Events(
            int year,
            int month,
            int day,
            double latitudeDeg,
            double longitudeEastDeg,
            int utcOffsetMinutes = 0)
        {
            var jd0 = AstroTime.JulianDay(year, month, day); // 0h UTC
            var deltaTDays = AstroTime.DeltaTSeconds(year, month) / 86400.0;
            var windowStart = -(double)utcOffsetMinutes; // 00:00 local, in minutes from 00:00 UTC
            var windowEnd = windowStart + 1440.0;

            double AltitudeMinusThreshold(double minutes)
            {
                var jdUt = jd0 + minutes / 1440.0;
                // Position is a function of Dynamical Time; sidereal time of Universal Time.
                var pos = Moon.PositionAt(jdUt + deltaTDays);
                var h0 = 0.7275 * pos.HorizontalParallaxDeg - 0.5667;
                var hourAngle = HorizontalCoordinates.HourAngleDeg(jdUt, pos.RightAscensionDeg, longitudeEastDeg);
                var altitude = HorizontalCoordinates.FromHourAngle(hourAngle, pos.DeclinationDeg, latitudeDeg).AltitudeDeg;
                return altitude - h0;
            }

            double? rise = null;
            double? set = null;

            var prevTime = windowStart;
            var prevValue = AltitudeMinusThreshold(windowStart);
            var time = windowStart + StepMinutes;
            while (time <= windowEnd + 1e-9)
            {
                var value = AltitudeMinusThreshold(time);
                if ((prevValue < 0) != (value < 0))
                {
                    var crossing = Refine(prevTime, time, prevValue, AltitudeMinusThreshold);
                    if (prevValue < 0 && value >= 0) rise = crossing;
                    else set = crossing;
                }

                prevTime = time;
                prevValue = value;
                time += StepMinutes;
            }

            MoonDayType type;
            if (rise != null || set != null) type = MoonDayType.Normal;
            else if (AltitudeMinusThreshold((windowStart + windowEnd) / 2.0) > 0) type = MoonDayType.AlwaysUp;
            else type = MoonDayType.AlwaysDown;

            return new MoonEvents(type, rise, set);
        }

        private static double Refine(double lo, double hi, double loValue, Func<double, double> f)
        {
            var a = lo;
            var b = hi;
            var fa = loValue;
            for (var i = 0; i < RefineIterations; i++)
            {
                var mid = (a + b) / 2.0;
                var fm = f(mid);
                if ((fa < 0) != (fm < 0))
                {
                    b = mid;
                }
                else
                {
                    a = mid;
                    fa = fm;
                }
            }

            return (a + b) / 2.0;
        }
    }
}
