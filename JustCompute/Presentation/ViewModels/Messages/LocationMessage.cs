using JustCompute.Presentation.ViewModels.Common;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.ViewModels.Messages
{
    class LocationMessage(Location location, LocationInputContext locationInputContext)
    {
        public Location Location { get; private set; } = location;
        public LocationInputContext LocationInputContext { get; private set; } = locationInputContext;
    }
}
