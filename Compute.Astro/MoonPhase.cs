using System;
using System.Collections.Generic;
using System.Linq;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>The four principal lunar phases.</summary>
    public enum MoonPhaseType
    {
        /// <summary>New Moon.</summary>
        New,

        /// <summary>First Quarter.</summary>
        FirstQuarter,

        /// <summary>Full Moon.</summary>
        Full,

        /// <summary>Last Quarter.</summary>
        LastQuarter,
    }

    /// <summary>A principal phase and the instant it occurs (Julian Day, Dynamical Time).</summary>
    public readonly record struct MoonPhaseEvent(MoonPhaseType Type, double Jde);

    /// <summary>
    /// Times of New / First Quarter / Full / Last Quarter Moon.
    ///
    /// Clean-room from J. Meeus, <i>Astronomical Algorithms</i>, 2nd ed., ch. 49.
    /// Results are in Dynamical Time (TD); subtract ΔT (<see cref="AstroTime.DeltaTSeconds"/>) for UTC.
    ///
    /// Verified: new moon Feb 1977 = 1977-02-18 03:37 TD (Example 49.a) and the
    /// 2017-08-21 eclipse new moon to ~1 min; full &amp; first-quarter paths to ~1–2 min.
    /// </summary>
    public static class MoonPhase
    {
        /// <summary>The fraction of a lunation each principal phase sits at.</summary>
        public static double KOffset(this MoonPhaseType type) => type switch
        {
            MoonPhaseType.New => 0.0,
            MoonPhaseType.FirstQuarter => 0.25,
            MoonPhaseType.Full => 0.5,
            MoonPhaseType.LastQuarter => 0.75,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

        /// <summary>14 planetary-argument corrections shared by all phases.</summary>
        private static double PlanetaryCorrection(double k, double t)
        {
            var a = new[]
            {
                299.77 + 0.107408 * k - 0.009173 * t * t,
                251.88 + 0.016321 * k,
                251.83 + 26.651886 * k,
                349.42 + 36.412478 * k,
                84.66 + 18.206239 * k,
                141.74 + 53.303771 * k,
                207.14 + 2.453732 * k,
                154.84 + 7.306860 * k,
                34.52 + 27.261239 * k,
                207.19 + 0.121824 * k,
                291.34 + 1.844379 * k,
                161.72 + 24.198154 * k,
                269.28 + 25.513099 * k,
                174.65 + 76.531100 * k,
            };
            var coeff = new[]
            {
                0.000325, 0.000165, 0.000164, 0.000126, 0.000110, 0.000062, 0.000060,
                0.000056, 0.000047, 0.000042, 0.000040, 0.000037, 0.000035, 0.000023,
            };
            var sum = 0.0;
            for (var i = 0; i < a.Length; i++) sum += coeff[i] * Math.Sin(ToRadians(NormalizeDegrees(a[i])));
            return sum;
        }

        /// <summary>Phase instant (JDE) for a given lunation index and phase.</summary>
        public static double Jde(int lunation, MoonPhaseType type)
        {
            var k = lunation + type.KOffset();
            var t = k / 1236.85;
            var mean = 2451550.09766 + 29.530588861 * k +
                       0.00015437 * t * t - 0.000000150 * t * t * t + 0.00000000073 * t * t * t * t;

            var e = 1.0 - 0.002516 * t - 0.0000074 * t * t;
            var m = NormalizeDegrees(2.5534 + 29.10535670 * k - 0.0000014 * t * t - 0.00000011 * t * t * t);
            var mp = NormalizeDegrees(201.5643 + 385.81693528 * k + 0.0107582 * t * t +
                0.00001238 * t * t * t - 0.000000058 * t * t * t * t);
            var f = NormalizeDegrees(160.7108 + 390.67050284 * k - 0.0016118 * t * t -
                0.00000227 * t * t * t + 0.000000011 * t * t * t * t);
            var omega = NormalizeDegrees(124.7746 - 1.56375588 * k + 0.0020672 * t * t + 0.00000215 * t * t * t);

            double correction;
            if (type == MoonPhaseType.New || type == MoonPhaseType.Full)
            {
                correction = NewFullCorrection(e, m, mp, f, omega);
            }
            else
            {
                var c = QuarterCorrection(e, m, mp, f, omega);
                var w = 0.00306 - 0.00038 * e * Math.Cos(ToRadians(m)) + 0.00026 * Math.Cos(ToRadians(mp)) -
                        0.00002 * Math.Cos(ToRadians(mp - m)) + 0.00002 * Math.Cos(ToRadians(mp + m)) +
                        0.00002 * Math.Cos(ToRadians(2 * f));
                correction = c + (type == MoonPhaseType.FirstQuarter ? w : -w);
            }

            return mean + correction + PlanetaryCorrection(k, t);
        }

        /// <summary>The four phases of the lunation nearest the given Julian Day, in time order.</summary>
        public static IReadOnlyList<MoonPhaseEvent> PhasesNear(double jd)
        {
            // Mean lunations since 2000-01-06 new moon.
            var kApprox = (jd - 2451550.09766) / 29.530588861;
            var baseLunation = (int)Math.Floor(kApprox);
            var all = new List<MoonPhaseEvent>();
            for (var lun = baseLunation - 1; lun <= baseLunation + 1; lun++)
            {
                foreach (MoonPhaseType type in Enum.GetValues(typeof(MoonPhaseType)))
                {
                    all.Add(new MoonPhaseEvent(type, Jde(lun, type)));
                }
            }

            return all.Where(e => Math.Abs(e.Jde - jd) < 35.0).OrderBy(e => e.Jde).ToList();
        }

        /// <summary>Lunation index whose New Moon is closest to the given Julian Day.</summary>
        public static int NearestLunation(double jd) =>
            (int)Math.Round((jd - 2451550.09766) / 29.530588861);

        private static double NewFullCorrection(double e, double m, double mp, double f, double omega)
        {
            double S(double x) => Math.Sin(ToRadians(x));
            return -0.40720 * S(mp) + 0.17241 * e * S(m) + 0.01608 * S(2 * mp) +
                   0.01039 * S(2 * f) + 0.00739 * e * S(mp - m) - 0.00514 * e * S(mp + m) +
                   0.00208 * e * e * S(2 * m) - 0.00111 * S(mp - 2 * f) - 0.00057 * S(mp + 2 * f) +
                   0.00056 * e * S(2 * mp + m) - 0.00042 * S(3 * mp) + 0.00042 * e * S(m + 2 * f) +
                   0.00038 * e * S(m - 2 * f) - 0.00024 * e * S(2 * mp - m) - 0.00017 * S(omega) -
                   0.00007 * S(mp + 2 * m) + 0.00004 * S(2 * mp - 2 * f) + 0.00004 * S(3 * m) +
                   0.00003 * S(mp + m - 2 * f) + 0.00003 * S(2 * mp + 2 * f) - 0.00003 * S(mp + m + 2 * f) +
                   0.00003 * S(mp - m + 2 * f) - 0.00002 * S(mp - m - 2 * f) - 0.00002 * S(3 * mp + m) +
                   0.00002 * S(4 * mp);
        }

        private static double QuarterCorrection(double e, double m, double mp, double f, double omega)
        {
            double S(double x) => Math.Sin(ToRadians(x));
            return -0.62801 * S(mp) + 0.17172 * e * S(m) - 0.01183 * e * S(mp + m) +
                   0.00862 * S(2 * mp) + 0.00804 * S(2 * f) + 0.00454 * e * S(mp - m) +
                   0.00204 * e * e * S(2 * m) - 0.00180 * S(mp - 2 * f) - 0.00070 * S(mp + 2 * f) -
                   0.00040 * S(3 * mp) - 0.00034 * e * S(2 * mp - m) + 0.00032 * e * S(m + 2 * f) +
                   0.00032 * e * S(m - 2 * f) - 0.00028 * e * e * S(mp + 2 * m) + 0.00027 * e * S(2 * mp + m) -
                   0.00017 * S(omega) - 0.00005 * S(mp - m - 2 * f) + 0.00004 * S(2 * mp + 2 * f) -
                   0.00004 * S(mp + m + 2 * f) + 0.00004 * S(mp - 2 * m) + 0.00003 * S(mp + m - 2 * f) +
                   0.00003 * S(3 * m) + 0.00002 * S(2 * mp - 2 * f) + 0.00002 * S(mp - m + 2 * f) -
                   0.00002 * S(3 * mp + m);
        }
    }
}
