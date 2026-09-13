namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks for the lunar routines. Position/illumination are pinned to
/// Meeus worked Examples 47.a / 48.a; phase and rise/set are pinned to known
/// real-world instants.
/// </summary>
public class MoonValidationTests
{
    // Meeus Example 47.a — 1992 April 12.0 TD (JDE 2448724.5).
    private const double Jde47A = 2448724.5;

    [Fact]
    public void MoonPosition_Example47a()
    {
        var p = Moon.PositionAt(Jde47A);
        Assert.Equal(-3.229126, p.LatitudeDeg, 1e-4);           // geometric, exact
        Assert.Equal(368409.7, p.DistanceKm, 0.5);
        Assert.Equal(0.991990, p.HorizontalParallaxDeg, 1e-4);
        Assert.Equal(133.16723, p.ApparentLongitudeDeg, 2e-3);  // incl. nutation Δψ
    }

    [Fact]
    public void MoonPosition_ApparentEquatorial_Example47a()
    {
        var p = Moon.PositionAt(Jde47A);
        Assert.Equal(134.68847, p.RightAscensionDeg, 5e-3);
        Assert.Equal(13.76837, p.DeclinationDeg, 5e-3);
    }

    [Fact]
    public void MoonIllumination_Example48a()
    {
        Assert.Equal(0.6786, Moon.IlluminatedFraction(Jde47A), 2e-3);
        Assert.InRange(Moon.PhaseAngle(Jde47A), 68.5, 69.5);
    }

    [Fact]
    public void Phase_NewMoon_MeeusExample49a()
    {
        // New Moon of February 1977 (Meeus 49.a): 1977-02-18 03:37 TD.
        Assert.Equal(2443192.65118, MoonPhase.Jde(-283, MoonPhaseType.New), 1e-3);
    }

    [Fact]
    public void Phase_RealWorldAnchors()
    {
        // 2017-08-21 total solar eclipse new moon (~18:30 UT + ΔT).
        Assert.Equal(2457987.2718, MoonPhase.Jde(218, MoonPhaseType.New), 3e-3);
        // 2024-09-18 full moon 02:34 UT.
        Assert.Equal(2460571.6074, MoonPhase.Jde(305, MoonPhaseType.Full), 3e-3);
        // 2024-09-11 first quarter 06:06 UT.
        Assert.Equal(2460564.7542, MoonPhase.Jde(305, MoonPhaseType.FirstQuarter), 3e-3);
    }

    [Fact]
    public void PhasesNear_ReturnsOrderedDistinctPhases()
    {
        var phases = MoonPhase.PhasesNear(AstroTime.JulianDay(2024, 9, 15));
        Assert.True(phases.Count >= 4);
        for (var i = 1; i < phases.Count; i++)
        {
            Assert.True(phases[i - 1].Jde <= phases[i].Jde, "phases must be time-ordered");
        }
    }

    [Fact]
    public void MoonRiseSet_LondonFullMoon()
    {
        var e = MoonRiseSet.Events(2024, 9, 18, 51.5074, -0.1278);
        Assert.Equal(MoonDayType.Normal, e.Type);
        Assert.Equal(1099.0, e.MoonriseUtcMinutes!.Value, 2.0); // 18:19 UT — at sunset for this full Moon
        Assert.Equal(354.0, e.MoonsetUtcMinutes!.Value, 2.0);   // 05:54 UT
    }

    /// <summary>Local-day window: Łódź (UTC+2), 2026-06-24 → moonrise 16:01 / moonset 00:48 (per Apple).</summary>
    [Fact]
    public void MoonRiseSet_LodzLocalDayWindow()
    {
        var e = MoonRiseSet.Events(2026, 6, 24, 51.7667, 19.5, utcOffsetMinutes: 120);
        static double Local(double utcMin) => ((utcMin + 120.0) % 1440.0 + 1440.0) % 1440.0;

        Assert.NotNull(e.MoonriseUtcMinutes);
        Assert.NotNull(e.MoonsetUtcMinutes);
        Assert.Equal((16 * 60) + 1.0, Local(e.MoonriseUtcMinutes!.Value), 5.0); // 16:01
        Assert.Equal(48.0, Local(e.MoonsetUtcMinutes!.Value), 8.0);             // 00:48
    }

    [Fact]
    public void MoonPhaseName_TracksIlluminationAndWaxing()
    {
        Assert.Equal(MoonPhaseName.New, MoonPhaseNaming.Of(0.0, waxing: true));
        Assert.Equal(MoonPhaseName.Full, MoonPhaseNaming.Of(1.0, waxing: false));
        Assert.Equal(MoonPhaseName.FirstQuarter, MoonPhaseNaming.Of(0.5, waxing: true));
        Assert.Equal(MoonPhaseName.LastQuarter, MoonPhaseNaming.Of(0.5, waxing: false));
        Assert.Equal(MoonPhaseName.WaxingCrescent, MoonPhaseNaming.Of(0.25, waxing: true));
        Assert.Equal(MoonPhaseName.WaningGibbous, MoonPhaseNaming.Of(0.75, waxing: false));

        // The 2024-09-18 full moon must read as Full from the ephemeris itself.
        Assert.Equal(MoonPhaseName.Full, MoonPhaseNaming.At(AstroTime.JulianDay(2024, 9, 18, 2, 34)));
    }
}
