using Compute.Core.Domain.Entities.Models;
using JustCompute.Presentation.Locations;

namespace JustCompute.Presentation.Tests.Locations
{
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
