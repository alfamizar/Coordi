namespace Compute.Astro.Tests;

/// <summary>
/// Constellation classification, boundary data, precession round-trips and planetary sanity.
/// </summary>
public class ConstellationTests
{
    /// <summary>
    /// Sweep the Sun through a full year: every day it must sit in one of the 13 constellations
    /// the ecliptic actually crosses (the 12 zodiacal ones plus Ophiuchus). This exercises the
    /// whole zodiac band of the boundary data plus the B1875 precession path end to end.
    /// </summary>
    [Fact]
    public void SunStaysOnTheEclipticBand()
    {
        var eclipticBand = new HashSet<Constellation>
        {
            Constellation.PSC, Constellation.ARI, Constellation.TAU, Constellation.GEM,
            Constellation.CNC, Constellation.LEO, Constellation.VIR, Constellation.LIB,
            Constellation.SCO, Constellation.OPH, Constellation.SGR, Constellation.CAP,
            Constellation.AQR,
        };
        const double jd2026Jan1 = 2461041.5; // 2026-01-01 00:00 UT
        var visited = new HashSet<Constellation>();
        for (var day = 0; day < 366; day++)
        {
            var jd = jd2026Jan1 + day;
            var c = Sun.ConstellationAt(jd);
            Assert.True(eclipticBand.Contains(c), $"day {day}: Sun in {c}, expected an ecliptic constellation");
            visited.Add(c);
        }

        // Over a full year the Sun must pass through the entire band.
        Assert.Equal(eclipticBand, visited);
    }

    /// <summary>A couple of fixed, well-inside-the-boundary dates with known answers.</summary>
    [Fact]
    public void SunKnownDates()
    {
        // 2026-01-05: Sun deep in Sagittarius (it leaves for Capricornus around Jan 19).
        Assert.Equal(Constellation.SGR, Sun.ConstellationAt(2461045.5));
        // 2026-12-08: Sun in Ophiuchus (roughly Nov 30 – Dec 18 each year).
        Assert.Equal(Constellation.OPH, Sun.ConstellationAt(2461382.5));
    }

    /// <summary>PrecessFromB1875 must invert PrecessToB1875 to sub-arcsecond accuracy.</summary>
    [Fact]
    public void PrecessionRoundTrip()
    {
        const double jd = 2461041.5; // 2026-01-01
        (double Ra, double Dec)[] points = [(0.0, 0.0), (101.3, -16.7), (266.4, -29.0), (310.0, 45.0), (37.9, 89.3)];
        foreach (var (ra, dec) in points)
        {
            var b = Coordinates.PrecessToB1875(jd, ra, dec);
            var back = Coordinates.PrecessFromB1875(jd, b.RightAscensionDeg, b.DeclinationDeg);
            var dRa = (back.RightAscensionDeg - ra + 540.0) % 360.0 - 180.0;
            Assert.True(Math.Abs(dRa) < 1e-4, $"RA drift {dRa} for ({ra},{dec})");
            Assert.True(Math.Abs(back.DeclinationDeg - dec) < 1e-4, $"Dec drift for ({ra},{dec})");
        }
    }

    /// <summary>Geocentric planet positions: inner planets stay near the Sun, sanity over a year.</summary>
    [Fact]
    public void GeocentricPlanetsStayNearEclipticAndSun()
    {
        const double jd0 = 2461041.5;
        for (var day = 0; day < 365; day += 10)
        {
            var jd = jd0 + day;
            var sun = Sun.PositionAt(jd);
            foreach (var planet in new[] { Planet.Mercury, Planet.Venus })
            {
                var p = Planets.GeocentricEquatorial(planet, jd);
                // Angular separation from the Sun stays within each planet's max elongation (+margin).
                var maxSep = planet == Planet.Mercury ? 29.0 : 48.5;
                var sep = AngularSeparationDeg(p.RightAscensionDeg, p.DeclinationDeg, sun.RightAscension, sun.Declination);
                Assert.True(sep < maxSep, $"{planet} {sep}° from Sun on day {day}");
            }

            // All planets stay within ~9° of the ecliptic → |dec| ≤ obliquity + 9.
            foreach (Planet planet in Enum.GetValues<Planet>())
            {
                if (planet == Planet.Earth) continue;
                var p = Planets.GeocentricEquatorial(planet, jd);
                Assert.True(Math.Abs(p.DeclinationDeg) < 33.0, $"{planet} dec {p.DeclinationDeg}");
            }
        }
    }

    private static double AngularSeparationDeg(double ra1, double dec1, double ra2, double dec2)
    {
        const double d2r = Math.PI / 180.0;
        var cosSep = Math.Sin(dec1 * d2r) * Math.Sin(dec2 * d2r) +
                     Math.Cos(dec1 * d2r) * Math.Cos(dec2 * d2r) * Math.Cos((ra1 - ra2) * d2r);
        return Math.Acos(Math.Clamp(cosSep, -1.0, 1.0)) / d2r;
    }

    [Fact]
    public void BrightStarsLandInTheirConstellations()
    {
        // Well-known stars with catalogue J2000 coordinates.
        AssertStar(Constellation.CMA, 6.0 + 45.0 / 60.0 + 8.9 / 3600.0, -(16.0 + 42.0 / 60.0 + 58.0 / 3600.0));   // Sirius
        AssertStar(Constellation.UMI, 2.0 + 31.0 / 60.0 + 49.09 / 3600.0, 89.0 + 15.0 / 60.0 + 50.8 / 3600.0);    // Polaris
        AssertStar(Constellation.ORI, 5.0 + 55.0 / 60.0 + 10.3 / 3600.0, 7.0 + 24.0 / 60.0 + 25.4 / 3600.0);      // Betelgeuse
        AssertStar(Constellation.LYR, 18.0 + 36.0 / 60.0 + 56.3 / 3600.0, 38.0 + 47.0 / 60.0 + 1.0 / 3600.0);     // Vega
        AssertStar(Constellation.SCO, 16.0 + 29.0 / 60.0 + 24.4 / 3600.0, -(26.0 + 25.0 / 60.0 + 55.0 / 3600.0)); // Antares

        static void AssertStar(Constellation expected, double raHours, double decDeg) =>
            Assert.Equal(expected, Constellations.Of(Coordinates.JdJ2000, raHours * 15.0, decDeg));
    }

    [Fact]
    public void FullNamesAreExposed()
    {
        Assert.Equal("Ursa Major", Constellation.UMA.FullName());
        Assert.Equal("Canis Major", Constellation.CMA.FullName());
        Assert.Equal("Coma Berenices", Constellation.COM.FullName());
        Assert.Equal(88, Enum.GetValues<Constellation>().Length);
    }

    [Fact]
    public void BoundaryPolygonsAreWellFormed()
    {
        foreach (Constellation c in Enum.GetValues<Constellation>())
        {
            var poly = Constellations.BoundaryB1875(c);
            Assert.True(poly.Count >= 4, $"{c} has only {poly.Count} vertices");
            foreach (var (ra, dec) in poly)
            {
                Assert.InRange(dec, -90.0, 90.0);
                // RA of wrapped polygons is stored unwrapped, so allow beyond 360°.
                Assert.InRange(ra, 0.0, 720.0);
            }
        }
    }
}
