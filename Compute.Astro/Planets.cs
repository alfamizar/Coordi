using System;
using System.Collections.Generic;
using static Compute.Astro.AstroMath;

namespace Compute.Astro
{
    /// <summary>The eight major planets.</summary>
    public enum Planet
    {
        /// <summary>Mercury.</summary>
        Mercury,

        /// <summary>Venus.</summary>
        Venus,

        /// <summary>Earth (elements are for the Earth–Moon barycenter).</summary>
        Earth,

        /// <summary>Mars.</summary>
        Mars,

        /// <summary>Jupiter.</summary>
        Jupiter,

        /// <summary>Saturn.</summary>
        Saturn,

        /// <summary>Uranus.</summary>
        Uranus,

        /// <summary>Neptune.</summary>
        Neptune,
    }

    /// <summary>Heliocentric ecliptic (J2000) position of a planet, plus its instantaneous distance.</summary>
    /// <param name="X">Heliocentric rectangular ecliptic x (AU), toward the vernal equinox.</param>
    /// <param name="Y">Heliocentric rectangular ecliptic y (AU).</param>
    /// <param name="Z">Heliocentric rectangular ecliptic z (AU), toward ecliptic north.</param>
    /// <param name="RadiusAu">Heliocentric distance r (AU).</param>
    /// <param name="LongitudeDeg">Heliocentric ecliptic longitude (degrees, [0,360)).</param>
    public readonly record struct PlanetPosition(
        double X,
        double Y,
        double Z,
        double RadiusAu,
        double LongitudeDeg);

    /// <summary>
    /// Keplerian planetary positions from JPL's "Approximate Positions of the Planets"
    /// (Standish/Williams, Table 1: mean elements + centennial rates, valid 1800–2050 AD to
    /// arcminute-level accuracy — far beyond what an orrery visualization can show).
    ///
    /// Clean-room implementation: elements at epoch T, mean anomaly M = L − ϖ, Kepler's equation
    /// solved by Newton iteration, then the orbital-plane position rotated into the ecliptic via
    /// (ω, i, Ω). Verified against the in-house Sun series (Earth's heliocentric longitude opposes
    /// the Sun's geocentric longitude) and against orbital-period/extremes invariants in tests.
    ///
    /// Static physical data an orrery needs travels alongside: mean radius (relative display
    /// sizing), the IAU north-pole direction (ICRF equatorial RA/Dec, degrees — fixes the axial
    /// tilt in space), and the sidereal spin rate (degrees/day; negative = retrograde rotation,
    /// e.g. Venus and Uranus).
    /// </summary>
    public static class Planets
    {
        /// <summary>Mean radius in kilometres.</summary>
        public static double MeanRadiusKm(this Planet planet) => Physical[planet].MeanRadiusKm;

        /// <summary>Right ascension of the IAU north pole (degrees, ICRF).</summary>
        public static double PoleRaDeg(this Planet planet) => Physical[planet].PoleRaDeg;

        /// <summary>Declination of the IAU north pole (degrees, ICRF).</summary>
        public static double PoleDecDeg(this Planet planet) => Physical[planet].PoleDecDeg;

        /// <summary>Sidereal spin rate in degrees/day; negative for retrograde rotation.</summary>
        public static double SpinDegPerDay(this Planet planet) => Physical[planet].SpinDegPerDay;

        /// <summary>Heliocentric ecliptic position of <paramref name="planet"/> at Julian Day <paramref name="jd"/>.</summary>
        public static PlanetPosition PositionAt(Planet planet, double jd)
        {
            var t = AstroTime.JulianCenturies(jd);
            var e = ElementsFor[planet];

            var a = e.A + e.ADot * t;
            var ecc = e.E + e.EDot * t;
            var inc = ToRadians(e.I + e.IDot * t);
            var l = e.L + e.LDot * t;
            var varpi = e.Varpi + e.VarpiDot * t;
            var omega = ToRadians(e.Node + e.NodeDot * t);              // Ω, longitude of ascending node
            var argPeri = ToRadians(varpi - (e.Node + e.NodeDot * t));  // ω = ϖ − Ω

            // Mean anomaly, normalized to (−180°, 180°] for a well-behaved Newton start.
            var m = NormalizeDegrees(l - varpi);
            if (m > 180.0) m -= 360.0;
            var mRad = ToRadians(m);

            // Kepler's equation M = E − e·sinE. Newton–Raphson from E₀ = M + e·sinM; every planetary
            // eccentricity here is < 0.21, where this converges quadratically — 8 steps is far past
            // double precision, no early-exit bookkeeping needed.
            var eAnom = mRad + ecc * Math.Sin(mRad);
            for (var i = 0; i < 8; i++)
            {
                eAnom -= (eAnom - ecc * Math.Sin(eAnom) - mRad) / (1.0 - ecc * Math.Cos(eAnom));
            }

            // Orbital-plane coordinates (x' toward perihelion).
            var xp = a * (Math.Cos(eAnom) - ecc);
            var yp = a * Math.Sqrt(1.0 - ecc * ecc) * Math.Sin(eAnom);

            // Rotate by ω (in plane), then i (about the node line), then Ω (in the ecliptic).
            var cosW = Math.Cos(argPeri);
            var sinW = Math.Sin(argPeri);
            var cosI = Math.Cos(inc);
            var sinI = Math.Sin(inc);
            var cosO = Math.Cos(omega);
            var sinO = Math.Sin(omega);
            var x = (cosW * cosO - sinW * sinO * cosI) * xp + (-sinW * cosO - cosW * sinO * cosI) * yp;
            var y = (cosW * sinO + sinW * cosO * cosI) * xp + (-sinW * sinO + cosW * cosO * cosI) * yp;
            var z = sinW * sinI * xp + cosW * sinI * yp;

            return new PlanetPosition(
                X: x,
                Y: y,
                Z: z,
                RadiusAu: Math.Sqrt(x * x + y * y + z * z),
                LongitudeDeg: NormalizeDegrees(ToDegrees(Math.Atan2(y, x))));
        }

        /// <summary>
        /// Orbital period in Julian years: LDot is °/century, so a 360° sweep takes 360/LDot centuries.
        /// </summary>
        public static double OrbitalPeriodYears(Planet planet) => 360.0 / ElementsFor[planet].LDot * 100.0;

        /// <summary>
        /// Geocentric equatorial RA/Dec (degrees, equinox of date) of <paramref name="planet"/> at Julian
        /// Day <paramref name="jd"/>: heliocentric planet − heliocentric Earth, converted from the ecliptic
        /// frame with the mean obliquity. Light-time and aberration are ignored (≤ ~0.01°) — far below
        /// what a sky chart or constellation lookup can show.
        /// </summary>
        public static Equatorial GeocentricEquatorial(Planet planet, double jd)
        {
            var e = PositionAt(Planet.Earth, jd);
            var p = PositionAt(planet, jd);
            var x = p.X - e.X;
            var y = p.Y - e.Y;
            var z = p.Z - e.Z;
            var lonDeg = NormalizeDegrees(ToDegrees(Math.Atan2(y, x)));
            var latDeg = ToDegrees(Math.Atan2(z, Math.Sqrt(x * x + y * y)));
            return Coordinates.EclipticToEquatorial(lonDeg, latDeg, Nutation.MeanObliquityDeg(jd));
        }

        /// <summary>
        /// The planet's meridian angle W (degrees) at <paramref name="jd"/>: the accumulated spin about
        /// its own axis. The zero point is arbitrary for visualization (a surface marker's phase can't be
        /// checked by eye), but the <i>rate</i> is the true IAU sidereal rotation rate, so relative spin
        /// speeds across planets — and against the time controls — are real.
        /// </summary>
        public static double MeridianAngleDeg(Planet planet, double jd) =>
            NormalizeDegrees(planet.SpinDegPerDay() * (jd - AstroTime.J2000));

        /// <summary>Static physical data per planet.</summary>
        private readonly struct PhysicalData
        {
            public PhysicalData(double meanRadiusKm, double poleRaDeg, double poleDecDeg, double spinDegPerDay)
            {
                MeanRadiusKm = meanRadiusKm;
                PoleRaDeg = poleRaDeg;
                PoleDecDeg = poleDecDeg;
                SpinDegPerDay = spinDegPerDay;
            }

            public double MeanRadiusKm { get; }

            public double PoleRaDeg { get; }

            public double PoleDecDeg { get; }

            public double SpinDegPerDay { get; }
        }

        private static readonly IReadOnlyDictionary<Planet, PhysicalData> Physical =
            new Dictionary<Planet, PhysicalData>
            {
                [Planet.Mercury] = new PhysicalData(2439.7, 281.01, 61.45, 6.1385025),
                [Planet.Venus] = new PhysicalData(6051.8, 272.76, 67.16, -1.4813688),
                [Planet.Earth] = new PhysicalData(6371.0, 0.0, 90.0, 360.9856235),
                [Planet.Mars] = new PhysicalData(3389.5, 317.68, 52.89, 350.89198226),
                [Planet.Jupiter] = new PhysicalData(69911.0, 268.057, 64.495, 870.536),
                [Planet.Saturn] = new PhysicalData(58232.0, 40.589, 83.537, 810.7939024),
                [Planet.Uranus] = new PhysicalData(25362.0, 257.311, -15.175, -501.1600928),
                [Planet.Neptune] = new PhysicalData(24622.0, 299.36, 43.46, 536.3128492),
            };

        /// <summary>Mean orbital elements at J2000 + centennial rates (degrees, AU; rates per Julian century).</summary>
        private readonly struct Elements
        {
            public Elements(
                double a, double aDot,
                double e, double eDot,
                double i, double iDot,
                double l, double lDot,
                double varpi, double varpiDot,
                double node, double nodeDot)
            {
                A = a;
                ADot = aDot;
                E = e;
                EDot = eDot;
                I = i;
                IDot = iDot;
                L = l;
                LDot = lDot;
                Varpi = varpi;
                VarpiDot = varpiDot;
                Node = node;
                NodeDot = nodeDot;
            }

            public double A { get; }

            public double ADot { get; }

            public double E { get; }

            public double EDot { get; }

            public double I { get; }

            public double IDot { get; }

            public double L { get; }

            public double LDot { get; }

            public double Varpi { get; }

            public double VarpiDot { get; }

            public double Node { get; }

            public double NodeDot { get; }
        }

        private static readonly IReadOnlyDictionary<Planet, Elements> ElementsFor =
            new Dictionary<Planet, Elements>
            {
                [Planet.Mercury] = new Elements(
                    0.38709927, 0.00000037, 0.20563593, 0.00001906, 7.00497902, -0.00594749,
                    252.25032350, 149472.67411175, 77.45779628, 0.16047689, 48.33076593, -0.12534081),
                [Planet.Venus] = new Elements(
                    0.72333566, 0.00000390, 0.00677672, -0.00004107, 3.39467605, -0.00078890,
                    181.97909950, 58517.81538729, 131.60246718, 0.00268329, 76.67984255, -0.27769418),
                [Planet.Earth] = new Elements( // Earth–Moon barycenter
                    1.00000261, 0.00000562, 0.01671123, -0.00004392, -0.00001531, -0.01294668,
                    100.46457166, 35999.37244981, 102.93768193, 0.32327364, 0.0, 0.0),
                [Planet.Mars] = new Elements(
                    1.52371034, 0.00001847, 0.09339410, 0.00007882, 1.84969142, -0.00813131,
                    -4.55343205, 19140.30268499, -23.94362959, 0.44441088, 49.55953891, -0.29257343),
                [Planet.Jupiter] = new Elements(
                    5.20288700, -0.00011607, 0.04838624, -0.00013253, 1.30439695, -0.00183714,
                    34.39644051, 3034.74612775, 14.72847983, 0.21252668, 100.47390909, 0.20469106),
                [Planet.Saturn] = new Elements(
                    9.53667594, -0.00125060, 0.05386179, -0.00050991, 2.48599187, 0.00193609,
                    49.95424423, 1222.49362201, 92.59887831, -0.41897216, 113.66242448, -0.28867794),
                [Planet.Uranus] = new Elements(
                    19.18916464, -0.00196176, 0.04725744, -0.00004397, 0.77263783, -0.00242939,
                    313.23810451, 428.48202785, 170.95427630, 0.40805281, 74.01692503, 0.04240589),
                [Planet.Neptune] = new Elements(
                    30.06992276, 0.00026291, 0.00859048, 0.00005105, 1.77004347, 0.00035372,
                    -55.12002969, 218.45945325, 44.96476227, -0.32241464, 131.78422574, -0.00508664),
            };
    }
}
