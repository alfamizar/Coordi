using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Entities.Models.Moon;

namespace Compute.Core.Domain.Services.Moon
{
    public interface IMoonService
    {
        Task<List<LunarEclipseInfo>> GetMoonEclipsesAsync(Location location, DateTime date);
    }
}