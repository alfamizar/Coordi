using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Compute.Astro
{
    /// <summary>Degrees / decimal-minutes decomposition of a signed angle (the "DDM" form).</summary>
    public readonly record struct Ddm(bool Negative, int Degrees, double Minutes)
    {
        /// <summary>Decompose a signed decimal degree value into degrees and decimal minutes.</summary>
        public static Ddm Of(double decimalDegrees)
        {
            var neg = decimalDegrees < 0;
            var a = Math.Abs(decimalDegrees);
            var d = Math.Floor(a);
            return new Ddm(neg, (int)d, (a - d) * 60.0);
        }
    }

    /// <summary>
    /// Human-readable formatting and tolerant parsing of geographic coordinates
    /// (latitude/longitude). Pure string/number work — no external dependency. This is
    /// the licence-clean replacement for CoordinateSharp's <c>CoordinatePart</c> display and
    /// the building block for a coordinate-converter screen.
    /// </summary>
    public static class GeoFormat
    {
        /// <summary>Which of the two geographic axes a value belongs to (drives the hemisphere letter).</summary>
        public enum Axis
        {
            /// <summary>Latitude — N/S.</summary>
            Latitude,

            /// <summary>Longitude — E/W.</summary>
            Longitude,
        }

        private static readonly char[] HemisphereLetters = { 'N', 'S', 'E', 'W' };

        // Note º (U+00BA) and ª (U+00AA) are Unicode *letters*, so they must be allowed before
        // the letter check — they're common degree-sign stand-ins from Spanish/Portuguese keyboards.
        private const string AllowedSymbols = "+-.,°'\"′″’”*^ºª";

        private static char Hemisphere(Axis axis, bool negative) =>
            axis == Axis.Latitude
                ? (negative ? 'S' : 'N')
                : (negative ? 'W' : 'E');

        /// <summary>Degrees-minutes-seconds, e.g. <c>N 47° 36' 22.32"</c>.</summary>
        public static string FormatDms(double decimalDegrees, Axis axis, int secondsDecimals = 2)
        {
            var hemi = Hemisphere(axis, decimalDegrees < 0);
            // Decompose after rounding to the displayed precision (integer-scaled seconds)
            // so a value like 46°59'59.999" carries cleanly to 47°0'0.00".
            var dec = AstroMath.Clamp(secondsDecimals, 0, 6);
            var factor = 1L;
            for (var i = 0; i < dec; i++) factor *= 10L;
            var perMinute = 60L * factor;
            var perDegree = 60L * perMinute;
            var totalScaled = (long)Math.Round(Math.Abs(decimalDegrees) * 3600.0 * factor);
            var d = totalScaled / perDegree;
            var m = totalScaled % perDegree / perMinute;
            var sScaled = totalScaled % perMinute;
            var sec = dec == 0
                ? sScaled.ToString(CultureInfo.InvariantCulture)
                : (sScaled / factor).ToString(CultureInfo.InvariantCulture) + "." + AstroMath.PadZeros(sScaled % factor, dec);
            return FormattableString.Invariant($"{hemi} {d}° {m}' {sec}\"");
        }

        /// <summary>Degrees and decimal minutes, e.g. <c>N 47° 36.372'</c>.</summary>
        public static string FormatDdm(double decimalDegrees, Axis axis, int minutesDecimals = 3)
        {
            var hemi = Hemisphere(axis, decimalDegrees < 0);
            var dec = AstroMath.Clamp(minutesDecimals, 0, 8);
            var factor = 1L;
            for (var i = 0; i < dec; i++) factor *= 10L;
            var perDegree = 60L * factor;
            var totalScaled = (long)Math.Round(Math.Abs(decimalDegrees) * 60.0 * factor);
            var d = totalScaled / perDegree;
            var mScaled = totalScaled % perDegree;
            var min = dec == 0
                ? mScaled.ToString(CultureInfo.InvariantCulture)
                : (mScaled / factor).ToString(CultureInfo.InvariantCulture) + "." + AstroMath.PadZeros(mScaled % factor, dec);
            return FormattableString.Invariant($"{hemi} {d}° {min}'");
        }

        /// <summary>Signed decimal degrees, e.g. <c>47.606200°</c>.</summary>
        public static string FormatDecimal(double decimalDegrees, int decimals = 6) =>
            RoundToString(decimalDegrees, decimals) + "°";

        /// <summary>
        /// Parse a coordinate component written as decimal degrees, DMS, or DDM, with an
        /// optional N/S/E/W hemisphere letter or leading sign. Returns signed decimal
        /// degrees, or null if nothing numeric can be read.
        ///
        /// Accepts e.g. <c>47.6062</c>, <c>-122.3321</c>, <c>47°36'22.32"N</c>,
        /// <c>S 33° 52.128'</c>, <c>122 19 55.56 W</c>.
        /// </summary>
        public static double? ParseOrNull(string text)
        {
            if (text == null) return null;
            var raw = text.Trim().ToUpperInvariant();
            if (raw.Length == 0) return null;

            // Reject any character that isn't a digit, whitespace, hemisphere letter, or DMS symbol.
            foreach (var ch in raw)
            {
                if (char.IsLetter(ch) && Array.IndexOf(HemisphereLetters, ch) < 0 && AllowedSymbols.IndexOf(ch) < 0)
                {
                    return null;
                }

                if (!char.IsDigit(ch) && !char.IsWhiteSpace(ch) && !char.IsLetter(ch) && AllowedSymbols.IndexOf(ch) < 0)
                {
                    return null;
                }
            }

            var negative = raw.StartsWith("-", StringComparison.Ordinal);
            var body = raw.Replace(',', '.');
            foreach (var h in HemisphereLetters)
            {
                if (body.IndexOf(h) >= 0)
                {
                    if (h == 'S' || h == 'W') negative = true;
                    body = body.Replace(h, ' ');
                }
            }

            // Replace D/M/S symbols and separators with spaces.
            var builder = new StringBuilder(body.Length);
            foreach (var ch in body)
            {
                builder.Append(char.IsDigit(ch) || ch == '.' ? ch : ' ');
            }

            var parts = new List<double>();
            foreach (var token in builder.ToString().Split(' '))
            {
                if (token.Length == 0) continue;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    parts.Add(parsed);
                }
            }

            if (parts.Count == 0 || parts.Count > 3) return null;

            var degrees = Math.Abs(parts[0]);
            var minutes = parts.Count > 1 ? parts[1] : 0.0;
            var seconds = parts.Count > 2 ? parts[2] : 0.0;
            var magnitude = degrees + minutes / 60.0 + seconds / 3600.0;
            return negative ? -magnitude : magnitude;
        }

        /// <summary>
        /// Like <see cref="ParseOrNull"/> but throws <see cref="FormatException"/> on
        /// unparseable input.
        /// </summary>
        public static double Parse(string text) =>
            ParseOrNull(text) ?? throw new FormatException("not a coordinate: \"" + text + "\"");

        private static string RoundToString(double value, int decimals)
        {
            if (decimals <= 0) return ((long)value).ToString(CultureInfo.InvariantCulture);
            var factor = 1.0;
            for (var i = 0; i < decimals; i++) factor *= 10.0;
            var scaled = Math.Round(value * factor) / factor;
            // Render with a fixed number of fractional digits (no platform locale).
            var neg = scaled < 0;
            var a = Math.Abs(scaled);
            var whole = (long)Math.Floor(a);
            var frac = (long)Math.Round((a - whole) * factor);
            var carry = 0L;
            if (frac >= (long)factor)
            {
                carry = 1;
                frac -= (long)factor;
            }

            var fracStr = AstroMath.PadZeros(frac, decimals);
            return (neg ? "-" : string.Empty) + (whole + carry).ToString(CultureInfo.InvariantCulture) + "." + fracStr;
        }
    }
}
