using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Services.Sun;
using JustCompute.Features.Eclipses;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.SunEclipses
{
    public partial class SunEclipsesViewModel(ViewModelServices services, ISunService sunService)
        : EclipsesViewModel<SolarEclipseInfo>(services)
    {
        private readonly ISunService _sunService = sunService;

        /// <summary>Contact times come back in the location's own zone, like the rest of the app.</summary>
        protected override Task<List<SolarEclipseInfo>> ComputeEclipsesAsync(Location location, DateTime utcNow) =>
            _sunService.GetSunEclipsesAsync(location, utcNow);
    }
}
