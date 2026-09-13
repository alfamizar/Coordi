namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// When two <see cref="Location"/> objects mean the same place.
    ///
    /// Not reference equality, and not value equality either: the same place arrives from three
    /// sources that know different things about it — a saved row with an id, the device's own
    /// fix with no id, and a city picked from the catalogue — and the list has to recognise them
    /// as one entry or it grows duplicates every time it refreshes.
    /// </summary>
    public static class LocationIdentity
    {
        /// <summary>Coordinates this close are the same spot: about 11 cm at the equator.</summary>
        private const double CoordinateTolerance = 0.000001;

        public static bool AreSame(Location left, Location right)
        {
            // Two persisted rows are the same place only if they are the same row.
            if (left.Id > 0 && right.Id > 0)
            {
                return left.Id == right.Id;
            }

            // There is only ever one "where I am now", however far it has moved since.
            if (left.IsCurrent && right.IsCurrent)
            {
                return true;
            }

            return Math.Abs(left.Latitude - right.Latitude) < CoordinateTolerance
                && Math.Abs(left.Longitude - right.Longitude) < CoordinateTolerance
                && string.Equals(left.Name, right.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// The list's slot for the device's own position: current, but never saved. Told apart by
        /// having no persisted id, which is also what makes it neither editable nor deletable.
        /// </summary>
        public static bool IsDeviceSlot(Location location) => location.IsCurrent && location.Id <= 0;

        /// <summary>
        /// The fallback the location service invents so screens have something to compute from
        /// before anywhere has been chosen. Neither saved nor the device's own fix, which is what
        /// separates it from the two rows that legitimately have no id.
        /// </summary>
        public static bool IsPlaceholder(Location location) => !location.IsCurrent && location.Id <= 0;
    }
}
