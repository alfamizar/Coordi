using System;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>
    /// Tropical zodiac signs: twelve equal 30° arcs of ecliptic longitude measured
    /// from the vernal equinox (0° = Aries). This is the astrological convention used
    /// for "the Sun's sign on a given date" — distinct from the IAU constellations
    /// (see <see cref="Constellation"/>).
    /// </summary>
    public enum ZodiacSign
    {
        /// <summary>Aries — ecliptic longitude 0°–30°.</summary>
        Aries,

        /// <summary>Taurus — 30°–60°.</summary>
        Taurus,

        /// <summary>Gemini — 60°–90°.</summary>
        Gemini,

        /// <summary>Cancer — 90°–120°.</summary>
        Cancer,

        /// <summary>Leo — 120°–150°.</summary>
        Leo,

        /// <summary>Virgo — 150°–180°.</summary>
        Virgo,

        /// <summary>Libra — 180°–210°.</summary>
        Libra,

        /// <summary>Scorpio — 210°–240°.</summary>
        Scorpio,

        /// <summary>Sagittarius — 240°–270°.</summary>
        Sagittarius,

        /// <summary>Capricorn — 270°–300°.</summary>
        Capricorn,

        /// <summary>Aquarius — 300°–330°.</summary>
        Aquarius,

        /// <summary>Pisces — 330°–360°.</summary>
        Pisces,
    }

    /// <summary>Tropical zodiac lookups.</summary>
    public static class Zodiac
    {
        private static readonly string[] Symbols =
        {
            "♈", "♉", "♊", "♋", "♌", "♍",
            "♎", "♏", "♐", "♑", "♒", "♓",
        };

        /// <summary>Ecliptic longitude (degrees) at which the sign begins.</summary>
        public static int StartLongitudeDeg(this ZodiacSign sign) => (int)sign * 30;

        /// <summary>The sign's astrological symbol, e.g. ♈ for Aries.</summary>
        public static string Symbol(this ZodiacSign sign) => Symbols[(int)sign];

        /// <summary>Sign for a given apparent ecliptic longitude (degrees).</summary>
        public static ZodiacSign FromEclipticLongitude(double longitudeDeg)
        {
            var index = (int)Math.Floor(NormalizeDegrees(longitudeDeg) / 30.0) % 12;
            return (ZodiacSign)index;
        }

        /// <summary>Convenience: the Sun's zodiac sign at the given UTC instant.</summary>
        public static ZodiacSign OfSun(double jd) =>
            FromEclipticLongitude(Sun.PositionAt(jd).ApparentLongitude);

        /// <summary>The Moon's zodiac sign (sign of its apparent ecliptic longitude) at the given instant.</summary>
        public static ZodiacSign OfMoon(double jd) =>
            FromEclipticLongitude(Moon.PositionAt(jd).ApparentLongitudeDeg);
    }
}
