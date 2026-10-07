namespace Compute.Astro.Tests;

/// <summary>
/// The azimuth was rewritten to remove a tan δ that runs away at the celestial poles, and the
/// claim made for the rewrite was that it is the <i>same angle</i> — numerator and denominator
/// both multiplied by cos δ, which atan2 cannot notice.
///
/// A claim like that is worth a test rather than an assurance, because everything that points at
/// anything goes through this function: rise and set times, the sky chart, the eclipse reduction.
/// If the two forms disagreed anywhere, they would disagree quietly and in a direction.
/// </summary>
public class AzimuthConditioningTests
{
    /// <summary>Meeus 13.5 as written, with the tan — the form this replaced.</summary>
    private static double TextbookAzimuth(double hourAngleDeg, double declinationDeg, double latitudeDeg)
    {
        var h = ToRadians(hourAngleDeg);
        var dec = ToRadians(declinationDeg);
        var phi = ToRadians(latitudeDeg);
        return NormalizeDegrees(ToDegrees(Math.Atan2(Math.Sin(h), Math.Cos(h) * Math.Sin(phi) - Math.Tan(dec) * Math.Cos(phi))) + 180.0);
    }

    /// <summary>Everywhere the old form is well conditioned the two must agree to the last useful digit.</summary>
    [Fact]
    public void AgreesWithTheTextbookFormAcrossTheSky()
    {
        var worst = 0.0;
        foreach (var lat in new[] { -89.0, -55.0, -23.4, 0.0, 23.4, 51.5, 78.0, 89.0 })
        {
            foreach (var dec in new[] { -89.5, -70.0, -23.4, 0.0, 23.4, 45.0, 70.0, 89.5 })
            {
                for (var ha = -179.0; ha <= 180.0; ha += 7.0)
                {
                    var ours = HorizontalCoordinates.FromHourAngle(ha, dec, lat).AzimuthDeg;
                    var theirs = TextbookAzimuth(ha, dec, lat);
                    // Round the long way: 359.9999 and 0.0001 are the same direction.
                    var d = Math.Abs(ours - theirs);
                    if (d > 180.0) d = 360.0 - d;
                    if (d > worst) worst = d;
                }
            }
        }

        Assert.True(worst < 1e-9, $"the two forms diverge by {worst} degrees");
    }

    /// <summary>
    /// And at the pole itself the answer is the one that needs no formula: a body at δ = +90
    /// stands due north at an altitude equal to the observer's latitude, whatever the hour angle
    /// does. This is the case the tan form reached through a number of order 10^16.
    /// </summary>
    [Fact]
    public void TheCelestialPolesReadDueNorthAndDueSouth()
    {
        foreach (var lat in new[] { -70.0, -35.0, 0.0, 35.0, 51.5, 70.0 })
        {
            foreach (var ha in new[] { -150.0, -37.0, 0.0, 22.0, 91.0, 179.0 })
            {
                var north = HorizontalCoordinates.FromHourAngle(ha, 90.0, lat);
                Assert.True(
                    Math.Abs(north.AltitudeDeg - lat) < 1e-9,
                    $"north pole altitude should equal the latitude: {north.AltitudeDeg} at {lat}");
                var offNorth = Math.Min(Math.Abs(north.AzimuthDeg), Math.Abs(north.AzimuthDeg - 360.0));
                Assert.True(offNorth < 1e-9, $"north pole should bear 0 degrees but read {north.AzimuthDeg}");

                var south = HorizontalCoordinates.FromHourAngle(ha, -90.0, lat);
                Assert.True(
                    Math.Abs(south.AltitudeDeg + lat) < 1e-9,
                    $"south pole altitude should be minus the latitude: {south.AltitudeDeg} at {lat}");
                Assert.True(
                    Math.Abs(south.AzimuthDeg - 180.0) < 1e-9,
                    $"south pole should bear 180 degrees but read {south.AzimuthDeg}");
            }
        }
    }

    // The library's own helpers are internal; these are the same arithmetic.
    private static double ToRadians(double deg) => deg * (Math.PI / 180.0);

    private static double ToDegrees(double rad) => rad * (180.0 / Math.PI);

    private static double NormalizeDegrees(double deg)
    {
        var r = deg % 360.0;
        return r < 0 ? r + 360.0 : r;
    }
}
