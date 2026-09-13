# Compute.Astro

Dependency-free astronomy and geodesy for .NET. A C# port of the clean-room Kotlin
Multiplatform `:astro` library from [AstroClaw](../../../../../../kmp-cmp/AstroClaw), written
from public algorithms — J. Meeus, *Astronomical Algorithms* (2nd ed.), Vincenty (1975) for
geodesics, Snyder/USGS PP 1395 for the transverse Mercator projection, and the JPL
"Approximate Positions of the Planets" elements.

No third-party runtime dependencies. Targets `netstandard2.0` and `net8.0`, so it runs on
.NET Framework, .NET Core/5+, MAUI, Xamarin, Unity and Mono.

This is the licence-clean replacement for **CoordinateSharp** (AGPL).

## What's in it

| Area | Type | Notes |
| --- | --- | --- |
| Time scales | `AstroTime` | Julian Day ⇄ proleptic Gregorian UTC (and `DateTime`), sidereal time, full Espenak–Meeus ΔT |
| Sun | `Sun`, `SunRiseSet` | Apparent position, equation of time, radius vector; rise/set/transit and the three twilights |
| Moon | `Moon`, `MoonRiseSet`, `MoonPhase`, `MoonPhaseNaming` | Truncated ELP-2000/82 position, distance, parallax, illumination; rise/set; the four principal phases and the eight display phases |
| Eclipses | `Eclipse`, `SolarEclipseLocalCircumstances` | Geocentric solar and lunar catalogue, lunar contact times, **full local solar circumstances** via Besselian elements, and umbral ground tracks |
| Sky position | `HorizontalCoordinates`, `SkySearch` | Alt-az for Sun, Moon, arbitrary RA/Dec and the galactic centre; topocentric parallax, refraction; "when is the body at this azimuth?" search |
| Stars & constellations | `Constellations`, `BrightStars`, `ConstellationLines` | IAU boundaries (B1875), 904 stars to V 4.5, 150 stick-figure polylines |
| Planets | `Planets` | Heliocentric and geocentric positions, orbital periods, IAU pole and spin data |
| Zodiac & almanac | `Zodiac`, `Almanac` | Tropical signs for Sun and Moon; traditional full-moon names including blue moons |
| Geodesy | `Geodesy` | Vincenty inverse: ellipsoidal distance and initial/final bearings on WGS84 |
| Grids & formatting | `GridReference`, `GeoFormat`, `Dms`, `Ddm` | UTM and MGRS; DMS/DDM/decimal formatting and tolerant parsing |

## Conventions

- **Angles** are degrees throughout.
- **Longitude is positive east.**
- **Time** inputs are Julian Days in **UTC** unless a member is explicitly documented as
  Dynamical Time (`Jde*` members). Convert with `AstroTime.DeltaTSeconds`.
- **Azimuth** is measured from true north, clockwise (0° = N, 90° = E) — the civil convention.
- Formatting and parsing are **culture-invariant**: a comma-decimal locale still renders
  `47.606200°`.

## Quick start

```csharp
using Compute.Astro;

var jd = AstroTime.JulianDay(DateTime.UtcNow);

// Sunrise and sunset in London today (minutes from 00:00 UTC).
var sun = SunRiseSet.Events(2024, 6, 21, latitudeDeg: 51.5074, longitudeEastDeg: -0.1278);
Console.WriteLine($"{sun.Type}: rise {sun.SunriseUtcMinutes}, set {sun.SunsetUtcMinutes}");

// Where is the Moon right now, as an observer actually sees it?
var moon = HorizontalCoordinates.OfMoon(jd, 51.5074, -0.1278, applyRefraction: true);
Console.WriteLine($"az {moon.AzimuthDeg:F1}° alt {moon.AltitudeDeg:F1}°");
Console.WriteLine($"{MoonPhaseNaming.At(jd)}, {Moon.IlluminatedFraction(jd):P0} lit");

// Local circumstances of a solar eclipse — contact times for one site.
var e = Eclipse.SolarLocalCircumstances(lunation: 300, latDeg: 32.78, longitudeEastDeg: -96.80);
if (e is { Visible: true })
{
    Console.WriteLine($"{e.LocalType}, magnitude {e.MagnitudeAtMax:F3}");
    Console.WriteLine($"totality {e.CentralDurationSeconds:F0} s");
}

// Distance and bearing on the ellipsoid.
var g = Geodesy.Inverse(51.5074, -0.1278, 40.7128, -74.0060);
Console.WriteLine($"{g.DistanceMeters / 1000:F1} km, bearing {g.InitialBearingDeg:F1}°");

// Grid references and coordinate formatting.
Console.WriteLine(GridReference.ToMgrs(48.8582, 2.2945).Format()); // 31U DQ 48xxx 11xxx
Console.WriteLine(GeoFormat.FormatDms(47.6062, GeoFormat.Axis.Latitude)); // N 47° 36' 22.32"
```

## Accuracy

The series are truncated, and the tolerances below are what the test suite asserts against
independent references — NASA/JPL Horizons (DE441), Meeus worked examples, and published
NASA eclipse circumstances.

| Quantity | Agreement |
| --- | --- |
| Sun apparent position | < 0.015° vs. Horizons |
| Moon apparent position | < 0.010° vs. Horizons |
| Moon distance | < 60 km vs. Horizons |
| Topocentric alt-az (Sun / Moon) | < 0.015° / < 0.012° vs. Horizons |
| Sunrise / sunset | ~1 min |
| Eclipse contact times | ~1–2 min (ephemeris-limited) |
| Vincenty distance | ~0.5 mm |
| UTM/MGRS round-trip | sub-millimetre across 80°S–84°N |

ΔT is the full piecewise Espenak–Meeus model: ≲1 s across the telescopic era, growing for
ancient dates and extrapolated beyond ~2015.

### Known limits

- Polar UPS grid regions (MGRS bands A/B/Y/Z) are not handled; `GridReference` covers the
  standard 80°S–84°N band.
- Vincenty can fail to converge for near-antipodal pairs; the best iterate is returned.
- `SkySearch` intentionally ignores near-zenith passes, where azimuth sweeps too fast for the
  coarse scan.
- Planetary elements are the JPL 1800–2050 set; accuracy degrades outside that window.

## Tests

The suite is a port of the Kotlin library's validation tests, plus .NET-specific coverage
(culture invariance, `DateTime` interop, exception contracts).

```bash
dotnet test Compute.Astro.Tests/Compute.Astro.Tests.csproj
```
