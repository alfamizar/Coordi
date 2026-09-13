namespace Compute.Core.Domain.Entities.Models.Route
{
    /// <summary>One measured hop between consecutive points.</summary>
    /// <param name="FromNumber">1-based index of the point this leg leaves.</param>
    /// <param name="DistanceMeters">
    /// Metres. Turned into words by the screen, which is where the reader's chosen unit is known.
    /// </param>
    public sealed record RouteLeg(
        int FromNumber,
        int ToNumber,
        double DistanceMeters,
        double InitialBearingDeg);

    /// <summary>
    /// A whole route: every leg, the summed length, the straight first-to-last line, and how much
    /// further the route goes than that line.
    /// </summary>
    /// <param name="DetourRatio">
    /// Total ÷ direct. Null when the route ends where it began, which leaves no meaningful ratio
    /// against a zero-length line.
    /// </param>
    public sealed record RouteResult(
        IReadOnlyList<RouteLeg> Legs,
        double TotalMeters,
        double DirectMeters,
        double DirectBearingDeg,
        double? DetourRatio);
}
