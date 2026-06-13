using Compute.Core.Domain.Entities.Models.Speed;

namespace JustCompute.Features.Settings
{
    public record SpeedOption(SpeedType SpeedType, string Name)
    {
        public override string ToString() => Name;
    }
}
