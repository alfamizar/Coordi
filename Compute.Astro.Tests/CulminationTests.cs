namespace Compute.Astro.Tests;

/// <summary>
/// Rise, transit and set for a fixed point of sky.
///
/// Checked against <see cref="SunRiseSet"/>, which arrives at the same answers by an entirely
/// different route — its own iteration over the Sun's changing position, validated against
/// NASA — where this solves one equation for something that does not move. Feed it the Sun's
/// position frozen at noon and the two should agree; where they differ is where one of them
/// is wrong.
///
/// That is a real oracle rather than a restatement: nothing in <see cref="Culmination"/> knows
/// how <see cref="SunRiseSet"/> works, and the Sun is the one fixed-position case whose answer
/// is independently known.
/// </summary>
public class CulminationTests
{
    private static readonly (double Lat, double Lon) Paris = (48.8566, 2.3522);
    private static readonly (double Lat, double Lon) Sydney = (-33.8688, 151.2093);

    /// <summary>Julian Day of an event <see cref="SunRiseSet"/> reports in minutes from UTC midnight.</summary>
    private static double SunEventJd(int year, int month, int day, double minutes) =>
        AstroTime.JulianDay(year, month, day) + minutes / 1440.0;

    [Fact]
    public void SolarNoonAgreesWithTheSunsOwnRiseSetModel()
    {
        double worstSeconds = 0.0;

        foreach (var (lat, lon) in new[] { Paris, Sydney })
        {
            for (int month = 1; month <= 12; month++)
            {
                var events = SunRiseSet.Events(2026, month, 15, lat, lon);
                var noonJd = SunEventJd(2026, month, 15, events.SolarNoonUtcMinutes);

                // The Sun's position at that instant, then treated as a fixed point.
                var sun = Sun.PositionAt(noonJd);
                var transit = Culmination.TransitNear(noonJd, sun.RightAscension, lon);

                worstSeconds = Math.Max(worstSeconds, Math.Abs(transit - noonJd) * 86400.0);
            }
        }

        // The Sun drifts about a degree a day along the ecliptic, so freezing it at noon and
        // solving for the meridian cannot be exact — but it is close, and a transit that was
        // wrong in principle would be minutes out, not seconds.
        Assert.True(worstSeconds < 30.0, $"worst solar-noon disagreement {worstSeconds:N1}s");
    }

    [Fact]
    public void SunriseAndSunsetAgreeWithTheSunsOwnRiseSetModel()
    {
        double worstMinutes = 0.0;

        foreach (var (lat, lon) in new[] { Paris, Sydney })
        {
            for (int month = 1; month <= 12; month++)
            {
                var events = SunRiseSet.Events(2026, month, 15, lat, lon);
                if (events.SunriseUtcMinutes is not double rise) continue;
                if (events.SunsetUtcMinutes is not double set) continue;

                var noonJd = SunEventJd(2026, month, 15, events.SolarNoonUtcMinutes);
                var sun = Sun.PositionAt(noonJd);

                // SunAltitude.Official is the horizon that model uses: refraction plus the Sun's
                // own semi-diameter, neither of which a star has.
                var passage = Culmination.Near(
                    noonJd, sun.RightAscension, sun.Declination, lat, lon, SunAltitude.Official);

                worstMinutes = Math.Max(
                    worstMinutes, Math.Abs(passage.RiseJdUtc!.Value - SunEventJd(2026, month, 15, rise)) * 1440.0);
                worstMinutes = Math.Max(
                    worstMinutes, Math.Abs(passage.SetJdUtc!.Value - SunEventJd(2026, month, 15, set)) * 1440.0);
            }
        }

        // Larger than the noon figure and for the same reason, doubled: the Sun has moved half a
        // day's worth of declination by the time it reaches the horizon, and this model holds it
        // still. A star, which really does hold still, has no such error.
        Assert.True(worstMinutes < 4.0, $"worst sunrise/sunset disagreement {worstMinutes:N2} min");
    }

    [Fact]
    public void TransitAltitudeIsWhatTheGeometrySays()
    {
        // An object on the celestial equator passes through the zenith at the equator, and at the
        // co-latitude anywhere else.
        Assert.Equal(90.0, Culmination.TransitAltitudeDeg(0.0, 0.0), 9);
        Assert.Equal(41.1434, Culmination.TransitAltitudeDeg(0.0, 48.8566), 4);

        // The celestial pole stands at the observer's latitude, north or south.
        Assert.Equal(48.8566, Culmination.TransitAltitudeDeg(90.0, 48.8566), 9);
        Assert.Equal(33.8688, Culmination.TransitAltitudeDeg(-90.0, -33.8688), 9);
    }

    [Fact]
    public void CircumpolarAndNeverRisingAreToldApart()
    {
        var jd = AstroTime.JulianDay(2026, 8, 19);

        // Polaris from Paris: up all day, every day.
        var polaris = Culmination.Near(jd, 37.95, 89.26, Paris.Lat, Paris.Lon);
        Assert.True(polaris.Circumpolar, "Polaris should be circumpolar from Paris");
        Assert.Null(polaris.RiseJdUtc);
        Assert.Null(polaris.SetJdUtc);

        // The southern pole, from the same place: never.
        var south = Culmination.Near(jd, 0.0, -89.0, Paris.Lat, Paris.Lon);
        Assert.True(south.NeverRises, "the south celestial pole should never rise over Paris");
        Assert.True(south.TransitAltitudeDeg < 0.0);

        // And something ordinary does both.
        var orion = Culmination.Near(jd, 83.82, -5.39, Paris.Lat, Paris.Lon);   // M42
        Assert.False(orion.Circumpolar);
        Assert.False(orion.NeverRises);
        Assert.NotNull(orion.RiseJdUtc);
        Assert.NotNull(orion.SetJdUtc);
        Assert.True(orion.RiseJdUtc < orion.TransitJdUtc && orion.TransitJdUtc < orion.SetJdUtc);
    }

    /// <summary>The transit really is the maximum, not merely near it.</summary>
    [Fact]
    public void NothingInTheNightStandsHigherThanTheTransit()
    {
        var jd = AstroTime.JulianDay(2026, 12, 1);
        var (lat, lon) = Paris;

        foreach (var (ra, dec) in new[] { (83.82, -5.39), (10.68, 41.27), (250.42, 36.47) })
        {
            var passage = Culmination.Near(jd, ra, dec, lat, lon);
            var peak = Culmination.AltitudeDegAt(passage.TransitJdUtc, ra, dec, lat, lon);
            Assert.Equal(passage.TransitAltitudeDeg, peak, 6);

            foreach (var offsetMinutes in new[] { -90, -30, -5, 5, 30, 90 })
            {
                var other = Culmination.AltitudeDegAt(
                    passage.TransitJdUtc + offsetMinutes / 1440.0, ra, dec, lat, lon);
                Assert.True(other <= peak + 1e-9, $"higher {offsetMinutes}min off transit: {other} > {peak}");
            }
        }
    }

    /// <summary>A transit is found near the instant asked about, not near some other day's.</summary>
    [Fact]
    public void TheTransitReturnedIsTheNearestOne()
    {
        var (lat, lon) = Paris;
        var jd = AstroTime.JulianDay(2026, 3, 1);

        for (int i = 0; i < 40; i++)
        {
            var passage = Culmination.Near(jd, 279.23, 38.78, lat, lon);   // Vega
            Assert.True(
                Math.Abs(passage.TransitJdUtc - jd) <= 0.5,
                $"transit {passage.TransitJdUtc} is more than half a day from {jd}");
            jd += 0.37;
        }
    }
}
