using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Services.Moon;
using JustCompute.Features.Eclipses;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.MoonEclipses
{
    public partial class MoonEclipsesViewModel(ViewModelServices services, IMoonService moonService)
        : EclipsesViewModel<LunarEclipseInfo>(services)
    {
        private readonly IMoonService _moonService = moonService;

        protected override Task<List<LunarEclipseInfo>> ComputeEclipsesAsync(Location location, DateTime utcNow) =>
            _moonService.GetMoonEclipsesAsync(location, utcNow);
    }
}
