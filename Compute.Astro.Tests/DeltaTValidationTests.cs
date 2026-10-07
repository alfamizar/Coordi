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
    public void DeltaT_ModernSegmentLeavesThePublishedExtrapolationBehind()
    {
        // This test used to assert the opposite: that 2005–2050 stayed on Espenak–Meeus'
        // extrapolation, 62.92 + 0.32217 t + 0.005589 t². That polynomial was fitted before the
        // Earth stopped slowing down, and it has been drifting away from the measured value ever
        // since — five seconds out by 2020, six by 2026, eleven by 2033. What it asserts now is
        // that the library no longer follows it, and by how much, so the divergence is on the
        // record rather than a surprise.
        foreach (var (year, drift) in new[] { (2005, 0.3), (2017, 2.5), (2026, 6.3), (2033, 8.0) })
        {
            var y = year + 6.5 / 12.0;
            var t = y - 2000.0;
            var extrapolation = 62.92 + 0.32217 * t + 0.005589 * t * t;
            var ours = AstroTime.DeltaTSeconds(year);
            Assert.True(
                Math.Abs(extrapolation - ours - drift) < 1.5,
                $"at {year} the extrapolation is {extrapolation}, we say {ours}; expected a gap near {drift} s");
        }
    }
}
