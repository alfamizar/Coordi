namespace Compute.Core.Domain.Entities.Models.Distance
{
    public class DistanceUnitOfMeasure
    {
        public DistanceType DistanceType { get; private set; }

        public string DistanceUomName { get; private set; }

        public DistanceUnitOfMeasure (DistanceType distanceType, string distanceUomName)
        {
            DistanceType = distanceType;
            DistanceUomName = distanceUomName;
        }

        public override string ToString()
        {
            return DistanceUomName;
        }
    }
}
