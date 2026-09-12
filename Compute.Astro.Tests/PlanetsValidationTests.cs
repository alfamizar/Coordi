namespace Compute.Astro.Tests;

/// <summary>
/// Validates the Keplerian planetary ephemeris against independent truths: the in-house Sun
/// series (Earth's heliocentric longitude must oppose the Sun's geocentric one), textbook
/// orbital periods and distance extremes, and the observed 2020 Jupiter–Saturn great
/// conjunction as an external, date-stamped event.
/// </summary>
public class PlanetsValidationTests
{
    private static double AngleDiff(double a, double b)
    {
        var d = (a - b) % 360.0;
        if (d < 0) d += 360.0;
        if (d > 180.0) d -= 360.0;
        return Math.Abs(d);
    }

    /// <summary>
    /// Earth's heliocentric longitude must be the Sun's geocentric longitude + 180°. The table is
    /// J2000-frame while the Sun series is equinox-of-date, so general precession in longitude
    /// (≈5029.1″/century) is added before comparing; the residual must stay under 0.05°.
    /// </summary>
    [Fact]
    public void EarthOpposesTheSunSeries()
    {
        double[] samples =
        [
            AstroTime.JulianDay(2000, 1, 1, 12),
            AstroTime.JulianDay(2010, 6, 15),
            AstroTime.JulianDay(2020, 3, 20),
            AstroTime.JulianDay(2026, 8, 12, 18),
            AstroTime.JulianDay(2040, 11, 1),
        ];
        foreach (var jd in samples)
        {
            var earth = Planets.PositionAt(Planet.Earth, jd);
            var precessionDeg = 5029.0966 / 3600.0 * AstroTime.JulianCenturies(jd);
            var sun = Sun.PositionAt(jd).ApparentLongitude;
            var diff = AngleDiff(earth.LongitudeDeg + precessionDeg, sun + 180.0);
            Assert.True(diff < 0.05, $"Earth vs Sun mismatch {diff}° at jd={jd}");
        }
    }

    /// <summary>Orbital periods derived from the mean-longitude rates must match textbook values.</summary>
    [Fact]
    public void OrbitalPeriodsMatchTextbookValues()
    {
        var expected = new Dictionary<Planet, double>
        {
            [Planet.Mercury] = 0.2408,
            [Planet.Venus] = 0.6152,
            [Planet.Earth] = 1.0000,
            [Planet.Mars] = 1.8808,
            [Planet.Jupiter] = 11.862,
            [Planet.Saturn] = 29.457,
            [Planet.Uranus] = 84.01,
            [Planet.Neptune] = 164.8,
        };
        foreach (var (planet, years) in expected)
        {
            var got = Planets.OrbitalPeriodYears(planet);
            Assert.True(Math.Abs(got - years) / years < 0.01, $"{planet} period {got} ≠ {years} yr");
        }
    }

    /// <summary>
    /// Sampled distances stay inside each orbit's perihelion–aphelion band, and out-of-plane
    /// excursion stays under sin(i)·r.
    /// </summary>
    [Fact]
    public void DistancesAndLatitudesStayWithinOrbitalBounds()
    {
        var bounds = new Dictionary<Planet, (double Lo, double Hi)>
        {
            [Planet.Mercury] = (0.30, 0.47),
            [Planet.Venus] = (0.71, 0.73),
            [Planet.Earth] = (0.97, 1.02),
            [Planet.Mars] = (1.36, 1.67),
            [Planet.Jupiter] = (4.94, 5.46),
            [Planet.Saturn] = (9.0, 10.13),
            [Planet.Uranus] = (18.2, 20.11),
            [Planet.Neptune] = (29.7, 30.4),
        };
        var inclinations = new Dictionary<Planet, double>
        {
            [Planet.Mercury] = 7.1, [Planet.Venus] = 3.5, [Planet.Earth] = 0.1, [Planet.Mars] = 1.9,
            [Planet.Jupiter] = 1.4, [Planet.Saturn] = 2.6, [Planet.Uranus] = 0.9, [Planet.Neptune] = 1.8,
        };
        var jd = AstroTime.JulianDay(1900, 1, 1);
        var end = AstroTime.JulianDay(2050, 1, 1);
        while (jd < end)
        {
            foreach (Planet planet in Enum.GetValues<Planet>())
            {
                var p = Planets.PositionAt(planet, jd);
                var (lo, hi) = bounds[planet];
                Assert.True(p.RadiusAu >= lo && p.RadiusAu <= hi, $"{planet} r={p.RadiusAu} outside [{lo},{hi}] at jd={jd}");
                var maxZ = Math.Sin(inclinations[planet] * Math.PI / 180.0) * p.RadiusAu;
                Assert.True(Math.Abs(p.Z) <= maxZ, $"{planet} |z|={Math.Abs(p.Z)} exceeds sin(i)·r={maxZ} at jd={jd}");
            }

            jd += 500.0;
        }
    }

    /// <summary>
    /// The 2020-12-21 great conjunction: Jupiter and Saturn were ~6′ apart in the sky. Their
    /// geocentric ecliptic longitudes from this ephemeris must agree to a small fraction of a
    /// degree — an external, observed event pinning both outer-planet positions at once.
    /// </summary>
    [Fact]
    public void GreatConjunction2020()
    {
        var jd = AstroTime.JulianDay(2020, 12, 21, 18);
        var earth = Planets.PositionAt(Planet.Earth, jd);

        double GeocentricLon(Planet planet)
        {
            var p = Planets.PositionAt(planet, jd);
            var deg = Math.Atan2(p.Y - earth.Y, p.X - earth.X) * 180.0 / Math.PI;
            return (deg % 360.0 + 360.0) % 360.0;
        }

        var sep = AngleDiff(GeocentricLon(Planet.Jupiter), GeocentricLon(Planet.Saturn));
        Assert.True(sep < 0.25, $"Jupiter–Saturn geocentric separation {sep}° on 2020-12-21 (expected ≈0.1°)");
    }

    /// <summary>A planet returns to (nearly) the same heliocentric longitude after one derived period.</summary>
    [Fact]
    public void LongitudeClosesAfterOnePeriod()
    {
        var jd0 = AstroTime.JulianDay(2020, 1, 1);
        foreach (var planet in new[] { Planet.Mercury, Planet.Venus, Planet.Mars, Planet.Jupiter })
        {
            var period = Planets.OrbitalPeriodYears(planet) * 365.25;
            var l0 = Planets.PositionAt(planet, jd0).LongitudeDeg;
            var l1 = Planets.PositionAt(planet, jd0 + period).LongitudeDeg;
            Assert.True(AngleDiff(l0, l1) < 1.0, $"{planet} drifted {AngleDiff(l0, l1)}° over one period");
        }
    }

    /// <summary>
    /// Meridian angle advances at exactly the IAU spin rate — the invariant behind "self-rotation
    /// speed is preserved" in an orrery (Venus/Uranus negative = retrograde).
    /// </summary>
    [Fact]
    public void MeridianAdvancesAtTheIauSpinRate()
    {
        var jd = AstroTime.JulianDay(2026, 7, 4);
        foreach (Planet planet in Enum.GetValues<Planet>())
        {
            var perDay = Planets.MeridianAngleDeg(planet, jd + 1.0) - Planets.MeridianAngleDeg(planet, jd);
            perDay = (perDay % 360.0 + 360.0) % 360.0;
            var expected = (planet.SpinDegPerDay() % 360.0 + 360.0) % 360.0;
            Assert.True(AngleDiff(perDay, expected) < 1e-6, $"{planet} spin/day {perDay} ≠ {expected}");
        }
    }

    [Fact]
    public void PhysicalDataIsExposed()
    {
        Assert.Equal(6371.0, Planet.Earth.MeanRadiusKm());
        Assert.Equal(90.0, Planet.Earth.PoleDecDeg());
        Assert.True(Planet.Venus.SpinDegPerDay() < 0, "Venus rotates retrograde");
        Assert.True(Planet.Uranus.SpinDegPerDay() < 0, "Uranus rotates retrograde");
        Assert.Equal(69911.0, Planet.Jupiter.MeanRadiusKm());
        Assert.Equal(268.057, Planet.Jupiter.PoleRaDeg());
    }

    /// <summary>
    /// Distance from Earth, against two close approaches whose figures were published at the
    /// time: Mars on 2020-10-06 at 0.41492 AU (62.07 million km, its closest until 2035) and
    /// Jupiter's 2022-09-26 opposition at 3.9527 AU, the nearest in 59 years. Both are dated
    /// events, not quantities derived from this model.
    /// </summary>
    [Theory]
    [InlineData(2020, 10, 6, 14, Planet.Mars, 0.41492)]
    [InlineData(2022, 9, 26, 19, Planet.Jupiter, 3.95270)]
    public void GeocentricDistanceMatchesAPublishedApproach(
        int year, int month, int day, int hour, Planet planet, double expectedAu)
    {
        var d = Planets.GeocentricDistanceAu(planet, AstroTime.JulianDay(year, month, day, hour));

        Assert.InRange(d, expectedAu - 0.005, expectedAu + 0.005);
    }

    /// <summary>
    /// A triangle closes: however the two planets are arranged, the distance between them is at
    /// least the difference of their heliocentric radii and at most the sum. Cheap to state and
    /// it would catch a sign or a frame error anywhere in the difference vector.
    /// </summary>
    [Fact]
    public void GeocentricDistanceStaysWithinTheTriangle()
    {
        Planet[] planets = [Planet.Mercury, Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Uranus, Planet.Neptune];

        for (int step = 0; step < 60; step++)
        {
            var jd = AstroTime.JulianDay(2000, 1, 1) + step * 300.0;
            var earth = Planets.PositionAt(Planet.Earth, jd).RadiusAu;

            foreach (var planet in planets)
            {
                var r = Planets.PositionAt(planet, jd).RadiusAu;
                var d = Planets.GeocentricDistanceAu(planet, jd);

                Assert.InRange(d, Math.Abs(r - earth) - 1e-9, r + earth + 1e-9);
            }
        }
    }
}
