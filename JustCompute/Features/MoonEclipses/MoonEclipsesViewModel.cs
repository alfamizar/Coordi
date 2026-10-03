using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Services.Moon;
using JustCompute.Features.Eclipses;
using JustCompute.Resources.Strings;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.MoonEclipses
{
    public partial class MoonEclipsesViewModel(
        ViewModelServices services,
        IMoonService moonService,
        IStringLocalizer<AppStringsRes> localizer)
        : EclipsesViewModel<LunarEclipseInfo>(services, localizer)
    {
        private readonly IMoonService _moonService = moonService;

        protected override Task<List<LunarEclipseInfo>> ComputeEclipsesAsync(Location location, DateTime utcNow) =>
            _moonService.GetMoonEclipsesAsync(location, utcNow);
    }
}
