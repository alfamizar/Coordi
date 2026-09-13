using Compute.Core.Domain.Entities.Models.Fuel;

namespace JustCompute.Features.Distance
{
    /// <summary>A consumption convention as the picker shows it.</summary>
    public sealed record FuelUnitOption(FuelConsumptionMode Mode, string Name)
    {
        public override string ToString() => Name;
    }
}
