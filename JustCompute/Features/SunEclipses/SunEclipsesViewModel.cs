using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Services.Sun;
using JustCompute.Features.Eclipses;
using JustCompute.Resources.Strings;
using JustCompute.Shared.ViewModels;
using Microsoft.Extensions.Localization;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.SunEclipses
{
    public partial class SunEclipsesViewModel(
        ViewModelServices services,
        ISunService sunService,
        IStringLocalizer<AppStringsRes> localizer)
        : EclipsesViewModel<SolarEclipseInfo>(services, localizer)
    {
        private readonly ISunService _sunService = sunService;

        /// <summary>Contact times come back in the location's own zone, like the rest of the app.</summary>
        protected override Task<List<SolarEclipseInfo>> ComputeEclipsesAsync(Location location, DateTime utcNow) =>
            _sunService.GetSunEclipsesAsync(location, utcNow);
    }
}
