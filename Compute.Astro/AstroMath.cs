using System;
using System.Globalization;

namespace Compute.Astro
{
    /// <summary>
    /// Low-level angular helpers shared across the astronomy package.
    ///
    /// Clean-room implementation written from public algorithms
    /// (J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed.). No third-party source is used.
    /// </summary>
    internal static class AstroMath
    {
        public const double Deg2Rad = Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / Math.PI;

        public static double ToRadians(double deg) => deg * Deg2Rad;

        public static double ToDegrees(double rad) => rad * Rad2Deg;

        /// <summary>Normalize an angle in degrees to the range [0, 360).</summary>
        public static double NormalizeDegrees(double deg)
        {
            var d = deg % 360.0;
            if (d < 0) d += 360.0;
            return d;
        }

        /// <summary>Normalize an angle in degrees to the range (-180, 180].</summary>
        public static double NormalizeDegreesSigned(double deg)
        {
            var d = NormalizeDegrees(deg);
            if (d > 180.0) d -= 360.0;
            return d;
        }

        /// <summary>
        /// Rounds half away from positive infinity, matching Kotlin's <c>roundToInt</c> /
        /// <c>roundToLong</c> (which is <c>Math.round</c> on the JVM). Distinct from
        /// <see cref="Math.Round(double)"/>, whose ties go to even.
        /// </summary>
        public static long RoundHalfUp(double value) => (long)Math.Floor(value + 0.5);

        /// <summary>Constrains <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

        /// <summary>Constrains <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public static double Clamp(double value, double min, double max) => value < min ? min : (value > max ? max : value);

        /// <summary>Renders a non-negative integer left-padded with zeros, culture-invariantly.</summary>
        public static string PadZeros(long value, int width) =>
            value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
    }

    /// <summary>A decimal angle decomposed into sign / degrees / arcminutes / arcseconds.</summary>
    public readonly record struct Dms(bool Negative, int Degrees, int Minutes, double Seconds)
    {
        /// <summary>Decompose a signed decimal degree value into D M S.</summary>
        public static Dms Of(double decimalDegrees)
        {
            var neg = decimalDegrees < 0;
            var a = Math.Abs(decimalDegrees);
            var d = Math.Floor(a);
            var mFull = (a - d) * 60.0;
            var m = Math.Floor(mFull);
            var s = (mFull - m) * 60.0;
            return new Dms(neg, (int)d, (int)m, s);
        }

        /// <summary>e.g. <c>N 47° 36' 22.32"</c> given hemisphere letters.</summary>
        public string Format(char positive, char negativeChar, int secondsDecimals = 2)
        {
            var hemi = Negative ? negativeChar : positive;
            // Integer-scaled rendering: deterministic across platforms and carries cleanly
            // when the seconds round up to a full minute, so 46°59'60.00" becomes 47°0'0.00".
            var dec = AstroMath.Clamp(secondsDecimals, 0, 6);
            var factor = 1L;
            for (var i = 0; i < dec; i++) factor *= 10L;
            var d = Degrees;
            var m = Minutes;
            var sScaled = (long)Math.Round(Seconds * factor);
            if (sScaled >= 60L * factor)
            {
                sScaled -= 60L * factor;
                m += 1;
                if (m == 60)
                {
                    m = 0;
                    d += 1;
                }
            }

            var secStr = dec == 0
                ? sScaled.ToString(CultureInfo.InvariantCulture)
                : (sScaled / factor).ToString(CultureInfo.InvariantCulture) + "." + AstroMath.PadZeros(sScaled % factor, dec);

            return FormattableString.Invariant($"{hemi} {d}° {m}' {secStr}\"");
        }
    }
}
