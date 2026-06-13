using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.ViewModels.Messages
{
    class LocationMessage(Location location, LocationInputContext locationInputContext)
    {
        public Location Location { get; private set; } = location;
        public LocationInputContext LocationInputContext { get; private set; } = locationInputContext;
    }
}
