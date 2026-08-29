namespace Compute.Astro.Tests;

/// <summary>
/// Checks for the piecewise Espenak–Meeus ΔT model against the observed historical
/// record (IERS / Astronomical Almanac values) and for continuity at the segment
/// boundaries of the fit.
/// </summary>
public class DeltaTValidationTests
{
    [Fact]
    public void DeltaT_MatchesObservedHistoricalRecord()
    {
        // Observed ΔT (seconds); generous tolerances reflect the fit's residuals.
        Assert.Equal(120.0, AstroTime.DeltaTSeconds(1600, 1), 3.0);
        Assert.Equal(9.0, AstroTime.DeltaTSeconds(1700, 1), 2.0);
        Assert.Equal(13.7, AstroTime.DeltaTSeconds(1800, 1), 2.0);
        Assert.Equal(7.9, AstroTime.DeltaTSeconds(1860, 1), 2.0);
        Assert.Equal(-2.7, AstroTime.DeltaTSeconds(1900, 1), 1.5);
        Assert.Equal(24.0, AstroTime.DeltaTSeconds(1940, 1), 1.5);
        Assert.Equal(31.1, AstroTime.DeltaTSeconds(1955, 7), 1.0);
        Assert.Equal(45.5, AstroTime.DeltaTSeconds(1975, 1), 1.0);
        Assert.Equal(63.8, AstroTime.DeltaTSeconds(2000, 1), 1.0);
        // The 2005–2050 segment is an extrapolation; the real curve flattened,
        // so only sanity-check the modern era loosely.
        Assert.Equal(69.0, AstroTime.DeltaTSeconds(2020, 7), 4.0);
    }

    [Fact]
    public void DeltaT_AncientAndFarFutureAreSane()
    {
        // −1000 (Five Millennium Canon parabola): ΔT ≈ 25400 s.
        var ancient = AstroTime.DeltaTSeconds(-1000, 7);
        Assert.InRange(ancient, 24000.0, 27000.0);
        // Far future keeps growing quadratically, no discontinuity or sign flip.
        Assert.InRange(AstroTime.DeltaTSeconds(2500, 7), 1200.0, 2200.0);
        Assert.True(AstroTime.DeltaTSeconds(3000, 7) > AstroTime.DeltaTSeconds(2500, 7));
    }

    [Fact]
    public void DeltaT_IsContinuousAcrossSegmentBoundaries()
    {
        // Evaluate just either side of every boundary of the piecewise fit; the
        // published polynomials are constructed to join within a few seconds.
        int[] boundaries = [-500, 500, 1600, 1700, 1800, 1860, 1900, 1920, 1941, 1961, 1986, 2005, 2050, 2150];
        foreach (var b in boundaries)
        {
            var before = AstroTime.DeltaTSeconds(b - 1, 12);
            var after = AstroTime.DeltaTSeconds(b, 1);
            Assert.True(Math.Abs(after - before) < 4.0, $"ΔT jump at {b}: {before}s → {after}s");
        }
    }

    [Fact]
    public void DeltaT_ModernSegmentUnchangedFromPreviousModel()
    {
        // 2005–2050 must still be the same polynomial the eclipse tests were
        // validated with (62.92 + 0.32217 t + 0.005589 t²).
        foreach (var year in new[] { 2005, 2017, 2024, 2026, 2049 })
        {
            var y = year + 6.5 / 12.0;
            var t = y - 2000.0;
            var legacy = 62.92 + 0.32217 * t + 0.005589 * t * t;
            Assert.Equal(legacy, AstroTime.DeltaTSeconds(year), 1e-9);
        }
    }
}
