namespace Compute.Astro.Tests;

/// <summary>
/// Validates the alignment search by self-consistency against <see cref="HorizontalCoordinates"/>
/// (the search must find back a moment whose position was computed directly) and by checking that
/// every returned moment actually satisfies the requested constraints.
/// </summary>
public class SkySearchValidationTests
{
    private const double Lat = 51.77; // Łódź
    private const double Lon = 19.46;

    /// <summary>
    /// Compute where the Sun is at a chosen instant, then ask the search to find that exact
    /// configuration — it must return the same instant to within a second or two.
    /// </summary>
    [Fact]
    public void SearchFindsBackADirectlyComputedSunPosition()
    {
        var t0 = AstroTime.JulianDay(2026, 8, 12, 17, 0);
        var at = HorizontalCoordinates.OfSun(t0, Lat, Lon, applyRefraction: true);
        var hits = SkySearch.FindAlignments(
            SkyBody.Sun, t0 - 0.4, t0 + 0.4, Lat, Lon,
            azimuthDeg: at.AzimuthDeg, altitudeDeg: at.AltitudeDeg, altitudeTolDeg: 0.5);
        Assert.NotEmpty(hits);
        var best = hits.MinBy(h => Math.Abs(h.JdUt - t0))!;
        Assert.True(Math.Abs(best.JdUt - t0) * 86400.0 < 2.0, $"found {(best.JdUt - t0) * 86400.0}s away");
        Assert.True(Math.Abs(best.AzimuthDeg - at.AzimuthDeg) < 0.01, $"azimuth off: {best.AzimuthDeg}");
    }

    /// <summary>
    /// The PhotoPills poster case: Moon crossing azimuth 246.8° at the horizon (0° ± 0.5°).
    /// Over a year there must be matches, every one satisfying the constraints exactly,
    /// in chronological order, each with a sane illumination.
    /// </summary>
    [Fact]
    public void MoonAtAzimuthOnTheHorizonOverAYear()
    {
        var start = AstroTime.JulianDay(2026, 7, 5);
        var hits = SkySearch.FindAlignments(
            SkyBody.Moon, start, start + 365.0, Lat, Lon,
            azimuthDeg: 246.8, altitudeDeg: 0.0, altitudeTolDeg: 0.5);
        Assert.NotEmpty(hits);
        var prev = start;
        foreach (var h in hits)
        {
            Assert.True(Math.Abs(h.AzimuthDeg - 246.8) < 0.01, $"azimuth {h.AzimuthDeg} not at target");
            Assert.True(Math.Abs(h.AltitudeDeg) <= 0.5, $"altitude {h.AltitudeDeg} outside band");
            Assert.InRange(h.JdUt, start, start + 365.0);
            Assert.True(h.JdUt > prev, "results not chronological");
            Assert.NotNull(h.Illumination);
            Assert.InRange(h.Illumination!.Value, 0.0, 1.0);
            prev = h.JdUt;
        }
    }

    /// <summary>
    /// The Sun due west at 10° ± 1°: the altitude at the due-west crossing drifts ~0.25°/day
    /// seasonally at this latitude, so a ±1° band catches roughly a week around each of the two
    /// yearly passages (~10 days total); all hits must comply.
    /// </summary>
    [Fact]
    public void SunDueWestAtTenDegrees()
    {
        var start = AstroTime.JulianDay(2026, 1, 1);
        var hits = SkySearch.FindAlignments(
            SkyBody.Sun, start, start + 365.0, Lat, Lon,
            azimuthDeg: 270.0, altitudeDeg: 10.0, altitudeTolDeg: 1.0);
        Assert.InRange(hits.Count, 6, 20);
        foreach (var h in hits)
        {
            Assert.True(Math.Abs(h.AzimuthDeg - 270.0) < 0.01);
            Assert.True(Math.Abs(h.AltitudeDeg - 10.0) <= 1.0);
            Assert.Null(h.Illumination);
        }
    }
}

/// <summary>Ground-track checks for the Besselian central-line solver.</summary>
public class EclipsePathTests
{
    /// <summary>
    /// The 2026-08-12 total solar eclipse: NASA gives greatest eclipse at 65.2°N, 25.2°W
    /// (17:46 UT), with the umbral path crossing Greenland, Iceland and northern Spain.
    /// The computed central line must pass close to that greatest-eclipse point.
    /// </summary>
    [Fact]
    public void CentralLineOf2026Aug12PassesNearIceland()
    {
        var jd = AstroTime.JulianDay(2026, 8, 12, 12, 0, 0.0);
        var path = Eclipse.SolarCentralPathNear(jd);
        Assert.True(path.Count > 30, $"expected a long central line, got {path.Count}");

        // Some point must land within ~2° of the published greatest-eclipse sub-point.
        var nearGreatest = path.Any(p => Math.Abs(p.LatDeg - 65.2) < 2.0 && AngularLonDiff(p.LonEastDeg, -25.2) < 3.0);
        Assert.True(nearGreatest, "central line missed greatest eclipse");

        // The track sweeps a wide latitude range (Arctic down toward Spain).
        var minLat = path.Min(p => p.LatDeg);
        var maxLat = path.Max(p => p.LatDeg);
        Assert.True(maxLat - minLat > 10.0, $"track latitude span too small: {minLat}..{maxLat}");
    }

    /// <summary>A purely partial eclipse has no central line.</summary>
    [Fact]
    public void PartialEclipseHasNoCentralLine()
    {
        // 2025-03-29 was a partial solar eclipse (no umbral path touches Earth).
        var jd = AstroTime.JulianDay(2025, 3, 29, 12, 0, 0.0);
        Assert.Empty(Eclipse.SolarCentralPathNear(jd));
    }

    private static double AngularLonDiff(double a, double b)
    {
        var d = (a - b + 540.0) % 360.0 - 180.0;
        return Math.Abs(d);
    }
}
