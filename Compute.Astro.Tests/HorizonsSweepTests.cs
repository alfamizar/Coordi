namespace Compute.Astro.Tests;

/// <summary>
/// Bulk cross-validation of the truncated-series ephemeris against an independent
/// authority: NASA/JPL Horizons (DE441), sampled across years in <see cref="HorizonsFixture"/>.
/// Where the Meeus/NASA anchor tests pin a handful of points, this sweeps ~145 and
/// asserts the <i>worst</i> deviation stays within the series' documented accuracy — the
/// net that catches subtle drift (a mistyped coefficient, a dropped periodic term)
/// that single points can slip past.
///
/// Positions are compared as a great-circle angular separation between the engine's
/// direction and Horizons', so the metric is immune to RA wraparound and to azimuth
/// becoming ill-defined near the zenith/nadir.
/// </summary>
public class HorizonsSweepTests
{
    /// <summary>Great-circle central angle (degrees) between two (lon, lat) directions.</summary>
    private static double SeparationDeg(double lon1, double lat1, double lon2, double lat2)
    {
        var p1 = lat1 * Math.PI / 180.0;
        var p2 = lat2 * Math.PI / 180.0;
        var dl = (lon1 - lon2) * Math.PI / 180.0;
        var termA = Math.Cos(p2) * Math.Sin(dl);
        var termB = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
        var num = Math.Sqrt(termA * termA + termB * termB);
        var den = Math.Sin(p1) * Math.Sin(p2) + Math.Cos(p1) * Math.Cos(p2) * Math.Cos(dl);
        return Math.Atan2(num, den) * 180.0 / Math.PI;
    }

    private static double JdTd(int y, int mo, int d, int h, int mi)
    {
        var jdUt = AstroTime.JulianDay(y, mo, d, h, mi);
        return jdUt + AstroTime.DeltaTSeconds(y, mo) / 86400.0;
    }

    [Fact]
    public void Sun_GeocentricApparentPosition_MatchesHorizons()
    {
        var maxSep = 0.0;
        foreach (var r in HorizonsFixture.SunGeocentric)
        {
            var pos = Sun.PositionAt(JdTd((int)r[0], (int)r[1], (int)r[2], (int)r[3], (int)r[4]));
            maxSep = Math.Max(maxSep, SeparationDeg(pos.RightAscension, pos.Declination, r[5], r[6]));
        }

        // Meeus low-precision solar series is documented to ≈0.01°.
        Assert.True(maxSep < 0.015, $"worst Sun position error {maxSep}° exceeds 0.015°");
    }

    [Fact]
    public void Moon_GeocentricApparentPosition_MatchesHorizons()
    {
        var maxSep = 0.0;
        var maxDistKm = 0.0;
        foreach (var r in HorizonsFixture.MoonGeocentric)
        {
            var jd = JdTd((int)r[0], (int)r[1], (int)r[2], (int)r[3], (int)r[4]);
            var pos = Moon.PositionAt(jd);
            maxSep = Math.Max(maxSep, SeparationDeg(pos.RightAscensionDeg, pos.DeclinationDeg, r[5], r[6]));
            maxDistKm = Math.Max(maxDistKm, Math.Abs(pos.DistanceKm - r[7]));
        }

        // Truncated ELP series: ~10″ in longitude ⇒ ~0.003° on the sky (observed).
        Assert.True(maxSep < 0.010, $"worst Moon position error {maxSep}° exceeds 0.010°");
        // The abridged distance series drops small periodic terms; ~40 km observed.
        Assert.True(maxDistKm < 60.0, $"worst Moon distance error {maxDistKm} km exceeds 60 km");
    }

    [Fact]
    public void Topocentric_AltAz_MatchesHorizons()
    {
        var maxSunSep = 0.0;
        var maxMoonSep = 0.0;
        foreach (var r in HorizonsFixture.Topocentric)
        {
            int y = (int)r[0], mo = (int)r[1], d = (int)r[2], h = (int)r[3], mi = (int)r[4];
            double lat = r[5], lonEast = r[6], refAz = r[7], refEl = r[8];
            var jdUt = AstroTime.JulianDay(y, mo, d, h, mi);
            if ((int)r[9] == 0)
            {
                var hz = HorizontalCoordinates.OfSun(jdUt, lat, lonEast);
                maxSunSep = Math.Max(maxSunSep, SeparationDeg(hz.AzimuthDeg, hz.AltitudeDeg, refAz, refEl));
            }
            else
            {
                var hz = HorizontalCoordinates.OfMoon(jdUt, lat, lonEast, topocentric: true);
                maxMoonSep = Math.Max(maxMoonSep, SeparationDeg(hz.AzimuthDeg, hz.AltitudeDeg, refAz, refEl));
            }
        }

        // Sun: solar-series error plus the ignored 8.8″ parallax (~0.009° observed).
        Assert.True(maxSunSep < 0.015, $"worst topocentric Sun error {maxSunSep}° exceeds 0.015°");
        // Moon: our sea-level ellipsoidal parallax reduction vs Horizons' full model.
        Assert.True(maxMoonSep < 0.012, $"worst topocentric Moon error {maxMoonSep}° exceeds 0.012°");
    }
}
