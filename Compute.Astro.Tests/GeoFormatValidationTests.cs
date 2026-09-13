using System.Globalization;

namespace Compute.Astro.Tests;

/// <summary>Checks for geographic coordinate formatting and tolerant parsing.</summary>
public class GeoFormatValidationTests
{
    [Fact]
    public void FormatDms_LatLon()
    {
        Assert.Equal("N 47° 36' 22.32\"", GeoFormat.FormatDms(47.6062, GeoFormat.Axis.Latitude));
        Assert.Equal("W 122° 19' 55.56\"", GeoFormat.FormatDms(-122.3321, GeoFormat.Axis.Longitude));
    }

    [Fact]
    public void FormatDdm_LatLon()
    {
        Assert.Equal("N 47° 36.372'", GeoFormat.FormatDdm(47.6062, GeoFormat.Axis.Latitude));
        Assert.Equal("S 33° 52.128'", GeoFormat.FormatDdm(-33.8688, GeoFormat.Axis.Latitude));
    }

    [Fact]
    public void Parse_AcceptsManyForms()
    {
        Assert.Equal(47.6062, GeoFormat.Parse("47.6062"), 1e-9);
        Assert.Equal(-122.3321, GeoFormat.Parse("-122.3321"), 1e-9);
        Assert.Equal(47.6062, GeoFormat.Parse("47°36'22.32\"N"), 1e-6);
        Assert.Equal(-122.3321, GeoFormat.Parse("122°19'55.56\"W"), 1e-6);
        Assert.Equal(-33.8688, GeoFormat.Parse("S 33° 52.128'"), 1e-6);
        Assert.Equal(151.2093, GeoFormat.Parse("151 12 33.48 E"), 1e-6);
    }

    [Fact]
    public void FormatDms_CarriesWhenSecondsRoundToSixty()
    {
        // 46.9999999° is 46°59'59.99964" — at 2 decimals the seconds round to 60.00
        // and must carry all the way up to 47°0'0.00", never render as 59'60.00".
        Assert.Equal("N 47° 0' 0.00\"", GeoFormat.FormatDms(46.9999999, GeoFormat.Axis.Latitude));
        Assert.Equal("S 47° 0' 0.00\"", GeoFormat.FormatDms(-46.9999999, GeoFormat.Axis.Latitude));
        // Mid-degree carry: seconds roll the minutes, degrees stay.
        Assert.Equal("N 47° 1' 0.00\"", GeoFormat.FormatDms(47.0166666, GeoFormat.Axis.Latitude));
        Assert.Equal("W 180° 0' 0.00\"", GeoFormat.FormatDms(-179.99999999, GeoFormat.Axis.Longitude));
    }

    [Fact]
    public void FormatDdm_CarriesWhenMinutesRoundToSixty()
    {
        Assert.Equal("N 46° 0.000'", GeoFormat.FormatDdm(45.9999999, GeoFormat.Axis.Latitude));
        Assert.Equal("S 46° 0.000'", GeoFormat.FormatDdm(-45.9999999, GeoFormat.Axis.Latitude));
    }

    [Fact]
    public void DmsFormat_CarriesWhenSecondsRoundToSixty()
    {
        Assert.Equal("N 47° 0' 0.00\"", Dms.Of(46.9999999).Format('N', 'S'));
        Assert.Equal("S 47° 0' 0.00\"", Dms.Of(-46.9999999).Format('N', 'S'));
        Assert.Equal("N 47° 36' 22.32\"", Dms.Of(47.6062).Format('N', 'S'));
    }

    [Fact]
    public void Parse_RejectsGarbage()
    {
        Assert.Null(GeoFormat.ParseOrNull("north-ish"));
        Assert.Null(GeoFormat.ParseOrNull(""));
        Assert.Null(GeoFormat.ParseOrNull("34ede"));
        Assert.Null(GeoFormat.ParseOrNull("34fdf"));
        Assert.Null(GeoFormat.ParseOrNull("47°36'22.32?N"));
        Assert.Null(GeoFormat.ParseOrNull("12@34"));
        Assert.Null(GeoFormat.ParseOrNull("12 34 56 78"));
    }

    [Fact]
    public void Parse_ThrowsOnGarbage()
    {
        Assert.Throws<FormatException>(() => GeoFormat.Parse("north-ish"));
    }

    [Fact]
    public void Parse_NormalizesCommas()
    {
        Assert.Equal(47.6062, GeoFormat.Parse("47,6062"), 1e-9);
        Assert.Equal(-122.3321, GeoFormat.Parse("-122,3321"), 1e-9);
        Assert.Equal(47.6062, GeoFormat.Parse("47°36'22,32\"N"), 1e-6);
    }

    [Fact]
    public void Parse_AcceptsOrdinalIndicatorDegreeSigns()
    {
        // º (U+00BA) and ª are Unicode letters but common degree-sign stand-ins.
        Assert.Equal(47.6, GeoFormat.Parse("47º36'N"), 1e-9);
        Assert.Equal(-33.8688, GeoFormat.Parse("33.8688ºS"), 1e-9);
    }

    [Fact]
    public void RoundTrip_DmsParsesBackToOriginal()
    {
        foreach (var deg in new[] { 47.6062, -122.3321, 0.0, 89.999, -33.8688 })
        {
            var text = GeoFormat.FormatDms(deg, GeoFormat.Axis.Latitude, secondsDecimals: 4);
            Assert.True(Math.Abs(GeoFormat.Parse(text) - deg) < 1e-4, $"round-trip {deg} via \"{text}\"");
        }
    }

    /// <summary>
    /// Formatting must not depend on the ambient culture — a comma-decimal locale must
    /// still render "47.6062°", not "47,6062°". (No Kotlin equivalent: the KMP original
    /// has no CultureInfo to get wrong.)
    /// </summary>
    [Fact]
    public void Format_IsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("N 47° 36' 22.32\"", GeoFormat.FormatDms(47.6062, GeoFormat.Axis.Latitude));
            Assert.Equal("N 47° 36.372'", GeoFormat.FormatDdm(47.6062, GeoFormat.Axis.Latitude));
            Assert.Equal("47.606200°", GeoFormat.FormatDecimal(47.6062));
            Assert.Equal(47.6062, GeoFormat.Parse("47.6062"), 1e-9);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Ddm_DecomposesSignedAngles()
    {
        var d = Ddm.Of(-33.8688);
        Assert.True(d.Negative);
        Assert.Equal(33, d.Degrees);
        Assert.Equal(52.128, d.Minutes, 1e-6);
    }
}
