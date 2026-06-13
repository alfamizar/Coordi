namespace Compute.Core.Domain.Entities.Models.Distance
{
    public record DistanceUnitOfMeasure(DistanceType DistanceType, string DistanceUomName)
    {
        public override string ToString()
        {
            return DistanceUomName;
        }
    }
}
