namespace Compute.Core.Domain.Entities.Models.Fuel
{
    /// <summary>The volume a journey burns, and what that costs.</summary>
    /// <param name="Cost">Null when no price has been given — the volume is still worth showing.</param>
    public sealed record FuelEstimate(double Volume, double? Cost);
}
