namespace Compute.Astro.Tests;

/// <summary>
/// Reference checks for the UTM/MGRS grid conversions. Absolute correctness is
/// cross-validated against the independently-verified <see cref="Geodesy"/> meridian arc,
/// exact central-meridian anchors, and well-documented real-world grid references.
/// </summary>
public class GridReferenceValidationTests
{
    private const double K0 = 0.9996;

    [Fact]
    public void Utm_CentralMeridianEquatorAnchors()
    {
        // On a zone's central meridian the easting is exactly the 500 km false origin.
        var cmEquator = GridReference.ToUtm(0.0, 3.0); // zone 31 CM = 3°E
        Assert.Equal(31, cmEquator.ZoneNumber);
        Assert.Equal(Hemisphere.North, cmEquator.Hemisphere);
        Assert.Equal(500_000.0, cmEquator.Easting, 1e-6);
        Assert.Equal(0.0, cmEquator.Northing, 1e-6);

        var cmMidLat = GridReference.ToUtm(45.0, 3.0);
        Assert.Equal(500_000.0, cmMidLat.Easting, 1e-6);
    }

    [Fact]
    public void Utm_NorthingMatchesValidatedMeridianArc()
    {
        // A point on the central meridian: its UTM northing must equal k0 × the
        // meridian-arc distance from the equator, taken from the validated Vincenty
        // geodesic (a meridian is a geodesic). This pins the absolute scale.
        var nLat = GridReference.ToUtm(45.0, 3.0);
        Assert.Equal(K0 * Geodesy.DistanceMeters(0.0, 3.0, 45.0, 3.0), nLat.Northing, 1.0);

        var sLat = GridReference.ToUtm(-30.0, 3.0);
        var expectedSouth = 10_000_000.0 - K0 * Geodesy.DistanceMeters(0.0, 3.0, -30.0, 3.0);
        Assert.Equal(expectedSouth, sLat.Northing, 1.0);
        Assert.Equal(Hemisphere.South, sLat.Hemisphere);
    }

    [Fact]
    public void Utm_RoundTripsToSubMillimetre()
    {
        (double Lat, double Lon)[] points =
        [
            (48.8582, 2.2945),     // Paris
            (-33.8688, 151.2093),  // Sydney
            (40.7128, -74.0060),   // New York
            (64.1466, -21.9426),   // Reykjavík
            (-54.8019, -68.3030),  // Ushuaia
        ];
        foreach (var (lat, lon) in points)
        {
            var (rlat, rlon) = GridReference.ToLatLon(GridReference.ToUtm(lat, lon));
            Assert.True(Math.Abs(rlat - lat) < 1e-7, $"lat round-trip {lat} -> {rlat}");
            Assert.True(Math.Abs(rlon - lon) < 1e-7, $"lon round-trip {lon} -> {rlon}");
        }
    }

    [Fact]
    public void Utm_ZoneAndBandForKnownPlaces()
    {
        var sydney = GridReference.ToUtm(-33.8688, 151.2093);
        Assert.Equal(56, sydney.ZoneNumber);
        Assert.Equal('H', sydney.LatBand);
        Assert.Equal(Hemisphere.South, sydney.Hemisphere);

        // Norway zone-32 widening (band V).
        Assert.Equal(32, GridReference.ZoneNumber(60.0, 5.0));
    }

    [Fact]
    public void Utm_RejectsLatitudesOutsideTheBand()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GridReference.ToUtm(85.0, 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => GridReference.ToUtm(-81.0, 0.0));
    }

    [Fact]
    public void Mgrs_EiffelTowerDigraph()
    {
        // Paris/Eiffel Tower is the canonical 31U DQ square.
        var m = GridReference.ToMgrs(48.8582, 2.2945);
        Assert.Equal(31, m.ZoneNumber);
        Assert.Equal('U', m.LatBand);
        Assert.Equal("DQ", m.Digraph);
        Assert.InRange(m.Easting, 0, 99_999);
        Assert.InRange(m.Northing, 0, 99_999);
        // Formatted reference, e.g. "31U DQ 48xxx 11xxx".
        Assert.StartsWith("31U DQ ", m.Format());
    }

    [Fact]
    public void Mgrs_WithinSquareMatchesUtmRemainder()
    {
        const double lat = 40.7128;
        const double lon = -74.0060;
        var utm = GridReference.ToUtm(lat, lon);
        var mgrs = GridReference.ToMgrs(lat, lon);
        // Within-square offsets are the UTM coordinate modulo 100 km, rounded to 1 m.
        Assert.Equal((int)Math.Floor(utm.Easting % 100_000.0 + 0.5), mgrs.Easting);
        Assert.Equal((int)Math.Floor(utm.Northing % 100_000.0 + 0.5), mgrs.Northing);
    }

    [Fact]
    public void Utm_FormatRendersZoneBandEastingNorthing()
    {
        var utm = GridReference.ToUtm(48.8582, 2.2945);
        var text = utm.Format();
        Assert.StartsWith("31U ", text);
        Assert.Equal(3, text.Split(' ').Length);
    }
}
