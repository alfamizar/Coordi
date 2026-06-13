using Compute.Core.Domain.Entities.Models;
using Compute.Core.Extensions;
using CoordinateSharp;

namespace Compute.Core.Domain.Services.Sun
{
    public class SunService : ISunService
    {
        public async Task<List<BaseCelestialBodyCycle>> GetSunCyclesAsync(double lat, double lng, DateTime date)
        {
            return await Task.Run(() =>
            {
                EagerLoad el = new(EagerLoadType.Celestial)
                {
                    Extensions = new EagerLoad_Extensions(EagerLoad_ExtensionsType.Solar_Cycle | EagerLoad_ExtensionsType.Zodiac)
                };

                var sunCycles = new List<BaseCelestialBodyCycle>();

                var startDate = date.Date;
                var endDate = startDate.AddYears(1);
                var coordinate = new Coordinate(lat, lng, startDate, el);

                for (var day = startDate; day < endDate; day = day.AddDays(1))
                {
                    coordinate.GeoDate = day;

                    var zonedDateTime = coordinate.GetZonedDateTime();
                    coordinate.Offset = zonedDateTime?.Offset.ToTimeSpan().TotalHours ?? 0;

                    BaseCelestialBodyCycle celestialInfo = new()
                    {
                        GeoDate = coordinate.GeoDate,
                        RiseTime = coordinate.CelestialInfo.SunRise,
                        SetTime = coordinate.CelestialInfo.SunSet,
                        ZodiacSign = AstroExtensions.CalculateZodiacSign(coordinate.GeoDate),
                        IsDaylightSavingTime = zonedDateTime?.IsDaylightSavingTime() ?? false,
                    };
                    sunCycles.Add(celestialInfo);
                }

                return sunCycles;
            });
        }

        public async Task<List<SolarEclipseDetails>> GetSunEclipsesAsync(double lat, double lng, DateTime date)
        {
            return await Task.Run(() =>
            {
                return Celestial.Get_Solar_Eclipse_Table(lat, lng, date);
            });
        }
    }
}
