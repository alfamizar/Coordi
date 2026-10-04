using System.ComponentModel;
using JustCompute.Presentation.Locations;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.Tests.Locations
{
    /// <summary>
    /// The coordinate fields of the location editor and the Ruler. They were bound straight to
    /// numbers, which turned "50,5" into 505 in every language and "50.5" into 505 in German.
    /// </summary>
    public class EditableLocationTests
    {
        /// <summary>The field as a keyboard fills it: one character at a time.</summary>
        private static void Type(EditableLocation location, string text, bool latitude = true)
        {
            for (var i = 1; i <= text.Length; i++)
            {
                if (latitude) location.LatitudeText = text[..i];
                else location.LongitudeText = text[..i];
            }
        }

        [Theory]
        [InlineData("50,5")]
        [InlineData("50.5")]
        public void ADecimalTypedWithACommaOrADotIsRead(string typed)
        {
            var location = new EditableLocation();

            Type(location, typed);

            Assert.Equal(50.5, location.Latitude, 9);
            Assert.Equal(typed, location.LatitudeText);
            Assert.True(location.IsLatitudeValid);
        }

        [Fact]
        public void TheSeparatorIsNotEatenWhileItIsTheLastCharacter()
        {
            var location = new EditableLocation();

            location.LatitudeText = "50,";

            Assert.Equal("50,", location.LatitudeText);
        }

        [Fact]
        public void ALongitudeTakesTheFullRange()
        {
            var location = new EditableLocation();

            Type(location, "-179,95", latitude: false);

            Assert.Equal(-179.95, location.Longitude, 9);
            Assert.True(location.IsLongitudeValid);
        }

        [Theory]
        [InlineData("505")]
        [InlineData("abc")]
        [InlineData("")]
        public void WhatDoesNotReadAsALatitudeIsInvalid_AndLeavesTheLastOneThatDid(string typed)
        {
            var location = new EditableLocation();
            location.LatitudeText = "50.5";

            location.LatitudeText = typed;

            Assert.False(location.IsLatitudeValid);
            Assert.Equal(50.5, location.Latitude, 9);
            Assert.Equal(typed, location.LatitudeText);
        }

        [Fact]
        public void ANumberSetFromCodeRewritesTheField()
        {
            // A device fix, a city picked from search, a stop moved on the Ruler.
            var location = new EditableLocation();

            location.Latitude = 48.8566;

            Assert.Equal("48.8566", location.LatitudeText);
            Assert.True(location.IsLatitudeValid);
        }

        [Fact]
        public void TheFieldsOpenOnTheLocationBeingEdited()
        {
            var location = new EditableLocation(new Location { Latitude = 54.3667, Longitude = -18.6333 });

            Assert.Equal("54.3667", location.LatitudeText);
            Assert.Equal("-18.6333", location.LongitudeText);
            Assert.True(location.IsLatitudeValid && location.IsLongitudeValid);
        }

        [Fact]
        public void TypingAnnouncesTheNumber_SoTheRulerAndTheOffsetFollow()
        {
            var location = new EditableLocation();
            var changed = new List<string?>();
            ((INotifyPropertyChanged)location).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            location.LatitudeText = "50,5";

            Assert.Contains(nameof(EditableLocation.Latitude), changed);
            Assert.Contains(nameof(EditableLocation.TimeZoneOffset), changed);
            Assert.DoesNotContain(nameof(EditableLocation.LatitudeText), changed.Skip(1));
        }

        [Fact]
        public void TheSavedLocationCarriesWhatWasTyped()
        {
            var location = new EditableLocation();
            Type(location, "50,5");
            Type(location, "19,9", latitude: false);

            var saved = location.ToLocation();

            Assert.Equal(50.5, saved.Latitude, 9);
            Assert.Equal(19.9, saved.Longitude, 9);
        }
    }
}
