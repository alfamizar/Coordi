namespace Compute.Astro.Tests;

/// <summary>
/// ΔT is the one number in this library that cannot be derived from anything — it is measured,
/// and the Earth changes its mind about it. These check the current values against the source
/// they came from, and check that fixing the present did not disturb the past.
/// </summary>
public class DeltaTTests
{
    [Fact]
    public void TheCurrentValueMatchesTheMeasuredOne()
    {
        // USNO's monthly series gives 69.1330 s for 2026 April. The published Espenak–Meeus
        // extrapolation, which this used to follow, says 75.4 — six seconds of a ninety-second
        // totality.
        var april2026 = AstroTime.DeltaTSeconds(2026, 4);
        Assert.True(Math.Abs(april2026 - 69.13) < 0.06, $"ΔT for 2026 April came out {april2026}, measured 69.133");
    }

    [Fact]
    public void TheNearFutureFollowsThePublishedPrediction()
    {
        // USNO predicts 69.97 s for 2030.0 and 70.98 for 2033.0.
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(2030, 1) - 69.97) < 0.15);
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(2033, 1) - 70.98) < 0.15);
    }

    [Fact]
    public void ItIsContinuousWhereTheModelsMeet()
    {
        // Across the joins — into the table at 2005, out of it after the last prediction — a
        // step would put a jump in every contact time either side of the boundary.
        foreach (var boundary in new[] { 2005, 2033 })
        {
            var before = AstroTime.DeltaTSeconds(boundary - 1, 12);
            var after = AstroTime.DeltaTSeconds(boundary, 1);
            Assert.True(Math.Abs(after - before) < 0.5, $"ΔT jumps {after - before} s at {boundary}");
        }
    }

    [Fact]
    public void ItRisesSmoothlyThroughTheDecadesAhead()
    {
        var previous = AstroTime.DeltaTSeconds(2033, 1);
        for (var year = 2034; year <= 2100; year++)
        {
            var value = AstroTime.DeltaTSeconds(year, 1);
            Assert.True(value > previous - 0.5, $"ΔT fell at {year}: {previous} → {value}");
            Assert.True(value - previous < 3.0, $"ΔT jumped {value - previous} s at {year}");
            previous = value;
        }
    }

    [Fact]
    public void ThePastIsUntouched()
    {
        // Espenak–Meeus, unchanged, checked against its own published anchors.
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(1600, 1) - 120.0) < 1.0);
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(1900, 1) + 2.8) < 1.0);
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(1970, 1) - 40.2) < 1.0);
        Assert.True(Math.Abs(AstroTime.DeltaTSeconds(2000, 1) - 63.8) < 0.5);
    }
}
