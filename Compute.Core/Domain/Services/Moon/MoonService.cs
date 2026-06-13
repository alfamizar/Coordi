using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Extensions;
using CoordinateSharp;
using NodaTime;
using Distance = Compute.Core.Domain.Entities.Models.Distance.Distance;

namespace Compute.Core.Domain.Services.Moon
{
    public class MoonService : IMoonService
    {
        public async Task<List<MoonCycle>> GetMoonCyclesAsync(double lat, double lng, DateTime date)
        {
            return await Task.Run(() =>
            {
                var el = new EagerLoad(EagerLoadType.Celestial)
                {
                    Extensions = new EagerLoad_Extensions(EagerLoad_ExtensionsType.Lunar_Cycle | EagerLoad_ExtensionsType.Zodiac)
                };

                var moonCycles = new List<MoonCycle>();

                var startDate = date.Date;
                var endDate = startDate.AddYears(1);
                var coordinate = new Coordinate(lat, lng, startDate, el);

                for (var day = startDate; day < endDate; day = day.AddDays(1))
                {
                    coordinate.GeoDate = day;
                    var zonedDateTime = coordinate.GetZonedDateTime();
                    coordinate.Offset = zonedDateTime?.Offset.ToTimeSpan().TotalHours ?? 0;

                    var celestialInfo = new MoonCycle
                    {
                        GeoDate = coordinate.GeoDate,
                        EarthHemisphere = lat > 0 ? BaseCelestialBodyCycle.Hemisphere.Northern : BaseCelestialBodyCycle.Hemisphere.Southern,
                        RiseTime = coordinate.CelestialInfo.MoonRise,
                        SetTime = coordinate.CelestialInfo.MoonSet,
                        Distance = new Distance(coordinate.CelestialInfo.MoonDistance.Kilometers),
                        PhaseName = (MoonPhase)coordinate.CelestialInfo.MoonIllum.PhaseNameEnum,
                        ZodiacSign = AstroExtensions.CalculateZodiacSign(coordinate.GeoDate),
                        // CoordinateSharp has deprecated its astrological-sign API and offers no
                        // equivalent moon-sign replacement, so keep using it to preserve this feature.
#pragma warning disable CS0618
                        MoonInZodiacSign = (AstrologicalSignType)coordinate.CelestialInfo.AstrologicalSigns.EMoonSign,
#pragma warning restore CS0618
                        MoonName = (Entities.Models.Moon.MoonName)coordinate.CelestialInfo.AlmanacMoonName.EName,
                        IsDaylightSavingTime = zonedDateTime?.IsDaylightSavingTime() ?? false,
                    };
                    moonCycles.Add(celestialInfo);
                }
                return moonCycles;
            });
        }

        public async Task<List<LunarEclipseDetails>> GetMoonEclipsesAsync(double lat, double lng, DateTime date)
        {
            return await Task.Run(() =>
            {
                return Celestial.Get_Lunar_Eclipse_Table(lat, lng, date);
            });
        }
    }
}
