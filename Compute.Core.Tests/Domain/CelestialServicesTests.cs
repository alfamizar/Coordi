using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.AstroSign;
using MoonName = Compute.Core.Domain.Entities.Models.Moon.MoonName;
using MoonPhase = Compute.Core.Domain.Entities.Models.Moon.MoonPhase;
using Compute.Core.Domain.Services.Moon;
using Compute.Core.Domain.Services.Sun;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// Covers the services that were rebuilt on Compute.Astro when CoordinateSharp was
    /// dropped: the shapes the views bind to, the local-time conversion, and the two
    /// "absent event" conventions the XAML converters depend on.
    /// </summary>
    public class CelestialServicesTests
    {
        private const double LondonLat = 51.5074;
        private const double LondonLon = -0.1278;

        /// <summary>
        /// A location pinned to UTC, so the assertions below can stay in the published UT that
        /// NASA quotes. Real locations resolve their own zone; that is covered separately in
        /// <see cref="TimeZoneResolutionTests"/>.
        /// </summary>
        private static Location AtUtc(double latitude, double longitude) => new()
        {
            Latitude = latitude,
            Longitude = longitude,
            TimeZoneId = "UTC+00:00",
        };


        /// <summary>
        /// NASA's published local circumstances for Dallas, 2024-04-08 (UT): C1 17:23, C2 18:41,
        /// max 18:43, C3 18:45, C4 20:03, magnitude just over 1.
        /// </summary>
        [Fact]
        public async Task GetSunEclipsesAsync_MatchesPublishedLocalCircumstances()
        {
            var table = await new SunService().GetSunEclipsesAsync(AtUtc(32.78, -96.80), new DateTime(2026, 1, 1));

            // The table spans the whole century, as it always has.
            Assert.InRange(table.Count, 200, 250);
            Assert.All(table, e => Assert.InRange(e.Date.Year, 2000, 2099));

            var totality = table.Single(e => e.Date == new DateTime(2024, 4, 8));
            Assert.Equal(SolarEclipseType.Total, totality.Type);
            Assert.Equal(SolarEclipseLocalType.Total, totality.LocalType);
            Assert.True(totality.IsVisible);
            Assert.Equal(17, totality.PartialEclipseBegin.Hour);
            Assert.Equal(23, totality.PartialEclipseBegin.Minute);
            Assert.Equal(18, totality.MaximumEclipse.Hour);
            Assert.Equal(20, totality.PartialEclipseEnd.Hour);
            Assert.InRange(totality.CentralDuration.TotalSeconds, 200, 260); // ≈ 3m49s
            Assert.True(totality.Magnitude > 1.0);
        }

        /// <summary>
        /// An eclipse that misses this location must leave its contact times at
        /// <see langword="default"/> — that is exactly what <c>IsDateTimeSetConverter</c>
        /// tests to hide the row's labels.
        /// </summary>
        [Fact]
        public async Task GetSunEclipsesAsync_LeavesInvisibleEclipsesAtDefaultSoRowsStayHidden()
        {
            var table = await new SunService().GetSunEclipsesAsync(AtUtc(32.78, -96.80), new DateTime(2026, 1, 1));

            // The 2024-10-02 annular was over the South Pacific — nothing to see from Dallas.
            var annular = table.Single(e => e.Date == new DateTime(2024, 10, 2));
            Assert.Equal(SolarEclipseLocalType.None, annular.LocalType);
            Assert.False(annular.IsVisible);
            Assert.Equal(default, annular.PartialEclipseBegin);
            Assert.Equal(default, annular.MaximumEclipse);
            Assert.Equal(TimeSpan.Zero, annular.CentralDuration);
        }

        /// <summary>NASA contact times (UT) for the 2019-01-21 total lunar eclipse.</summary>
        [Fact]
        public async Task GetMoonEclipsesAsync_MatchesPublishedContactTimes()
        {
            var table = await new MoonService().GetMoonEclipsesAsync(AtUtc(LondonLat, LondonLon), new DateTime(2026, 1, 1));

            Assert.InRange(table.Count, 200, 250);

            var total = table.Single(e => e.Date == new DateTime(2019, 1, 21));
            Assert.Equal(LunarEclipseType.Total, total.Type);
            Assert.Equal(new TimeSpan(2, 37, 0).TotalMinutes, total.PenumbralEclipseBegin.TimeOfDay.TotalMinutes, 5.0);
            Assert.Equal(new TimeSpan(4, 41, 0).TotalMinutes, total.TotalEclipseBegin.TimeOfDay.TotalMinutes, 5.0);
            Assert.Equal(new TimeSpan(5, 12, 0).TotalMinutes, total.MidEclipse.TimeOfDay.TotalMinutes, 5.0);
            Assert.Equal(new TimeSpan(7, 48, 0).TotalMinutes, total.PenumbralEclipseEnd.TimeOfDay.TotalMinutes, 5.0);
            Assert.InRange(total.UmbralMagnitude, 1.18, 1.20);
        }

        [Fact]
        public async Task GetMoonEclipsesAsync_PenumbralEclipseHasNoUmbralContacts()
        {
            var table = await new MoonService().GetMoonEclipsesAsync(AtUtc(LondonLat, LondonLon), new DateTime(2026, 1, 1));

            var penumbral = table.First(e => e.Type == LunarEclipseType.Penumbral);
            Assert.Equal(default, penumbral.PartialEclipseBegin);
            Assert.Equal(default, penumbral.TotalEclipseBegin);
            Assert.NotEqual(default, penumbral.PenumbralEclipseBegin);
        }

        [Fact]
        public void CelestialSnapshot_CarriesGridReferencesAndCelestialData()
        {
            // Paris on a CEST (UTC+2) date; the Eiffel Tower is the canonical 31U DQ square.
            var snapshot = CelestialSnapshot.For(48.8582, 2.2945, new DateTime(2024, 9, 18), 2.0);

            Assert.Equal(31, snapshot.Mgrs!.LongZone);
            Assert.Equal("U", snapshot.Mgrs.LatZone);
            Assert.Equal("DQ", snapshot.Mgrs.Digraph);
            Assert.Equal("MGRS", snapshot.Mgrs.SystemType);

            Assert.Equal(31, snapshot.Utm!.LongZone);
            Assert.Equal(298.257223563, snapshot.Utm.InverseFlattening);
            Assert.Equal("UTM", snapshot.Utm.SystemType);
            Assert.InRange(snapshot.Utm.Easting, 448_000, 448_500);

            Assert.Equal(MoonPhase.FullMoon, snapshot.MoonPhase);
            Assert.Equal(AstroZodiacSign.Virgo, snapshot.ZodiacSign);
            Assert.NotNull(snapshot.SunRise);
            Assert.NotNull(snapshot.SunSet);
        }

        /// <summary>UTM and MGRS are undefined in the polar regions; the snapshot must not throw.</summary>
        [Fact]
        public void CelestialSnapshot_OmitsGridReferencesInThePolarRegions()
        {
            var snapshot = CelestialSnapshot.For(88.0, 0.0, new DateTime(2024, 6, 21), 0.0);

            Assert.Null(snapshot.Utm);
            Assert.Null(snapshot.Mgrs);
            Assert.Null(snapshot.SunRise); // midnight sun
        }
    }
}
