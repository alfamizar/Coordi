using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models.Route
{
    /// <summary>
    /// Measures a route through a series of places.
    ///
    /// Pure geometry over the Vincenty solution in <see cref="Geodesy"/>, so it is tested
    /// directly rather than through a screen.
    /// </summary>
    public static class RoutePlanner
    {
        /// <summary>
        /// The figures for a list of points, or null when there are fewer than two — a single pin
        /// has no length, and saying "0 km" about it would be a measurement rather than a blank.
        /// </summary>
        public static RouteResult? Measure(IReadOnlyList<Location> points)
        {
            if (points.Count < 2) return null;

            var legs = new List<RouteLeg>(points.Count - 1);
            double total = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Location from = points[i];
                Location to = points[i + 1];

                GeodesicResult hop = Geodesy.Inverse(
                    from.Latitude, from.Longitude, to.Latitude, to.Longitude);

                total += hop.DistanceMeters;
                legs.Add(new RouteLeg(i + 1, i + 2, hop.DistanceMeters, hop.InitialBearingDeg));
            }

            Location first = points[0];
            Location last = points[^1];
            GeodesicResult straight = Geodesy.Inverse(
                first.Latitude, first.Longitude, last.Latitude, last.Longitude);

            // A metre of slack: a route that returns to within a metre of its start is a loop,
            // and dividing by that leftover would report a spectacular and meaningless ratio.
            double? detour = straight.DistanceMeters > 1.0
                ? total / straight.DistanceMeters
                : null;

            return new RouteResult(legs, total, straight.DistanceMeters, straight.InitialBearingDeg, detour);
        }
    }
}
