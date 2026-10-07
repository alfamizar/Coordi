using System;

namespace Compute.Astro
{
    /// <summary>A UTC calendar date/time decomposed from a Julian Day.</summary>
    public readonly record struct CalendarDateTime(int Year, int Month, int Day, int Hour, int Minute, double Second);

    /// <summary>
    /// Time-scale conversions used by the celestial routines.
    ///
    /// Clean-room implementation from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed.
    /// (Julian Day: ch. 7; sidereal time: ch. 12; ΔT: NASA/Espenak–Meeus polynomial).
    ///
    /// All inputs are <b>UTC</b> civil time.
    /// </summary>
    public static class AstroTime
    {
        /// <summary>Julian Day of the J2000.0 epoch.</summary>
        public const double J2000 = 2451545.0;

        private const double JulianCentury = 36525.0;

        /// <summary>
        /// Julian Day for a <b>proleptic Gregorian</b> calendar date/time in UTC. Meeus
        /// eq. 7.1 with the Gregorian correction applied to all dates — the same
        /// convention as ISO 8601, and the exact inverse of <see cref="CalendarFromJulianDay"/>.
        /// Note that historical sources quote pre-1582 dates in the Julian calendar,
        /// which differs by several days.
        ///
        /// Reference checks (verified): 2000-01-01 12:00 → 2451545.0,
        /// 1957-10-04 19:26:24 → 2436116.31.
        /// </summary>
        public static double JulianDay(int year, int month, int day, int hour = 0, int minute = 0, double second = 0.0)
        {
            var y = year;
            var m = month;
            if (m <= 2)
            {
                y -= 1;
                m += 12;
            }

            var a = y / 100;
            var b = 2 - a + a / 4;
            var dayFraction = day + (hour + minute / 60.0 + second / 3600.0) / 24.0;
            return Math.Floor(365.25 * (y + 4716)) +
                   Math.Floor(30.6001 * (m + 1)) +
                   dayFraction + b - 1524.5;
        }

        /// <summary>
        /// Julian Day for a <see cref="DateTime"/>. The value is interpreted as UTC:
        /// a <see cref="DateTimeKind.Local"/> input is converted first, while
        /// <see cref="DateTimeKind.Unspecified"/> is taken to already be UTC.
        /// </summary>
        public static double JulianDay(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local) utc = utc.ToUniversalTime();
            return JulianDay(
                utc.Year, utc.Month, utc.Day,
                utc.Hour, utc.Minute,
                utc.Second + utc.Millisecond / 1000.0);
        }

        /// <summary>Julian Day for a <see cref="DateTimeOffset"/>, via its UTC instant.</summary>
        public static double JulianDay(DateTimeOffset instant) => JulianDay(instant.UtcDateTime);

        /// <summary>Julian centuries (of 36525 days) since J2000.0.</summary>
        public static double JulianCenturies(double jd) => (jd - J2000) / JulianCentury;

        /// <summary>
        /// ΔT = TD − UT, in seconds.
        ///
        /// Espenak–Meeus (the polynomial fit published with NASA's Five Millennium Canon of Solar
        /// Eclipses) everywhere except the present day, where it is no longer true. Its post-2005
        /// branch extrapolates a slowing Earth: it gives 75.5 s for 2026 and 81 s for 2035, while the
        /// Earth has instead been speeding up and the measured value has sat near 69 s since 2019.
        /// Six seconds is a mile of shadow track and six seconds of a ninety-second totality, on a
        /// screen that prints contact times to the second.
        ///
        /// So 2005 onward comes from <see cref="DeltaTRecent"/> — the USNO monthly series through the
        /// last observation, then USNO's own predictions — and past the end of that the long-term
        /// curve resumes, shifted to meet it. Accuracy: a few hundredths of a second where observed,
        /// USNO's own stated uncertainty (tenths of a second, growing) where predicted, and the
        /// Espenak–Meeus figures elsewhere — ≲1 s over the telescopic era, minutes to hours in
        /// antiquity, where the uncertainty is in the Earth's rotation itself rather than the fit.
        /// </summary>
        public static double DeltaTSeconds(int year, int month = 7)
        {
            var y = year + (month - 0.5) / 12.0;

            if (y < -500)
            {
                var u0 = (y - 1820.0) / 100.0;
                return -20.0 + 32.0 * u0 * u0;
            }

            if (y < 500)
            {
                var u = y / 100.0;
                return 10583.6 + u * (-1014.41 + u * (33.78311 + u * (-5.952053 +
                    u * (-0.1798452 + u * (0.022174192 + u * 0.0090316521)))));
            }

            if (y < 1600)
            {
                var u = (y - 1000.0) / 100.0;
                return 1574.2 + u * (-556.01 + u * (71.23472 + u * (0.319781 +
                    u * (-0.8503463 + u * (-0.005050998 + u * 0.0083572073)))));
            }

            if (y < 1700)
            {
                var t = y - 1600.0;
                return 120.0 + t * (-0.9808 + t * (-0.01532 + t / 7129.0));
            }

            if (y < 1800)
            {
                var t = y - 1700.0;
                return 8.83 + t * (0.1603 + t * (-0.0059285 + t * (0.00013336 - t / 1174000.0)));
            }

            if (y < 1860)
            {
                var t = y - 1800.0;
                return 13.72 + t * (-0.332447 + t * (0.0068612 + t * (0.0041116 + t * (-0.00037436 +
                    t * (0.0000121272 + t * (-0.0000001699 + t * 0.000000000875))))));
            }

            if (y < 1900)
            {
                var t = y - 1860.0;
                return 7.62 + t * (0.5737 + t * (-0.251754 + t * (0.01680668 +
                    t * (-0.0004473624 + t / 233174.0))));
            }

            if (y < 1920)
            {
                var t = y - 1900.0;
                return -2.79 + t * (1.494119 + t * (-0.0598939 + t * (0.0061966 - t * 0.000197)));
            }

            if (y < 1941)
            {
                var t = y - 1920.0;
                return 21.20 + t * (0.84493 + t * (-0.076100 + t * 0.0020936));
            }

            if (y < 1961)
            {
                var t = y - 1950.0;
                return 29.07 + t * (0.407 + t * (-1.0 / 233.0 + t / 2547.0));
            }

            if (y < 1986)
            {
                var t = y - 1975.0;
                return 45.45 + t * (1.067 + t * (-1.0 / 260.0 - t / 718.0));
            }

            if (y < 2005)
            {
                var t = y - 2000.0;
                return 63.86 + t * (0.3345 + t * (-0.060374 + t * (0.0017275 +
                    t * (0.000651814 + t * 0.00002373599))));
            }

            if (y < DeltaTRecentFirstYear + DeltaTRecent.Length - 1)
            {
                // Observed, then predicted, linearly between whole years.
                var index = (int)(y - DeltaTRecentFirstYear);
                var fraction = y - DeltaTRecentFirstYear - index;
                return DeltaTRecent[index] + fraction * (DeltaTRecent[index + 1] - DeltaTRecent[index]);
            }

            if (y < 2150)
            {
                // Past the last prediction: the long-term parabola, offset to be continuous with
                // it. The offset matters for the next few decades and is lost in the noise by
                // the time the parabola has any authority of its own.
                var u = (y - 1820.0) / 100.0;
                return -20.0 + 32.0 * u * u - 0.5628 * (2150.0 - y) + DeltaTLongTermOffset;
            }

            // The same shift, so 2150 is not a step either.
            var uFar = (y - 1820.0) / 100.0;
            return -20.0 + 32.0 * uFar * uFar + DeltaTLongTermOffset;
        }

        /// <summary>
        /// ΔT year by year from 2005, at the start of each year.
        ///
        /// The first 22 are observations from the USNO monthly series (maia.usno.navy.mil,
        /// ser7/deltat.data, read 2026-08-10); the rest are USNO's own predictions
        /// (ser7/deltat.preds). Both are short tables of small numbers, which is what makes it
        /// reasonable to carry them rather than model them: the Earth's rotation over the last
        /// twenty years has not followed any polynomial anyone fitted to it beforehand.
        /// </summary>
        private static readonly double[] DeltaTRecent =
        {
            64.6876, 64.8452, 65.1464, 65.4573, 65.7768, 66.0699,
            66.3246, 66.603, 66.9069, 67.281, 67.6439, 68.1024,
            68.5927, 68.9676, 69.2202, 69.3612, 69.3594, 69.2945,
            69.2039, 69.1752, 69.1377, 69.1099, 69.14, 69.34,
            69.63, 69.97, 70.32, 70.62, 70.98,
        };

        private const int DeltaTRecentFirstYear = 2005;

        /// <summary>
        /// What the long-term parabola has to be shifted by to meet the end of
        /// <see cref="DeltaTRecent"/>.
        ///
        /// Kept as a constant rather than computed so it is visible: the published model is
        /// 11.6 seconds away from the measured Earth by 2033, and that is the size of the
        /// correction being carried forward.
        /// </summary>
        private const double DeltaTLongTermOffset = 11.6468;

        /// <summary>Greenwich Mean Sidereal Time in degrees [0,360). Meeus eq. 12.4.</summary>
        public static double GreenwichMeanSiderealTimeDeg(double jd)
        {
            var t = JulianCenturies(jd);
            var theta = 280.46061837 +
                        360.98564736629 * (jd - J2000) +
                        0.000387933 * t * t -
                        t * t * t / 38_710_000.0;
            return AstroMath.NormalizeDegrees(theta);
        }

        /// <summary>
        /// Inverse of <see cref="JulianDay(int,int,int,int,int,double)"/>: decompose a Julian Day
        /// into <b>proleptic Gregorian</b> UTC calendar components (Meeus ch. 7, Gregorian branch
        /// for all dates, so the round-trip with <c>JulianDay</c> is exact for any civil date —
        /// historical Julian-calendar dates are NOT produced for pre-1582 instants).
        /// </summary>
        public static CalendarDateTime CalendarFromJulianDay(double jd)
        {
            var z = Math.Floor(jd + 0.5);
            var f = jd + 0.5 - z;
            var alpha = Math.Floor((z - 1867216.25) / 36524.25);
            var a = z + 1 + alpha - Math.Floor(alpha / 4);
            var b = a + 1524;
            var c = Math.Floor((b - 122.1) / 365.25);
            var d = Math.Floor(365.25 * c);
            var e = Math.Floor((b - d) / 30.6001);
            var dayFraction = b - d - Math.Floor(30.6001 * e) + f;
            var month = (int)(e < 14 ? e - 1 : e - 13);
            var year = (int)(month > 2 ? c - 4716 : c - 4715);
            var day = (int)Math.Floor(dayFraction);
            var hourFraction = (dayFraction - day) * 24.0;
            var hour = (int)Math.Floor(hourFraction);
            var minuteFraction = (hourFraction - hour) * 60.0;
            var minute = (int)Math.Floor(minuteFraction);
            var second = (minuteFraction - minute) * 60.0;
            return new CalendarDateTime(year, month, day, hour, minute, second);
        }

        /// <summary>
        /// Julian Day → a UTC <see cref="DateTime"/> (<see cref="DateTimeKind.Utc"/>), rounded to
        /// the millisecond. Only valid for years within <see cref="DateTime"/>'s 1..9999 range.
        /// </summary>
        public static DateTime DateTimeFromJulianDay(double jd)
        {
            var c = CalendarFromJulianDay(jd);
            var wholeSeconds = (int)Math.Floor(c.Second);
            var milliseconds = (int)Math.Round((c.Second - wholeSeconds) * 1000.0);
            var stamp = new DateTime(c.Year, c.Month, c.Day, c.Hour, c.Minute, 0, DateTimeKind.Utc);
            return stamp.AddSeconds(wholeSeconds).AddMilliseconds(milliseconds);
        }
    }
}
