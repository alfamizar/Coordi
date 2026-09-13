using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Domain.Services.Moon;
using Compute.Core.Utils;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// A location's UTC offset is a function of the date, not just the place. These cover the
    /// three ways that used to be wrong: daylight saving ignored, zones that are not whole hours
    /// rounded away, and zone borders assumed to follow meridians.
    /// </summary>
    public class TimeZoneResolutionTests
    {
        private static Location At(double latitude, double longitude) =>
            new() { Latitude = latitude, Longitude = longitude };

        [Fact]
        public void TimeZoneId_IsResolvedFromCoordinates()
        {
            Assert.Equal("Europe/London", At(51.5074, -0.1278).TimeZoneId);
            Assert.Equal("Asia/Tokyo", At(35.6839, 139.7744).TimeZoneId);
        }

        /// <summary>
        /// London is UTC+0 in winter and UTC+1 in summer. The old longitude approximation gave
        /// UTC+0 year round, so every summer sunrise was shown an hour early.
        /// </summary>
        [Fact]
        public void GetUtcOffset_FollowsDaylightSaving()
        {
            var london = At(51.5074, -0.1278);

            Assert.Equal(TimeSpan.Zero, london.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(TimeSpan.FromHours(1), london.GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc)));
        }

        /// <summary>The southern hemisphere runs the other way round, which a sign error would miss.</summary>
        [Fact]
        public void GetUtcOffset_FollowsSouthernHemisphereDaylightSaving()
        {
            var sydney = At(-33.8688, 151.2093);

            Assert.Equal(TimeSpan.FromHours(11), sydney.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(TimeSpan.FromHours(10), sydney.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)));
        }

        /// <summary>
        /// Not every zone is a whole hour off, which is why the offset is carried as a TimeSpan.
        /// Rounding these to hours put Kathmandu 45 minutes wrong.
        /// </summary>
        [Theory]
        [InlineData(28.3949, 84.1240, 5, 45)]   // Nepal, UTC+5:45
        [InlineData(28.6139, 77.2090, 5, 30)]   // India, UTC+5:30
        public void GetUtcOffset_HandlesSubHourZones(double lat, double lon, int hours, int minutes)
        {
            var offset = At(lat, lon).GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(new TimeSpan(hours, minutes, 0), offset);
        }

        /// <summary>
        /// China spans five geographic hours on one zone, and Spain sits west of Greenwich on
        /// central European time. Longitude alone gets both badly wrong.
        /// </summary>
        [Theory]
        [InlineData(43.8256, 87.6168, 8)]   // Ürümqi: longitude says +6
        [InlineData(40.4168, -3.7038, 2)]   // Madrid in August: longitude says 0
        public void GetUtcOffset_IgnoresLongitudeWhereZoneBordersDoNot(double lat, double lon, int expectedHours)
        {
            var offset = At(lat, lon).GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc));

            Assert.Equal(TimeSpan.FromHours(expectedHours), offset);
        }

        /// <summary>Picking an offset by hand pins the location and overrides the lookup.</summary>
        [Fact]
        public void AssigningTimeZoneOffset_PinsAFixedOffset()
        {
            var london = At(51.5074, -0.1278)!;

            london.TimeZoneOffset = TimeZoneOffset.FromOffset(TimeSpan.FromHours(-7));

            Assert.Equal("UTC-07:00", london.TimeZoneId);
            Assert.Equal(TimeSpan.FromHours(-7), london.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
            // Still fixed in summer: a hand-picked offset does not observe daylight saving.
            Assert.Equal(TimeSpan.FromHours(-7), london.GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc)));
        }

        /// <summary>
        /// The offset picker binds two-way, so it writes its current value back the moment the
        /// editor opens. That must not convert a resolved zone into a fixed offset — otherwise
        /// simply opening a location for editing would cost it its daylight saving.
        /// </summary>
        [Fact]
        public void AssigningTheOffsetItAlreadyHas_DoesNotPin()
        {
            var oslo = At(59.9111, 10.7528);
            Assert.Equal("Europe/Oslo", oslo.TimeZoneId);

            // What the picker echoes back on load: the offset in force right now.
            oslo.TimeZoneOffset = TimeZoneOffset.FromOffset(oslo.GetUtcOffset(DateTime.UtcNow));

            Assert.Equal("Europe/Oslo", oslo.TimeZoneId);
            // Still seasonal, which a pinned UTC+02:00 would not be.
            Assert.Equal(TimeSpan.FromHours(2), oslo.GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(TimeSpan.FromHours(1), oslo.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)));
        }

        /// <summary>Moving a pinned location keeps the user's offset; moving a looked-up one re-resolves.</summary>
        [Fact]
        public void MovingTheCoordinates_ReResolvesOnlyLookedUpZones()
        {
            var looked = At(51.5074, -0.1278);
            Assert.Equal("Europe/London", looked.TimeZoneId);
            looked.Latitude = 35.6839;
            looked.Longitude = 139.7744;
            Assert.Equal("Asia/Tokyo", looked.TimeZoneId);

            var pinned = At(51.5074, -0.1278);
            pinned.TimeZoneOffset = TimeZoneOffset.FromOffset(TimeSpan.FromHours(3));
            pinned.Latitude = 35.6839;
            pinned.Longitude = 139.7744;
            Assert.Equal("UTC+03:00", pinned.TimeZoneId);
        }

        [Fact]
        public void Placeholder_ObservesBritishSummerTime()
        {
            var placeholder = Location.CreatePlaceholder();

            Assert.Equal("Europe/London", placeholder.TimeZoneId);
            Assert.Equal(TimeSpan.FromHours(1), placeholder.GetUtcOffset(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Theory]
        [InlineData(0, 0, "UTC±0 (UTC)")]
        [InlineData(1, 0, "UTC+1")]
        [InlineData(-7, 0, "UTC-7")]
        [InlineData(5, 30, "UTC+5:30")]
        [InlineData(-3, -30, "UTC-3:30")]
        public void FormatOffset_WritesOffsetsTheWayTheUiShowsThem(int hours, int minutes, string expected)
        {
            Assert.Equal(expected, TimeZoneUtils.FormatOffset(new TimeSpan(hours, minutes, 0)));
        }

        [Theory]
        [InlineData(5, 30)]
        [InlineData(-7, 0)]
        [InlineData(0, 0)]
        public void FixedOffsetIds_RoundTrip(int hours, int minutes)
        {
            var offset = new TimeSpan(hours, minutes, 0);

            Assert.True(TimeZoneUtils.TryParseFixedOffset(TimeZoneUtils.ToFixedOffsetId(offset), out var parsed));
            Assert.Equal(offset, parsed);
        }

        [Fact]
        public void TryParseFixedOffset_RejectsIanaIds()
        {
            Assert.False(TimeZoneUtils.TryParseFixedOffset("Europe/London", out _));
            Assert.False(TimeZoneUtils.TryParseFixedOffset(null, out _));
        }

        /// <summary>
        /// The reason this mattered on screen: eclipse contact times were UTC while every other
        /// time in the app was local. A summer eclipse over London must now read BST.
        /// </summary>
        [Fact]
        public async Task EclipseContactTimes_AreInTheLocationsOwnZone()
        {
            var london = At(51.5074, -0.1278);

            var utcTable = await new MoonService()
                .GetMoonEclipsesAsync(
                    new Location { Latitude = 51.5074, Longitude = -0.1278, TimeZoneId = "UTC+00:00" },
                    new DateTime(2026, 1, 1));
            var localTable = await new MoonService().GetMoonEclipsesAsync(london, new DateTime(2026, 1, 1));

            // 2019-07-16 was a partial lunar eclipse; Britain was on BST, an hour ahead of UT.
            var inUtc = utcTable.Single(e => e.Date == new DateTime(2019, 7, 16));
            var inLondon = localTable.Single(e => e.Date == new DateTime(2019, 7, 16));

            Assert.Equal(inUtc.MidEclipse.AddHours(1), inLondon.MidEclipse);

            // A January eclipse is on GMT, so it must be unchanged — proving the offset is read
            // per eclipse rather than applied as one blanket shift.
            var winterUtc = utcTable.Single(e => e.Date == new DateTime(2019, 1, 21));
            var winterLondon = localTable.Single(e => e.Date == new DateTime(2019, 1, 21));

            Assert.Equal(winterUtc.MidEclipse, winterLondon.MidEclipse);
        }
    }
}
