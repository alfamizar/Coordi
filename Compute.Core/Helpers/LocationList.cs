using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Helpers
{
    /// <summary>
    /// Merging places into the list the Locations screen shows.
    ///
    /// The screen assembles one list out of three sources that keep arriving independently —
    /// saved rows, the device fix, and whatever the user just picked — so the rules for "already
    /// there" and "put it at the top" are what stop it filling with duplicates. They are pure
    /// list operations, so they live here where they can be tested, rather than inside a view
    /// model that needs a running app to exercise.
    /// </summary>
    public static class LocationList
    {
        /// <summary>Those of <paramref name="candidates"/> the list does not already hold.</summary>
        public static List<Location> MissingFrom(
            IEnumerable<Location> candidates, IReadOnlyCollection<Location> existing) =>
            [.. candidates.Where(candidate => !existing.Any(held => LocationIdentity.AreSame(held, candidate)))];

        /// <summary>
        /// Adds a place, or replaces the entry that already means the same place.
        /// </summary>
        /// <returns>True when the list changed.</returns>
        public static bool Upsert(IList<Location> list, Location location, bool insertAtStart = false)
        {
            Location? existing = list.FirstOrDefault(held => LocationIdentity.AreSame(held, location));

            if (existing is not null)
            {
                // Re-assigning the identical instance still raises a Replace, which snaps the
                // carousel back to the first item and throws away the user's selection.
                if (ReferenceEquals(existing, location)) return false;

                list[list.IndexOf(existing)] = location;
                return true;
            }

            if (insertAtStart)
            {
                list.Insert(0, location);
            }
            else
            {
                list.Add(location);
            }

            return true;
        }

        /// <summary>
        /// Moves a fresh device fix onto the entry already in the list, rather than replacing it.
        /// The list item is what the carousel is bound to, so swapping the instance loses the
        /// user's place in it.
        /// </summary>
        public static void CopyPositionInto(Location target, Location source)
        {
            target.Name = source.Name;
            target.Latitude = source.Latitude;
            target.Longitude = source.Longitude;
            target.City = source.City;
            target.IsCurrent = true;
        }
    }
}
