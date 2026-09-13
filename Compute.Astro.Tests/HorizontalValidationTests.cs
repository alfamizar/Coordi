namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks for the horizontal-coordinates API. The transform itself is
/// pinned to Meeus worked Example 13.b; the Sun/Moon convenience functions are
/// cross-checked against the validated rise/set engines (internal consistency:
/// the altitude at a rise instant must equal that engine's event altitude).
/// </summary>
public class HorizontalValidationTests
{
    [Fact]
    public void FromHourAngle_MeeusExample13b()
    {
        // Venus from the US Naval Observatory, 1987-04-10 19:21 UT:
        // H = 64.352133°, δ = −6.719892°, φ = 38.9213° → A = 68.0337° (from South),
        // h = 15.1249°. Our azimuth is from North, so A = 248.0337°.
        var hz = HorizontalCoordinates.FromHourAngle(64.352133, -6.719892, 38.9213);
        Assert.Equal(248.0337, hz.AzimuthDeg, 1e-3);
        Assert.Equal(15.1249, hz.AltitudeDeg, 1e-3);
    }

    [Fact]
    public void Sun_TransitsDueSouthAtSolarNoon()
    {
        // London, 2026-06-21: at solar noon the Sun must be due south at
        // altitude 90° − φ + δ ≈ 61.93°.
        const double lat = 51.5074;
        const double lon = -0.1278;
        var noon = SunRiseSet.Events(2026, 6, 21, lat, lon).SolarNoonUtcMinutes;
        var jd = AstroTime.JulianDay(2026, 6, 21) + noon / 1440.0;
        var hz = HorizontalCoordinates.OfSun(jd, lat, lon);
        Assert.Equal(180.0, hz.AzimuthDeg, 1.0);
        Assert.Equal(61.93, hz.AltitudeDeg, 0.1);
    }

    [Fact]
    public void Sun_AltitudeAtSunriseMatchesEventAltitude()
    {
        // At the instant SunRiseSet reports as sunrise, the Sun's centre must be at
        // the official event altitude (−0.8333°), and the azimuth in the NE for a
        // London midsummer dawn (≈ 49°).
        const double lat = 51.5074;
        const double lon = -0.1278;
        var rise = SunRiseSet.Events(2026, 6, 21, lat, lon).SunriseUtcMinutes!.Value;
        var jd = AstroTime.JulianDay(2026, 6, 21) + rise / 1440.0;
        var hz = HorizontalCoordinates.OfSun(jd, lat, lon);
        Assert.Equal(SunAltitude.Official, hz.AltitudeDeg, 0.3);
        Assert.Equal(49.0, hz.AzimuthDeg, 2.5);
    }

    [Fact]
    public void Moon_GeocentricAltitudeAtMoonriseMatchesEventAltitude()
    {
        // At the instant MoonRiseSet reports as moonrise (London, 2024-09-18), the
        // geocentric altitude must equal the event threshold h₀ = 0.7275·π − 0.5667.
        const double lat = 51.5074;
        const double lon = -0.1278;
        var rise = MoonRiseSet.Events(2024, 9, 18, lat, lon).MoonriseUtcMinutes!.Value;
        var jdUt = AstroTime.JulianDay(2024, 9, 18) + rise / 1440.0;
        var deltaTDays = AstroTime.DeltaTSeconds(2024, 9) / 86400.0;
        var h0 = 0.7275 * Moon.PositionAt(jdUt + deltaTDays).HorizontalParallaxDeg - 0.5667;
        var hz = HorizontalCoordinates.OfMoon(jdUt, lat, lon, topocentric: false);
        Assert.Equal(h0, hz.AltitudeDeg, 0.01);
    }

    [Fact]
    public void Moon_TopocentricParallaxLowersAltitude()
    {
        // Topocentric altitude must sit below the geocentric one by ≈ π·cos(h)
        // (up to ~1°); azimuth barely moves at mid-latitudes.
        var jdUt = AstroTime.JulianDay(2024, 9, 18, 22);
        const double lat = 51.5074;
        const double lon = -0.1278;
        var geo = HorizontalCoordinates.OfMoon(jdUt, lat, lon, topocentric: false);
        var topo = HorizontalCoordinates.OfMoon(jdUt, lat, lon, topocentric: true);
        var deltaTDays = AstroTime.DeltaTSeconds(2024, 9) / 86400.0;
        var parallax = Moon.PositionAt(jdUt + deltaTDays).HorizontalParallaxDeg;
        var expectedDrop = parallax * Math.Cos(geo.AltitudeDeg * Math.PI / 180.0);
        Assert.True(topo.AltitudeDeg < geo.AltitudeDeg, "topocentric must be lower");
        Assert.Equal(expectedDrop, geo.AltitudeDeg - topo.AltitudeDeg, 0.05);
        Assert.True(Math.Abs(topo.AzimuthDeg - geo.AzimuthDeg) < 0.2);
    }

    [Fact]
    public void Refraction_StandardValues()
    {
        Assert.Equal(0.478, HorizontalCoordinates.RefractionDeg(0.0), 0.03); // horizon ≈ 28.7'
        Assert.Equal(0.017, HorizontalCoordinates.RefractionDeg(45.0), 0.005);
        Assert.InRange(HorizontalCoordinates.RefractionDeg(89.9), 0.0, 0.001); // ~0 at zenith
        Assert.True(HorizontalCoordinates.RefractionDeg(-30.0) >= 0.0);        // clamped, no blow-up
    }

    [Fact]
    public void Refraction_AppliedRaisesApparentSun()
    {
        var jd = AstroTime.JulianDay(2026, 6, 21, 4);
        var airless = HorizontalCoordinates.OfSun(jd, 51.5074, -0.1278);
        var apparent = HorizontalCoordinates.OfSun(jd, 51.5074, -0.1278, applyRefraction: true);
        Assert.True(apparent.AltitudeDeg > airless.AltitudeDeg);
        Assert.Equal(airless.AzimuthDeg, apparent.AzimuthDeg, 1e-9);
    }

    [Fact]
    public void OfEquatorial_AgreesWithSunConvenienceMethod()
    {
        // The general equatorial path must reproduce OfSun when fed the Sun's own
        // apparent RA/Dec at the same instant.
        var jdUt = AstroTime.JulianDay(2026, 6, 21, 12);
        const double lat = 51.5074;
        const double lon = -0.1278;
        var deltaTDays = AstroTime.DeltaTSeconds(2026, 6) / 86400.0;
        var pos = Sun.PositionAt(jdUt + deltaTDays);
        var viaEquatorial = HorizontalCoordinates.OfEquatorial(jdUt, pos.RightAscension, pos.Declination, lat, lon);
        var viaSun = HorizontalCoordinates.OfSun(jdUt, lat, lon);
        Assert.Equal(viaSun.AzimuthDeg, viaEquatorial.AzimuthDeg, 1e-9);
        Assert.Equal(viaSun.AltitudeDeg, viaEquatorial.AltitudeDeg, 1e-9);
    }
}

/// <summary>
/// Validates the galactic-centre direction against spherical-geometry invariants: a fixed
/// declination δ culminates at altitude 90° − |φ − δ|, independent of date.
/// </summary>
public class GalacticCenterValidationTests
{
    private static Horizontal Culmination(double latDeg, double lonDeg)
    {
        // Sample a full sidereal day at 2-minute steps and keep the highest point.
        var start = AstroTime.JulianDay(2026, 7, 5);
        var best = HorizontalCoordinates.OfGalacticCenter(start, latDeg, lonDeg);
        var t = start;
        while (t < start + 1.0)
        {
            var h = HorizontalCoordinates.OfGalacticCenter(t, latDeg, lonDeg);
            if (h.AltitudeDeg > best.AltitudeDeg) best = h;
            t += 2.0 / 1440.0;
        }

        return best;
    }

    /// <summary>From Łódź (51.77°N) the Milky Way core barely rises: culmination ≈ 9.2°, due south.</summary>
    [Fact]
    public void FromLodzTheCoreSkimsTheSouthernHorizon()
    {
        var top = Culmination(51.77, 19.46);
        Assert.True(Math.Abs(top.AltitudeDeg - (90.0 - 51.77 - 29.008)) < 0.3, $"culmination {top.AltitudeDeg}° ≠ ~9.2°");
        Assert.True(Math.Abs(top.AzimuthDeg - 180.0) < 2.0, $"culmination azimuth {top.AzimuthDeg}° not due south");
    }

    /// <summary>From the Atacama (23°S) the core passes nearly overhead: culmination ≈ 84°.</summary>
    [Fact]
    public void FromAtacamaTheCorePassesNearlyOverhead()
    {
        var top = Culmination(-23.0, -68.0);
        Assert.True(Math.Abs(top.AltitudeDeg - 84.0) < 0.5, $"culmination {top.AltitudeDeg}° ≠ ~84°");
    }
}
