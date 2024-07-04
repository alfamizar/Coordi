using Compute.Core.Domain.Entities.Models.Moon;
using CoordinateSharp;

namespace Compute.Core.Domain.Services.Moon
{
    public interface IMoonService
    {
        Task<List<MoonCycle>> GetMoonCyclesAsync(double lat, double lng, DateTime date, int timeZoneOffset);

        Task<List<LunarEclipseDetails>> GetMoonEclipsesAsync(double lat, double lng, DateTime date);
    }
}