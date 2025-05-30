using Compute.Core.Domain.Entities.Models;
using CoordinateSharp;

namespace Compute.Core.Domain.Services.Sun
{
    public interface ISunService
    {
        Task<List<BaseCelestialBodyCycle>> GetSunCyclesAsync(double lat, double lng, DateTime date);

        Task<List<SolarEclipseDetails>> GetSunEclipsesAsync(double lat, double lng, DateTime date);
    }
}
