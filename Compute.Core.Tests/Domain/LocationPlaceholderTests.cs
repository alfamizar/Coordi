using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// Three kinds of row have no persisted id, and only one of them is the invented fallback.
    /// Confusing them is what put an undeletable London in the saved list: with no id it draws
    /// no delete affordance, so once listed it could never be removed.
    /// </summary>
    public class LocationPlaceholderTests
    {
        [Fact]
        public void ThePlaceholderIsNeitherSavedNorTheDeviceFix()
        {
            var placeholder = Location.CreatePlaceholder();

            Assert.True(LocationIdentity.IsPlaceholder(placeholder));
            Assert.False(placeholder.IsSaved);
            Assert.False(LocationIdentity.IsDeviceSlot(placeholder));
        }

        [Fact]
        public void TheDeviceFixIsNotAPlaceholder()
        {
            // Also has no id, and must keep its row: it is where the user actually is.
            var device = new Location { Name = "Here", Latitude = 48.85, Longitude = 2.35, IsCurrent = true };

            Assert.False(LocationIdentity.IsPlaceholder(device));
            Assert.True(LocationIdentity.IsDeviceSlot(device));
        }

        [Fact]
        public void ASavedPlaceIsNotAPlaceholder()
        {
            var saved = new Location { Id = 7, Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };

            Assert.False(LocationIdentity.IsPlaceholder(saved));
            Assert.True(saved.IsSaved);
        }

        [Fact]
        public void ASavedPlaceAtTheSameSpotAsThePlaceholderIsStillReal()
        {
            // Someone may genuinely save London. Having an id is what decides it, not the
            // coordinates, so their own row must survive.
            var londonSaved = new Location { Id = 3, Name = "London", Latitude = 51.5074, Longitude = -0.1278 };

            Assert.False(LocationIdentity.IsPlaceholder(londonSaved));
        }
    }
}
