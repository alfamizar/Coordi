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
                    //Set EagerLoad Extensions to load Sun Cycle only.
                    Extensions = new EagerLoad_Extensions(EagerLoad_ExtensionsType.Solar_Cycle | EagerLoad_ExtensionsType.Zodiac)
                };

                var sunCycles = new List<BaseCelestialBodyCycle>();

                int TotalNumberOfDaysInTheCurrentYear = DateTime.IsLeapYear(date.Year) ? 366 : 365;

                var coordinate = new Coordinate(lat, lng, date, el);

                for (int index = 0; index < TotalNumberOfDaysInTheCurrentYear; index++)
                {
                    coordinate.GeoDate = date + TimeSpan.FromDays(index);

                    var zonedDateTime = coordinate.GetZonedDateTime();
                    coordinate.Offset = zonedDateTime?.Offset.Seconds / 3600 ?? 0;

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