using Compute.Astro;
using Compute.Core.Domain.Entities.Models;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// The projection behind the sky chart. Small enough to state in full, and the one piece of
    /// the chart that is easy to get mirrored: a star map is drawn looking up, so east is on the
    /// left and the compass runs the other way round from a road map.
    /// </summary>
    public class SkyDomeTests
    {
        [Fact]
        public void TheZenithIsTheCentre()
        {
            var at = SkyDome.Project(azimuthDeg: 0.0, altitudeDeg: 90.0);

            Assert.NotNull(at);
            Assert.Equal(0.0, at!.Value.X, 9);
            Assert.Equal(0.0, at.Value.Y, 9);
        }

        [Fact]
        public void TheHorizonIsTheRim()
        {
            for (double az = 0.0; az < 360.0; az += 45.0)
            {
                var at = SkyDome.Project(az, 0.0);

                Assert.NotNull(at);
                Assert.Equal(1.0, Math.Sqrt(at!.Value.X * at.Value.X + at.Value.Y * at.Value.Y), 9);
            }
        }

        [Fact]
        public void NorthIsUpAndEastIsLeft()
        {
            // Canvas coordinates: y grows downward, so "up" is negative.
            var north = SkyDome.Project(0.0, 0.0)!.Value;
            var east = SkyDome.Project(90.0, 0.0)!.Value;
            var south = SkyDome.Project(180.0, 0.0)!.Value;
            var west = SkyDome.Project(270.0, 0.0)!.Value;

            Assert.Equal(-1.0, north.Y, 9);
            Assert.Equal(0.0, north.X, 9);

            Assert.Equal(-1.0, east.X, 9);
            Assert.Equal(0.0, east.Y, 9);

            Assert.Equal(1.0, south.Y, 9);
            Assert.Equal(1.0, west.X, 9);
        }

        [Fact]
        public void RadiusIsLinearInAltitude()
        {
            // Half way up the sky is half way out to the rim: what "equidistant" means, and what
            // makes a ring drawn at 30° land where a reader measuring with a fist expects it.
            Assert.Equal(0.5, Radius(45.0), 9);
            Assert.Equal(2.0 / 3.0, Radius(30.0), 9);
            Assert.Equal(1.0 / 3.0, Radius(60.0), 9);

            static double Radius(double altitude)
            {
                var at = SkyDome.Project(0.0, altitude)!.Value;
                return Math.Sqrt(at.X * at.X + at.Y * at.Y);
            }
        }

        [Fact]
        public void WhatIsBelowTheHorizonIsNotOnTheChart()
        {
            Assert.Null(SkyDome.Project(0.0, -0.001));
            Assert.Null(SkyDome.Project(123.0, -45.0));
        }

        [Fact]
        public void ThePoleStandsAtTheObserversLatitude()
        {
            // Whatever the date and whatever the hour, the celestial pole is due north at an
            // altitude equal to the latitude. It is the one position that never moves, which
            // makes it the check that the kept sidereal time is being applied correctly.
            foreach (var jd in new[]
                     {
                         AstroTime.JulianDay(2026, 1, 1),
                         AstroTime.JulianDay(2026, 6, 21, 7),
                         AstroTime.JulianDay(2026, 9, 12, 19, 37),
                     })
            {
                var dome = SkyDome.For(jd, 48.8566, 2.3522);
                var pole = dome.Horizontal(raDeg: 0.0, decDeg: 90.0);

                Assert.Equal(48.8566, pole.AltitudeDeg, 6);
                Assert.Equal(0.0, pole.AzimuthDeg, 6);
            }
        }

        [Fact]
        public void TheDomeAgreesWithTheEngineItShortcuts()
        {
            // SkyDome resolves the sidereal time once and subtracts each right ascension from it;
            // the engine's own path recomputes it per position. The saving is the whole point of
            // the type, so the two must not have drifted apart.
            var jd = AstroTime.JulianDay(2026, 9, 12, 19, 37);
            const double lat = 48.8566, lon = 2.3522;
            var dome = SkyDome.For(jd, lat, lon);

            foreach (var (ra, dec) in new[] { (0.0, 0.0), (101.29, -16.72), (279.23, 38.78), (359.9, -89.0) })
            {
                var expected = HorizontalCoordinates.OfEquatorial(jd, ra, dec, lat, lon);
                var actual = dome.Horizontal(ra, dec);

                Assert.Equal(expected.AltitudeDeg, actual.AltitudeDeg, 9);
                Assert.Equal(expected.AzimuthDeg, actual.AzimuthDeg, 9);
            }
        }
    }
}
