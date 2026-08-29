namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks for eclipse prediction, pinned to Meeus Example 54.a and to
/// well-documented real eclipses (type, gamma, magnitude, time of greatest eclipse in
/// TD, and NASA local circumstances).
/// </summary>
public class EclipseValidationTests
{
    /// <summary>UTC minutes-from-midnight of a Julian Day (contacts here stay within one UTC day).</summary>
    private static double UtcMinutes(double jd)
    {
        var c = AstroTime.CalendarFromJulianDay(jd);
        return c.Hour * 60.0 + c.Minute + c.Second / 60.0;
    }

    [Fact]
    public void Solar_MeeusExample54a_1993May21()
    {
        var e = Eclipse.SolarNear(-82);
        Assert.NotNull(e);
        Assert.Equal(SolarEclipseType.Partial, e!.Type);
        Assert.Equal(1.1348, e.Gamma, 1e-3);
        Assert.Equal(0.0097, e.U, 1e-3);
        Assert.Equal(0.740, e.Magnitude, 1e-3);
        Assert.Equal(2449129.0978, e.JdeMaximum, 2e-3); // 1993-05-21 14:20 TD
    }

    [Fact]
    public void Solar_TotalEclipses()
    {
        var e2017 = Eclipse.SolarNear(218);
        Assert.NotNull(e2017);
        Assert.Equal(SolarEclipseType.Total, e2017!.Type);
        Assert.Equal(0.4364, e2017.Gamma, 5e-3);

        var e2024 = Eclipse.SolarNear(300);
        Assert.NotNull(e2024);
        Assert.Equal(SolarEclipseType.Total, e2024!.Type);
        Assert.Equal(0.3437, e2024.Gamma, 5e-3);
    }

    [Fact]
    public void Solar_NonCentralAnnular_2014Apr29()
    {
        // 2014-04-29: the shadow axis missed Earth (|γ| ≈ 1.000 > 0.9972) but the
        // antumbra grazed Antarctica — NASA classifies it ANNULAR (non-central), not partial.
        var e = Eclipse.SolarNear(177);
        Assert.NotNull(e);
        Assert.Equal(SolarEclipseType.Annular, e!.Type);
        Assert.Equal(1.000, Math.Abs(e.Gamma), 5e-3);
    }

    [Fact]
    public void Solar_NoEclipseAwayFromNode()
    {
        // New moon of May 2024, between eclipse seasons.
        Assert.Null(Eclipse.SolarNear(301));
    }

    [Fact]
    public void Lunar_TotalAndPartial()
    {
        var total = Eclipse.LunarNear(235); // 2019-01-21 total
        Assert.NotNull(total);
        Assert.Equal(LunarEclipseType.Total, total!.Type);
        Assert.Equal(1.193, total.UmbralMagnitude, 5e-3);

        var partial = Eclipse.LunarNear(305); // 2024-09-18 partial (umbral)
        Assert.NotNull(partial);
        Assert.Equal(LunarEclipseType.Partial, partial!.Type);
        Assert.Equal(0.078, partial.UmbralMagnitude, 5e-3);
    }

    [Fact]
    public void Enumerate_SolarEclipsesOf2024()
    {
        var start = AstroTime.JulianDay(2024, 1, 1);
        var end = AstroTime.JulianDay(2024, 12, 31);
        var solar = Eclipse.SolarEclipsesBetween(start, end);
        // 2024 had two solar eclipses: total (Apr 8) and annular (Oct 2).
        Assert.Equal(2, solar.Count);
        Assert.Contains(solar, s => s.Type == SolarEclipseType.Total);
        Assert.Contains(solar, s => s.Type == SolarEclipseType.Annular);
    }

    [Fact]
    public void LunarContacts_Total_2019Jan21()
    {
        // NASA contact times (UT) for the 2019-01-21 total lunar eclipse.
        var e = Eclipse.LunarCircumstances(235);
        Assert.NotNull(e);
        Assert.Equal(LunarEclipseType.Total, e!.Type);
        Assert.Equal(156.0, UtcMinutes(e.PenumbralBeginJdUtc!.Value), 5.0); // 02:36
        Assert.Equal(214.0, UtcMinutes(e.PartialBeginJdUtc!.Value), 5.0);   // 03:34
        Assert.Equal(281.0, UtcMinutes(e.TotalBeginJdUtc!.Value), 5.0);     // 04:41
        Assert.Equal(312.0, UtcMinutes(e.MaximumJdUtc), 5.0);               // 05:12
        Assert.Equal(343.0, UtcMinutes(e.TotalEndJdUtc!.Value), 5.0);       // 05:43
        Assert.Equal(411.0, UtcMinutes(e.PartialEndJdUtc!.Value), 5.0);     // 06:51
        Assert.Equal(468.0, UtcMinutes(e.PenumbralEndJdUtc!.Value), 5.0);   // 07:48
    }

    [Fact]
    public void LunarContacts_Partial_2024Sep18()
    {
        // NASA contact times (UT) for the 2024-09-18 partial lunar eclipse.
        var e = Eclipse.LunarCircumstances(305);
        Assert.NotNull(e);
        Assert.Equal(LunarEclipseType.Partial, e!.Type);
        Assert.Null(e.TotalBeginJdUtc);
        Assert.Null(e.TotalEndJdUtc);
        Assert.Equal(41.0, UtcMinutes(e.PenumbralBeginJdUtc!.Value), 5.0); // 00:41
        Assert.Equal(133.0, UtcMinutes(e.PartialBeginJdUtc!.Value), 5.0);  // 02:13
        Assert.Equal(164.0, UtcMinutes(e.MaximumJdUtc), 5.0);              // 02:44
        Assert.Equal(196.0, UtcMinutes(e.PartialEndJdUtc!.Value), 5.0);    // 03:16
        Assert.Equal(287.0, UtcMinutes(e.PenumbralEndJdUtc!.Value), 5.0);  // 04:47
    }

    [Fact]
    public void SolarLocal_2024Apr08_VisibilityByLocation()
    {
        // 2024-04-08 total solar eclipse, greatest eclipse ≈ 18:17 UTC.
        var dallas = Eclipse.SolarCircumstances(300, 32.78, -96.80);
        Assert.NotNull(dallas);
        Assert.Equal(SolarEclipseType.Total, dallas!.Type);
        Assert.Equal(1097.0, UtcMinutes(dallas.MaximumJdUtc), 8.0); // 18:17 UTC
        Assert.True(dallas.VisibleAtMaximum);                       // afternoon Sun, high up
        Assert.True(dallas.SunAltitudeAtMaxDeg > 20.0);

        // Tokyo: it is the middle of the night there at 18:17 UTC — not visible.
        var tokyo = Eclipse.SolarCircumstances(300, 35.68, 139.69);
        Assert.NotNull(tokyo);
        Assert.False(tokyo!.VisibleAtMaximum);
        Assert.True(tokyo.SunAltitudeAtMaxDeg < 0.0);
    }

    [Fact]
    public void SolarLocal_2024Apr08_Dallas_TotalContactTimes()
    {
        // NASA / published local circumstances, Dallas TX (32.78, -96.80), 2024-04-08 (UT):
        // C1 17:23:14, C2 18:40:43, max 18:42:40, C3 18:44:32, C4 20:02:48; totality ≈ 3m49s.
        var e = Eclipse.SolarLocalCircumstances(300, 32.78, -96.80);
        Assert.NotNull(e);
        Assert.Equal(SolarEclipseLocalType.Total, e!.LocalType);
        Assert.True(e.Visible);
        Assert.Equal(1043.0, UtcMinutes(e.PartialBeginJdUtc!.Value), 3.0); // 17:23 C1
        Assert.Equal(1121.0, UtcMinutes(e.CentralBeginJdUtc!.Value), 3.0); // 18:41 C2
        Assert.Equal(1123.0, UtcMinutes(e.MaximumJdUtc!.Value), 3.0);      // 18:43 max
        Assert.Equal(1125.0, UtcMinutes(e.CentralEndJdUtc!.Value), 3.0);   // 18:45 C3
        Assert.Equal(1203.0, UtcMinutes(e.PartialEndJdUtc!.Value), 3.0);   // 20:03 C4
        Assert.Equal(229.0, e.CentralDurationSeconds!.Value, 40.0);        // ≈ 3m49s
        Assert.True(e.MagnitudeAtMax > 1.0);                               // total ⇒ ≥ 1
    }

    [Fact]
    public void SolarLocal_2024Apr08_PartialAndNotVisible()
    {
        // New York (40.71, -74.01) saw a deep partial, not totality.
        var ny = Eclipse.SolarLocalCircumstances(300, 40.71, -74.01);
        Assert.NotNull(ny);
        Assert.Equal(SolarEclipseLocalType.Partial, ny!.LocalType);
        Assert.True(ny.Visible);
        Assert.Null(ny.CentralBeginJdUtc);
        Assert.Null(ny.CentralDurationSeconds);
        Assert.InRange(ny.MagnitudeAtMax, 0.7, 1.0);
        // NASA NYC: partial 18:10:23 → max 19:25:34 → 20:36:31 (89.6% obscured).
        Assert.Equal(1090.0, UtcMinutes(ny.PartialBeginJdUtc!.Value), 3.0); // 18:10
        Assert.Equal(1166.0, UtcMinutes(ny.MaximumJdUtc!.Value), 3.0);      // 19:26
        Assert.Equal(1237.0, UtcMinutes(ny.PartialEndJdUtc!.Value), 3.0);   // 20:37

        // Tokyo: night-time at greatest eclipse ⇒ nothing visible.
        var tokyo = Eclipse.SolarLocalCircumstances(300, 35.68, 139.69);
        Assert.NotNull(tokyo);
        Assert.Equal(SolarEclipseLocalType.None, tokyo!.LocalType);
        Assert.False(tokyo.Visible);
        Assert.Null(tokyo.MaximumJdUtc);
    }

    [Fact]
    public void SolarLocal_2017Aug21_Carbondale_Total()
    {
        // 2017-08-21 total solar eclipse near greatest duration (Carbondale, IL).
        var e = Eclipse.SolarLocalCircumstances(218, 37.73, -89.22);
        Assert.NotNull(e);
        Assert.Equal(SolarEclipseLocalType.Total, e!.LocalType);
        Assert.True(e.Visible);
        Assert.NotNull(e.CentralDurationSeconds);
        Assert.True(e.CentralDurationSeconds!.Value > 120.0); // ~2m38s near greatest
    }

    [Fact]
    public void SolarLocal_2026Aug12_Lodz_SunSetsMidEclipse()
    {
        // Łódź, Poland (51°46′N, 19°30′E), the exact site from NASA's JSEX-EU page. A deep
        // partial where the Sun SETS during the eclipse: partial begins 17:15:38 UT (Sun alt
        // 7°), and greatest eclipse is clamped to sunset ≈ 18:07 UT (Sun alt 0°), mag 0.866.
        // Regression for the visibility bug — the local maximum must not dive below the horizon
        // to the geometric closest approach; it must clamp to the horizon and report visible.
        var start = AstroTime.JulianDay(2026, 1, 1);
        var end = AstroTime.JulianDay(2026, 12, 31);
        var visibleHere = Eclipse.SolarLocalCircumstancesBetween(start, end, 51.7667, 19.5)
            .Where(c => c.Visible)
            .ToList();
        Assert.Single(visibleHere); // only the Aug 12 partial is visible from Łódź in 2026
        var aug = visibleHere[0];
        Assert.Equal(SolarEclipseLocalType.Partial, aug.LocalType);
        Assert.True(aug.MagnitudeAtMax > 0.8, $"expected a deep partial, got {aug.MagnitudeAtMax}");
        Assert.Equal(1035.0, UtcMinutes(aug.PartialBeginJdUtc!.Value), 5.0); // 17:15 UT (C1)
        Assert.Equal(1087.0, UtcMinutes(aug.MaximumJdUtc!.Value), 5.0);      // ≈ 18:07 UT (clamped to sunset)
        Assert.True(Math.Abs(aug.SunAltitudeAtMaxDeg) < 2.0, $"Sun should be at the horizon at max, was {aug.SunAltitudeAtMaxDeg}");
    }

    [Fact]
    public void SolarLocal_EnumerateBetween_IncludesBoth2024Eclipses()
    {
        var start = AstroTime.JulianDay(2024, 1, 1);
        var end = AstroTime.JulianDay(2024, 12, 31);
        var list = Eclipse.SolarLocalCircumstancesBetween(start, end, 32.78, -96.80);
        Assert.Equal(2, list.Count); // Apr 8 total + Oct 2 annular
        Assert.Contains(list, c => c.GlobalType == SolarEclipseType.Total);
        Assert.Contains(list, c => c.GlobalType == SolarEclipseType.Annular);
    }

    [Fact]
    public void LunarCircumstancesBetween_Finds2024Eclipses()
    {
        var start = AstroTime.JulianDay(2024, 1, 1);
        var end = AstroTime.JulianDay(2024, 12, 31);
        var lunar = Eclipse.LunarEclipseCircumstancesBetween(start, end);
        // 2024: penumbral (Mar 25) + partial (Sep 18) are the umbral/penumbral pair.
        Assert.Contains(lunar, l => l.Type == LunarEclipseType.Partial);
        Assert.All(lunar, l => Assert.InRange(l.MaximumJdUtc, start, end));
    }
}
