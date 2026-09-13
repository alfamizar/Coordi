using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Eclipses;

namespace Compute.Core.Domain.Services.Sun
{
    public interface ISunService
    {
        Task<List<SolarEclipseInfo>> GetSunEclipsesAsync(Location location, DateTime date);
    }
}
