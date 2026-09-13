namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks against independently known values (Meeus worked examples and
/// canonical WGS84 figures). These are the oracle for the clean-room port.
/// </summary>
public class AstroValidationTests
{
    [Fact]
    public void JulianDay_ReferenceEpochs()
    {
        Assert.Equal(2451545.0, AstroTime.JulianDay(2000, 1, 1, 12), 1e-6);
        Assert.Equal(2436116.31, AstroTime.JulianDay(1957, 10, 4, 19, 26, 24.0), 1e-2);
    }

    [Fact]
    public void JulianDay_RoundTripsProlepticGregorianAcrossHistory()
    {
        // JulianDay and CalendarFromJulianDay are both proleptic Gregorian; the
        // round-trip must be exact for any civil date, ancient or future.
        (int Month, int Day)[] dates = [(1, 15), (2, 28), (7, 4), (12, 31)];
        for (var year = -800; year <= 3000; year += 61)
        {
            foreach (var (month, day) in dates)
            {
                var jd = AstroTime.JulianDay(year, month, day, 12);
                var c = AstroTime.CalendarFromJulianDay(jd);
                Assert.Equal(year, c.Year);
                Assert.Equal(month, c.Month);
                Assert.Equal(day, c.Day);
                Assert.Equal(12, c.Hour);
            }
        }

        // The Gregorian reform anchor in proleptic terms: 1582-10-15 00:00 = JD 2299160.5.
        Assert.Equal(2299160.5, AstroTime.JulianDay(1582, 10, 15), 1e-9);
    }

    [Fact]
    public void JulianDay_FromDateTimeMatchesComponents()
    {
        var utc = new DateTime(1957, 10, 4, 19, 26, 24, DateTimeKind.Utc);
        Assert.Equal(AstroTime.JulianDay(1957, 10, 4, 19, 26, 24.0), AstroTime.JulianDay(utc), 1e-9);
        Assert.Equal(AstroTime.JulianDay(utc), AstroTime.JulianDay(new DateTimeOffset(utc)), 1e-9);
    }

    [Fact]
    public void DateTimeFromJulianDay_RoundTrips()
    {
        var utc = new DateTime(2024, 4, 8, 18, 17, 21, DateTimeKind.Utc);
        var back = AstroTime.DateTimeFromJulianDay(AstroTime.JulianDay(utc));
        Assert.Equal(DateTimeKind.Utc, back.Kind);
        Assert.True(Math.Abs((back - utc).TotalMilliseconds) < 2.0, $"round-trip drift {(back - utc).TotalMilliseconds} ms");
    }

    [Fact]
    public void SiderealTime_MeeusExample12a()
    {
        var jd = AstroTime.JulianDay(1987, 4, 10);
        Assert.Equal(197.693195, AstroTime.GreenwichMeanSiderealTimeDeg(jd), 1e-4);
    }

    [Fact]
    public void Sun_ObliquityAndDeclinationAtJ2000()
    {
        // Mean obliquity ε₀ at J2000 is the canonical 23°26'21.448" (Meeus eq. 22.2).
        Assert.Equal(23.4392911, Nutation.MeanObliquityDeg(AstroTime.J2000), 1e-6);

        var pos = Sun.PositionAt(AstroTime.J2000);
        // Sun.PositionAt returns the *apparent* obliquity; check it tracks the true
        // obliquity from the full series.
        Assert.Equal(Nutation.TrueObliquityDeg(AstroTime.J2000), pos.Obliquity, 1e-3);
        Assert.Equal(-23.03, pos.Declination, 0.05);
    }

    [Fact]
    public void Sun_EquationOfTime()
    {
        Assert.Equal(-14.23, Sun.EquationOfTimeMinutes(AstroTime.JulianDay(2024, 2, 11, 12)), 0.1);
        Assert.Equal(16.49, Sun.EquationOfTimeMinutes(AstroTime.JulianDay(2024, 11, 3, 12)), 0.1);
    }

    [Fact]
    public void Sunrise_LondonSummerSolstice()
    {
        var e = SunRiseSet.Events(2024, 6, 21, 51.5074, -0.1278);
        Assert.Equal(DayType.Normal, e.Type);
        Assert.Equal(223.0, e.SunriseUtcMinutes!.Value, 2.0); // 03:43 UTC
        Assert.Equal(1222.0, e.SunsetUtcMinutes!.Value, 2.0); // 20:22 UTC
    }

    [Fact]
    public void Sunrise_PolarCasesDoNotThrow()
    {
        // High Arctic in midsummer / midwinter must classify, not crash.
        var summer = SunRiseSet.Events(2024, 6, 21, 78.0, 15.0);
        var winter = SunRiseSet.Events(2024, 12, 21, 78.0, 15.0);
        Assert.Equal(DayType.MidnightSun, summer.Type);
        Assert.Equal(DayType.PolarNight, winter.Type);
    }

    [Fact]
    public void Vincenty_KnownWgs84Distances()
    {
        Assert.Equal(111319.4908, Geodesy.DistanceMeters(0.0, 0.0, 0.0, 1.0), 1e-3);
        Assert.Equal(110574.389, Geodesy.DistanceMeters(0.0, 0.0, 1.0, 0.0), 1e-2);
    }

    [Fact]
    public void Zodiac_Boundaries()
    {
        // Sun crosses into Cancer at the June solstice (λ = 90°).
        Assert.Equal(ZodiacSign.Cancer, Zodiac.FromEclipticLongitude(90.0));
        Assert.Equal(ZodiacSign.Aries, Zodiac.FromEclipticLongitude(0.0));
        Assert.Equal(ZodiacSign.Pisces, Zodiac.FromEclipticLongitude(359.9));
    }

    [Fact]
    public void Zodiac_SymbolsAndStartLongitudes()
    {
        Assert.Equal("♈", ZodiacSign.Aries.Symbol());
        Assert.Equal("♓", ZodiacSign.Pisces.Symbol());
        Assert.Equal(0, ZodiacSign.Aries.StartLongitudeDeg());
        Assert.Equal(210, ZodiacSign.Scorpio.StartLongitudeDeg());
    }
}
