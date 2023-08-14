using Compute.Core.Domain.Entities.Models;
using CoordinateSharp;

namespace Compute.Core.Domain.Services
{
    public interface ISunService
    {
        Task<List<BaseCelestialBodyCycle>> GetSunCyclesAsync(double lat, double lng, DateTime date, int timeZoneOffset);

        Task<List<SolarEclipseDetails>> GetSunEclipsesAsync(double lat, double lng, DateTime date);
    }
}
