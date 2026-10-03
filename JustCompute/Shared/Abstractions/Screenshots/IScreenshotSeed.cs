using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.Abstractions.Screenshots;

/// <summary>
/// State a store screenshot needs but no deep link can produce: a route already pinned, a trip
/// already running, a sky already dark. Screens ask for it rather than reading the harness
/// themselves, so which of the two implementations runs is decided once, where services are
/// registered, instead of by a preprocessor block in every screen that takes part.
/// </summary>
public interface IScreenshotSeed
{
    /// <summary>Pins the Ruler should open with, or empty.</summary>
    IReadOnlyList<Location> RouteStops { get; }

    /// <summary>A trip for Speed and Distance to show as running, or null.</summary>
    DemoTrip? Trip { get; }

    /// <summary>The local hour the sky chart should open on, or null to open on now.</summary>
    int? SkyHour { get; }
}

/// <summary>Travelled and straight-line metres, and the bearing between the ends.</summary>
public sealed record DemoTrip(double Travelled, double Direct, int Bearing);
