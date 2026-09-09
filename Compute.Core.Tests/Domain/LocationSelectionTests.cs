using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// The app fills the one "where we compute from" slot in for the user until they pick
    /// somewhere. It used to fill it only with the invented placeholder, so a phone that knew
    /// exactly where it was still reported London's sunrise to every screen.
    /// </summary>
    public class LocationSelectionTests
    {
        private static Location Placeholder() => Location.CreatePlaceholder();

        private static Location Fix(double lat = 48.8566, double lon = 2.3522) =>
            new() { Name = "Paris", Latitude = lat, Longitude = lon, IsCurrent = true };

        private static Location Saved(int id = 7) =>
            new() { Id = id, Name = "Warsaw", Latitude = 52.2297, Longitude = 21.0122 };

        [Fact]
        public void TheDeviceFixTakesTheSlotFromThePlaceholder()
        {
            var placeholder = Placeholder();

            Assert.True(LocationSelection.ShouldAdoptDeviceFix(
                selected: placeholder, placeholder: placeholder, adoptedFix: null, replacedFix: null));
        }

        [Fact]
        public void TheDeviceFixTakesAnEmptySlot()
        {
            // Before any screen has read the property there is not even a placeholder yet.
            Assert.True(LocationSelection.ShouldAdoptDeviceFix(
                selected: null, placeholder: null, adoptedFix: null, replacedFix: null));
        }

        [Fact]
        public void APlaceTheUserPickedIsNotDisplacedByAFix()
        {
            var placeholder = Placeholder();
            var chosen = Saved();

            Assert.False(LocationSelection.ShouldAdoptDeviceFix(
                selected: chosen, placeholder: placeholder, adoptedFix: null, replacedFix: null));
        }

        [Fact]
        public void AnAdoptedFixGivesWayToTheNextOne()
        {
            // Adoption is the app standing in for a choice, not making one, so it must not
            // harden into something that outranks the position the device now reports.
            var placeholder = Placeholder();
            var adopted = Fix();

            Assert.True(LocationSelection.ShouldAdoptDeviceFix(
                selected: adopted, placeholder: placeholder, adoptedFix: adopted, replacedFix: null));
        }

        [Fact]
        public void ASelectionOnTheSupersededFixFollowsItAcross()
        {
            // The Locations screen moves a fresh fix onto the row already in the list and hands
            // that row back, so the instance the selection points at stops being the live one.
            // The single case where a real choice is re-pointed rather than left alone.
            var placeholder = Placeholder();
            var oldFix = Fix();

            Assert.True(LocationSelection.ShouldAdoptDeviceFix(
                selected: oldFix, placeholder: placeholder, adoptedFix: null, replacedFix: oldFix));
        }

        [Fact]
        public void AChoiceElsewhereSurvivesTheFixBeingSwappedOut()
        {
            var placeholder = Placeholder();
            var chosen = Saved();

            Assert.False(LocationSelection.ShouldAdoptDeviceFix(
                selected: chosen, placeholder: placeholder, adoptedFix: null, replacedFix: Fix()));
        }

        [Fact]
        public void NeitherStandInCountsAsAChoice()
        {
            // What the restore reads: a stand-in must never block the place the user picked in
            // an earlier session from being read back over it.
            var placeholder = Placeholder();
            var adopted = Fix();

            Assert.False(LocationSelection.IsUserChoice(placeholder, placeholder, adopted));
            Assert.False(LocationSelection.IsUserChoice(adopted, placeholder, adopted));
            Assert.False(LocationSelection.IsUserChoice(null, placeholder, adopted));
        }

        [Fact]
        public void APickedPlaceCountsAsAChoice()
        {
            var placeholder = Placeholder();

            Assert.True(LocationSelection.IsUserChoice(Saved(), placeholder, adoptedFix: null));
        }
    }
}
