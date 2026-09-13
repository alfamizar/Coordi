using Compute.Astro;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// What one planet is doing, for one place, on one day: where it stands now and the crossing
    /// it makes from rise through its highest point to set.
    ///
    /// Immutable and computed once, like <see cref="CelestialSnapshot"/> — except for the two
    /// "now" figures, which a screen recomputes on its clock tick.
    /// </summary>
    /// <param name="Planet">Which planet. Earth is never among them.</param>
    /// <param name="AltitudeDeg">Altitude above the horizon now, geometric (no refraction).</param>
    /// <param name="AzimuthDeg">Azimuth now, degrees clockwise from north.</param>
    /// <param name="Rise">Rise in local time, or null when the planet is circumpolar or never rises.</param>
    /// <param name="Transit">Upper culmination in local time. Always present: a planet crosses the meridian even when it stays below the horizon.</param>
    /// <param name="TransitAltitudeDeg">Altitude at that culmination. Negative when the best of the day is still below the horizon.</param>
    /// <param name="Set">Set in local time, or null for the same two reasons as <paramref name="Rise"/>.</param>
    /// <param name="Circumpolar">Above the horizon for the whole day.</param>
    /// <param name="NeverRises">Below it for the whole day.</param>
    /// <param name="DistanceAu">Distance from Earth in astronomical units.</param>
    public sealed record PlanetSnapshot(
        Planet Planet,
        double AltitudeDeg,
        double AzimuthDeg,
        DateTime? Rise,
        DateTime Transit,
        double TransitAltitudeDeg,
        DateTime? Set,
        bool Circumpolar,
        bool NeverRises,
        double DistanceAu)
    {
        /// <summary>Whether the planet is above the horizon at the instant this was taken.</summary>
        public bool IsUp => AltitudeDeg > 0.0;

        /// <summary>
        /// The seven planets other than Earth, in order out from the Sun, for a place and an
        /// instant. Always all seven: whether one is worth looking at is the reader's judgement,
        /// and a list that silently dropped Neptune would be answering a question nobody asked.
        /// </summary>
        /// <param name="utcNow">The instant the "now" figures describe.</param>
        /// <param name="localDate">The local day whose crossing is wanted.</param>
        /// <param name="offsetHours">The place's offset from UTC, for reporting times in its own clock.</param>
        public static IReadOnlyList<PlanetSnapshot> AllFor(
            double latitude,
            double longitude,
            DateTime utcNow,
            DateTime localDate,
            double offsetHours)
        {
            var jdNow = AstroTime.JulianDay(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

            // Local noon, in UTC: the crossing nearest it is the one belonging to this local day,
            // which is the same convention the Sun and Moon models use for a calendar date.
            var jdLocalNoon = AstroTime.JulianDay(localDate.Date) + (12.0 - offsetHours) / 24.0;

            return
            [
                .. OrderedPlanets.Select(planet => For(planet, latitude, longitude, jdNow, jdLocalNoon, offsetHours))
            ];
        }

        private static readonly Planet[] OrderedPlanets =
        [
            Planet.Mercury, Planet.Venus, Planet.Mars,
            Planet.Jupiter, Planet.Saturn, Planet.Uranus, Planet.Neptune,
        ];

        private static PlanetSnapshot For(
            Planet planet,
            double latitude,
            double longitude,
            double jdNow,
            double jdLocalNoon,
            double offsetHours)
        {
            var now = Planets.GeocentricEquatorial(planet, jdNow);
            var here = HorizontalCoordinates.OfEquatorial(
                jdNow, now.RightAscensionDeg, now.DeclinationDeg, latitude, longitude);

            var passage = PassageOf(planet, latitude, longitude, jdLocalNoon);

            return new PlanetSnapshot(
                Planet: planet,
                AltitudeDeg: here.AltitudeDeg,
                AzimuthDeg: here.AzimuthDeg,
                Rise: ToLocal(passage.RiseJdUtc, offsetHours),
                Transit: ToLocal(passage.TransitJdUtc, offsetHours),
                TransitAltitudeDeg: passage.TransitAltitudeDeg,
                Set: ToLocal(passage.SetJdUtc, offsetHours),
                Circumpolar: passage.Circumpolar,
                NeverRises: passage.NeverRises,
                DistanceAu: Planets.GeocentricDistanceAu(planet, jdNow));
        }

        /// <summary>
        /// <see cref="Culmination"/> solves for something that holds still, and a planet does not
        /// quite. Solving once with the position at noon puts Mercury's transit up to a minute out,
        /// because it has moved by the time it gets there; re-solving with the position at that
        /// first answer removes almost all of it, and a third pass would move nothing a clock
        /// showing minutes could display.
        /// </summary>
        private static SkyPassage PassageOf(Planet planet, double latitude, double longitude, double jdReference)
        {
            var first = Planets.GeocentricEquatorial(planet, jdReference);
            var transit = Culmination.TransitNear(jdReference, first.RightAscensionDeg, longitude);

            var atTransit = Planets.GeocentricEquatorial(planet, transit);
            return Culmination.Near(
                transit, atTransit.RightAscensionDeg, atTransit.DeclinationDeg, latitude, longitude);
        }

        private static DateTime? ToLocal(double? jdUtc, double offsetHours) =>
            jdUtc is double jd ? ToLocal(jd, offsetHours) : null;

        private static DateTime ToLocal(double jdUtc, double offsetHours) =>
            AstroTime.DateTimeFromJulianDay(jdUtc).AddHours(offsetHours);
    }
}
