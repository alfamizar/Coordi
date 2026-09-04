using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Utils;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// A location resolves its zone from its coordinates and caches the answer. These cover the
    /// rule that decides when that cache is thrown away: moving the pin re-resolves a looked-up
    /// zone, but never an offset the user picked by hand.
    /// </summary>
    public class LocationTimeZoneInvalidationTests
    {
        private const double MountainViewLat = 37.4220;
        private const double MountainViewLon = -122.0840;

        [Fact]
        public void ZoneReadBeforeCoordinatesArrive_IsReResolvedOnceTheyDo()
        {
            var location = new Location();

            // A view binding to TimeZoneOffset while the object is still blank does exactly this,
            // and (0, 0) resolves to UTC. Caching that permanently made every such location
            // report UTC no matter where it was afterwards placed.
            _ = location.TimeZoneId;

            location.Latitude = MountainViewLat;
            location.Longitude = MountainViewLon;

            Assert.Equal("America/Los_Angeles", location.TimeZoneId);
        }

        [Fact]
        public void MovingALocation_ReResolvesItsLookedUpZone()
        {
            var location = new Location { Latitude = 51.5074, Longitude = -0.1278 };
            Assert.Equal("Europe/London", location.TimeZoneId);

            location.Latitude = 35.6762;
            location.Longitude = 139.6503;

            Assert.Equal("Asia/Tokyo", location.TimeZoneId);
        }

        [Fact]
        public void AHandPickedOffset_SurvivesTheCoordinatesChanging()
        {
            var location = new Location { Latitude = 51.5074, Longitude = -0.1278 };
            location.TimeZoneId = TimeZoneUtils.ToFixedOffsetId(TimeSpan.FromHours(5.5));

            location.Latitude = MountainViewLat;
            location.Longitude = MountainViewLon;

            Assert.Equal("UTC+05:30", location.TimeZoneId);
            Assert.Equal(TimeSpan.FromHours(5.5), location.GetUtcOffset(DateTime.UtcNow));
        }

        [Fact]
        public void APinnedZeroOffset_IsNotMistakenForALookup()
        {
            var location = new Location { Latitude = 51.5074, Longitude = -0.1278 };
            location.TimeZoneId = TimeZoneUtils.ToFixedOffsetId(TimeSpan.Zero);

            location.Latitude = MountainViewLat;
            location.Longitude = MountainViewLon;

            Assert.Equal("UTC+00:00", location.TimeZoneId);
            Assert.Equal(TimeSpan.Zero, location.GetUtcOffset(DateTime.UtcNow));
        }

        [Theory]
        [InlineData("UTC+00:00", true)]
        [InlineData("UTC-07:00", true)]
        [InlineData("UTC+05:30", true)]
        [InlineData("UTC", false)]          // a lookup result, not a choice
        [InlineData("Europe/London", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsPinnedFixedOffset_AcceptsOnlySignedIds(string? id, bool expected) =>
            Assert.Equal(expected, TimeZoneUtils.IsPinnedFixedOffset(id));

        [Fact]
        public void ADisplayedOffset_FollowsTheLocation()
        {
            var location = new Location();
            _ = location.TimeZoneOffset;   // the binding that poisoned the cache

            location.Latitude = MountainViewLat;
            location.Longitude = MountainViewLon;

            TimeZoneOffset shown = location.TimeZoneOffset;
            Assert.NotEqual(TimeSpan.Zero, shown.Offset);
        }
    }
}
