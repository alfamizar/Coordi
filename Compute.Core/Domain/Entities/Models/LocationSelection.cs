namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// Which of the three candidates wins the one slot every screen computes from.
    ///
    /// A place the user picked always wins. Failing that the app fills the slot in for them,
    /// first with the device's own fix and, until one arrives, with an invented placeholder.
    /// The distinction that matters is not what the location says but where it came from, so
    /// these are reference comparisons against the two stand-ins the service is holding.
    /// </summary>
    public static class LocationSelection
    {
        /// <summary>
        /// Whether the current selection is somewhere the user actually picked, as opposed to
        /// either stand-in. A stand-in must never block a persisted choice from being restored
        /// over it, which is the other thing this answers.
        /// </summary>
        public static bool IsUserChoice(Location? selected, Location? placeholder, Location? adoptedFix) =>
            selected is not null
            && !ReferenceEquals(selected, placeholder)
            && !ReferenceEquals(selected, adoptedFix);

        /// <summary>
        /// Whether an arriving device fix should take the slot.
        /// </summary>
        /// <param name="replacedFix">
        /// The fix this one supersedes. The Locations screen moves a fresh fix onto the row
        /// already in the list and hands that row back, so a selection still pointing at the
        /// superseded instance has to follow it across or it quietly goes stale — the one case
        /// where a genuine user choice is re-pointed rather than left alone.
        /// </param>
        public static bool ShouldAdoptDeviceFix(
            Location? selected, Location? placeholder, Location? adoptedFix, Location? replacedFix) =>
            !IsUserChoice(selected, placeholder, adoptedFix)
            || (replacedFix is not null && ReferenceEquals(selected, replacedFix));

        /// <summary>A move shorter than this is never a change of town, whatever the nearest town says.</summary>
        public const double MinimumTownChangeMeters = 5_000;

        /// <summary>
        /// Whether <paramref name="fresh"/> puts the device in another town than <paramref name="shown"/>:
        /// the nearest town differs, and it has moved further than a town's edge can wobble.
        ///
        /// Both, because either alone misfires. Living between two towns, a network fix a few hundred
        /// metres out flips the nearest one on alternate launches; and a big city is wider than any
        /// distance worth asking about. Towns are compared by name and country, because the catalogue
        /// they come from gives its rows no ids.
        /// </summary>
        public static bool IsDifferentTown(Location shown, Location fresh) =>
            !IsSameTown(shown.City, fresh.City)
            && new GeoPoint(shown.Latitude, shown.Longitude)
                .DistanceMetersTo(new GeoPoint(fresh.Latitude, fresh.Longitude)) > MinimumTownChangeMeters;

        private static bool IsSameTown(City a, City b) =>
            string.Equals(a.CityName, b.CityName, StringComparison.Ordinal)
            && string.Equals(a.CountryName, b.CountryName, StringComparison.Ordinal);
    }
}
