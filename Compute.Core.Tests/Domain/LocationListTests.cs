using Compute.Core.Domain.Entities.Models;
using Compute.Core.Helpers;

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

    public class LocationListTests
    {
        private static Location Saved(int id, string name) =>
            new() { Id = id, Name = name, Latitude = id, Longitude = id };

        [Fact]
        public void MissingFrom_KeepsOnlyWhatIsNotAlreadyHeld()
        {
            List<Location> held = [Saved(1, "A"), Saved(2, "B")];
            List<Location> incoming = [Saved(2, "B"), Saved(3, "C")];

            var missing = LocationList.MissingFrom(incoming, held);

            Assert.Single(missing);
            Assert.Equal(3, missing[0].Id);
        }

        [Fact]
        public void Upsert_ReplacesTheEntryMeaningTheSamePlace()
        {
            List<Location> list = [Saved(1, "A"), Saved(2, "B")];
            var renamed = Saved(2, "B renamed");

            Assert.True(LocationList.Upsert(list, renamed));

            Assert.Equal(2, list.Count);
            Assert.Equal("B renamed", list[1].Name);
        }

        [Fact]
        public void Upsert_OfTheVerySameInstance_ChangesNothing()
        {
            // Re-assigning the identical instance raises a Replace, which resets the carousel
            // to the first item and discards the user's selection.
            var held = Saved(1, "A");
            List<Location> list = [held];

            Assert.False(LocationList.Upsert(list, held));
            Assert.Single(list);
        }

        [Fact]
        public void Upsert_CanPutANewPlaceAtTheTop()
        {
            List<Location> list = [Saved(1, "A")];

            Assert.True(LocationList.Upsert(list, Saved(2, "B"), insertAtStart: true));

            Assert.Equal("B", list[0].Name);
            Assert.Equal("A", list[1].Name);
        }

        [Fact]
        public void CopyPositionInto_MovesTheFixOntoTheExistingEntry()
        {
            var slot = new Location { IsCurrent = true, Name = "old", Latitude = 1, Longitude = 1 };
            var fix = new Location
            {
                Name = "Kyoto",
                Latitude = 35.0117,
                Longitude = 135.7683,
                City = new City { CityName = "Kyoto" },
            };

            LocationList.CopyPositionInto(slot, fix);

            Assert.Equal("Kyoto", slot.Name);
            Assert.Equal(35.0117, slot.Latitude);
            Assert.Equal("Kyoto", slot.City.CityName);
            Assert.True(slot.IsCurrent);
        }
    }
}
