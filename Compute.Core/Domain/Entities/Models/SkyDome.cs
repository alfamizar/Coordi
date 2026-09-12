using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// The sky over one place at one instant, flattened onto a disc.
    ///
    /// Zenith at the centre, horizon at the rim, north up and <b>east on the left</b>. That last
    /// one looks wrong beside a map and is right here: this is the view looking up, so the
    /// compass runs anticlockwise, the way it does on a planisphere held overhead.
    ///
    /// Radius is linear in altitude — the azimuthal equidistant projection. It stretches
    /// everything near the horizon, which is where the atmosphere already ruins the view, and
    /// keeps the overhead sky honest.
    /// </summary>
    public readonly struct SkyDome
    {
        private readonly double _hourAngleAtRa0Deg;
        private readonly double _latitudeDeg;

        private SkyDome(double hourAngleAtRa0Deg, double latitudeDeg)
        {
            _hourAngleAtRa0Deg = hourAngleAtRa0Deg;
            _latitudeDeg = latitudeDeg;
        }

        /// <summary>
        /// The dome as seen from here, now. The sidereal time is resolved once and kept, because
        /// the chart converts a thousand catalogue positions through it for a single frame.
        /// </summary>
        public static SkyDome For(double jdUtc, double latitudeDeg, double longitudeEastDeg) =>
            new(HorizontalCoordinates.HourAngleDeg(jdUtc, 0.0, longitudeEastDeg), latitudeDeg);

        /// <summary>
        /// Where an equatorial position of date stands in this sky.
        ///
        /// The hour angle of a position is the hour angle of RA zero less its own right
        /// ascension, so the expensive part of <see cref="HorizontalCoordinates.OfEquatorial"/>
        /// is done once in <see cref="For"/> and never again.
        /// </summary>
        public Horizontal Horizontal(double raDeg, double decDeg) =>
            HorizontalCoordinates.FromHourAngle(_hourAngleAtRa0Deg - raDeg, decDeg, _latitudeDeg);

        /// <summary>
        /// Where a position falls on the unit disc, or null when it is below the horizon.
        ///
        /// Returned in the range [-1, 1] on both axes rather than in pixels, so the rejection
        /// test and the arrangement of the compass can be checked without a canvas.
        /// </summary>
        public static (double X, double Y)? Project(double azimuthDeg, double altitudeDeg)
        {
            if (altitudeDeg < 0.0) return null;

            var r = (90.0 - altitudeDeg) / 90.0;
            var az = azimuthDeg * Math.PI / 180.0;

            // Both negative: the azimuth turns anticlockwise on a chart of the sky, and y grows
            // downward on a canvas while north is up.
            return (-r * Math.Sin(az), -r * Math.Cos(az));
        }
    }
}
