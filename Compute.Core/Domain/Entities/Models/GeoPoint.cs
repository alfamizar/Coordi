using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// A plain latitude/longitude pair in decimal degrees — the lightweight stand-in for
    /// what used to be a <c>CoordinateSharp.Coordinate</c> wherever the app only needed a
    /// position to measure from.
    /// </summary>
    /// <param name="Latitude">Latitude in decimal degrees.</param>
    /// <param name="Longitude">Longitude in decimal degrees, positive east.</param>
    public readonly record struct GeoPoint(double Latitude, double Longitude)
    {
        /// <summary>Ellipsoidal (WGS84) distance to <paramref name="other"/> in metres.</summary>
        public double DistanceMetersTo(GeoPoint other) =>
            Geodesy.DistanceMeters(Latitude, Longitude, other.Latitude, other.Longitude);
    }
}
