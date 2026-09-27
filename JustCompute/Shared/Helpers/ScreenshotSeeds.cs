using JustCompute.Shared.Abstractions.Screenshots;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.Helpers
{
    /// <summary>What every real launch gets: nothing seeded, every screen opens on the present.</summary>
    public sealed class NoScreenshotSeed : IScreenshotSeed
    {
        public IReadOnlyList<Location> RouteStops => [];

        public DemoTrip? Trip => null;

        public int? SkyHour => null;
    }

    /// <summary>
    /// Whatever the capture script handed <see cref="ScreenshotHarness"/>. Registered in Debug
    /// builds only; read on every call because the harness parses its inputs after the
    /// container is built.
    /// </summary>
    public sealed class HarnessScreenshotSeed : IScreenshotSeed
    {
        public IReadOnlyList<Location> RouteStops => ScreenshotHarness.SeededRouteStops;

        public DemoTrip? Trip =>
            ScreenshotHarness.SeededTrip is { } trip ? new DemoTrip(trip.Travelled, trip.Direct, trip.Bearing) : null;

        public int? SkyHour => ScreenshotHarness.SeededSkyHour;
    }
}
