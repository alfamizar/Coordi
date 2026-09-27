using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Domain
{
    public class LocationIdentityTests
    {
        private static Location Saved(int id, string name = "Paris") =>
            new() { Id = id, Name = name, Latitude = 48.8566, Longitude = 2.3522 };

        [Fact]
        public void TwoSavedRows_AreTheSamePlaceOnlyIfTheSameRow()
        {
            Assert.True(LocationIdentity.AreSame(Saved(7), Saved(7)));
            Assert.False(LocationIdentity.AreSame(Saved(7), Saved(8)));
        }

        [Fact]
        public void SavedRows_AreComparedByIdEvenWhenTheCoordinatesMatch()
        {
            // Two saved rows can legitimately sit at the same spot under different names.
            var a = Saved(7, "Home");
            var b = Saved(8, "Home");

            Assert.False(LocationIdentity.AreSame(a, b));
        }

        [Fact]
        public void ThereIsOnlyOneCurrentPlace_HoweverFarItHasMoved()
        {
            var before = new Location { IsCurrent = true, Latitude = 48.85, Longitude = 2.35 };
            var after = new Location { IsCurrent = true, Latitude = 35.68, Longitude = 139.65 };

            Assert.True(LocationIdentity.AreSame(before, after));
        }

        [Fact]
        public void UnsavedPlaces_MatchOnCoordinatesAndName()
        {
            var a = new Location { Name = "Kyoto", Latitude = 35.0117, Longitude = 135.7683 };
            var b = new Location { Name = "Kyoto", Latitude = 35.0117, Longitude = 135.7683 };
            var elsewhere = new Location { Name = "Kyoto", Latitude = 35.9, Longitude = 135.7683 };
            var renamed = new Location { Name = "Kyōto", Latitude = 35.0117, Longitude = 135.7683 };

            Assert.True(LocationIdentity.AreSame(a, b));
            Assert.False(LocationIdentity.AreSame(a, elsewhere));
            Assert.False(LocationIdentity.AreSame(a, renamed));
        }

        [Fact]
        public void TheDeviceSlot_IsCurrentAndUnsaved()
        {
            Assert.True(LocationIdentity.IsDeviceSlot(new Location { IsCurrent = true, Id = 0 }));
            Assert.False(LocationIdentity.IsDeviceSlot(new Location { IsCurrent = true, Id = 4 }));
            Assert.False(LocationIdentity.IsDeviceSlot(new Location { IsCurrent = false, Id = 0 }));
        }
    }
}
