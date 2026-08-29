namespace Compute.Astro.Tests;

/// <summary>Shape and consistency checks for the star catalogue and stick-figure line data.</summary>
public class BrightStarsTests
{
    [Fact]
    public void CatalogueShape()
    {
        var all = BrightStars.All;
        Assert.InRange(all.Count, 700, 1100);
        // Brightest-first ordering, all within the packing limit.
        for (var i = 1; i < all.Count; i++)
        {
            Assert.True(all[i - 1].Magnitude <= all[i].Magnitude, "catalogue must be sorted brightest-first");
        }

        Assert.All(all, s => Assert.True(s.Magnitude <= 4.55));
        Assert.All(all, s =>
        {
            Assert.InRange(s.RaDeg, 0.0, 360.0);
            Assert.InRange(s.DecDeg, -90.0, 90.0);
        });
    }

    [Fact]
    public void FamousStarsAreWhereTheyBelong()
    {
        var byName = BrightStars.All.Where(s => s.Name != null).ToDictionary(s => s.Name!);

        var sirius = byName["Sirius"];
        Assert.True(sirius.Magnitude < -1.0);
        Assert.Equal(Constellation.CMA, Constellations.Of(Coordinates.JdJ2000, sirius.RaDeg, sirius.DecDeg));

        var vega = byName["Vega"];
        Assert.Equal(Constellation.LYR, Constellations.Of(Coordinates.JdJ2000, vega.RaDeg, vega.DecDeg));

        var polaris = byName["Polaris"];
        Assert.True(polaris.DecDeg > 89.0);
        Assert.Equal(Constellation.UMI, Constellations.Of(Coordinates.JdJ2000, polaris.RaDeg, polaris.DecDeg));

        var antares = byName["Antares"];
        Assert.Equal(Constellation.SCO, Constellations.Of(Coordinates.JdJ2000, antares.RaDeg, antares.DecDeg));
        Assert.True(antares.Bv > 1.2, $"Antares should be red, B-V {antares.Bv}");

        var betelgeuse = byName["Betelgeuse"];
        Assert.Equal(Constellation.ORI, Constellations.Of(Coordinates.JdJ2000, betelgeuse.RaDeg, betelgeuse.DecDeg));

        // Rigel is blue-white; the colour index must reflect it.
        Assert.True(byName["Rigel"].Bv < 0.2);
    }

    [Fact]
    public void ConstellationLinesShape()
    {
        var lines = ConstellationLines.Polylines;
        Assert.InRange(lines.Count, 100, 250);
        foreach (var line in lines)
        {
            Assert.True(line.Length >= 4 && line.Length % 2 == 0, $"malformed polyline of {line.Length} values");
            for (var i = 0; i < line.Length; i += 2)
            {
                Assert.InRange(line[i], 0.0, 360.0);
                Assert.InRange(line[i + 1], -90.0, 90.0);
            }
        }

        // Line vertices are bright-star positions: most should land within ~1° of a catalogue star.
        var stars = BrightStars.All;
        var near = 0;
        var total = 0;
        foreach (var line in lines)
        {
            for (var i = 0; i < line.Length; i += 2)
            {
                total++;
                var ra = line[i];
                var dec = line[i + 1];
                if (stars.Any(s => Math.Abs(s.DecDeg - dec) < 1.0 && Math.Abs((s.RaDeg - ra + 540.0) % 360.0 - 180.0) < 3.0))
                {
                    near++;
                }
            }
        }

        Assert.True((double)near / total > 0.8, $"only {near}/{total} line vertices near catalogue stars");
    }

    /// <summary>
    /// Every catalogued star must classify into some constellation without throwing, and
    /// precession from J2000 to 2026 must move stars only slightly.
    /// </summary>
    [Fact]
    public void ClassifierHandlesAllStars()
    {
        foreach (var s in BrightStars.All)
        {
            Constellations.Of(Coordinates.JdJ2000, s.RaDeg, s.DecDeg);
        }

        const double jd2026 = 2461041.5;
        var sirius = BrightStars.All.First(s => s.Name == "Sirius");
        var moved = Coordinates.PrecessEquatorial(Coordinates.JdJ2000, jd2026, sirius.RaDeg, sirius.DecDeg);
        Assert.True(Math.Abs(moved.DeclinationDeg - sirius.DecDeg) < 0.4);
    }
}
