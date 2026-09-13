using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Domain.Entities.Models.Moon;
using Compute.Core.Extensions;
using Compute.Core.Utils;
using Distance = Compute.Core.Domain.Entities.Models.Distance.Distance;
using MoonPhase = Compute.Core.Domain.Entities.Models.Moon.MoonPhase;

namespace Compute.Core.Domain.Services.Moon
{
    public class MoonService : IMoonService
    {
        public async Task<List<LunarEclipseInfo>> GetMoonEclipsesAsync(Location location, DateTime date)
        {
            var lat = location.Latitude;
            var lng = location.Longitude;

            return await Task.Run(() =>
            {
                var (startJd, endJd) = EclipseTableRange.ForCenturyOf(date);

                return Eclipse.LunarEclipseCircumstancesBetween(startJd, endJd)
                    .Select(e =>
                    {
                        // One offset per eclipse, taken at greatest eclipse. Resolving each contact
                        // separately could straddle a daylight-saving change and print an event
                        // whose contacts run backwards.
                        var offsetHours = location.GetUtcOffsetHours(
                            AstroTime.DateTimeFromJulianDay(e.MaximumJdUtc));

                        return new LunarEclipseInfo
                        {
                            Date = AstroTime.DateTimeFromJulianDay(e.MaximumJdUtc).AddHours(offsetHours).Date,
                            Type = e.Type,
                            // A lunar eclipse looks the same everywhere on the night side, so
                            // "visible from here" reduces to the Moon being above the horizon.
                            IsVisible = HorizontalCoordinates
                                .OfMoon(e.MaximumJdUtc, lat, lng, applyRefraction: true)
                                .AltitudeDeg > 0.0,
                            PenumbralEclipseBegin = CelestialTimeUtils.ToLocalTimeOrDefault(e.PenumbralBeginJdUtc, offsetHours),
                            PenumbralEclipseEnd = CelestialTimeUtils.ToLocalTimeOrDefault(e.PenumbralEndJdUtc, offsetHours),
                            PartialEclipseBegin = CelestialTimeUtils.ToLocalTimeOrDefault(e.PartialBeginJdUtc, offsetHours),
                            PartialEclipseEnd = CelestialTimeUtils.ToLocalTimeOrDefault(e.PartialEndJdUtc, offsetHours),
                            TotalEclipseBegin = CelestialTimeUtils.ToLocalTimeOrDefault(e.TotalBeginJdUtc, offsetHours),
                            TotalEclipseEnd = CelestialTimeUtils.ToLocalTimeOrDefault(e.TotalEndJdUtc, offsetHours),
                            MidEclipse = CelestialTimeUtils.ToLocalTimeOrDefault(e.MaximumJdUtc, offsetHours),
                            UmbralMagnitude = e.UmbralMagnitude,
                            PenumbralMagnitude = e.PenumbralMagnitude,
                        };
                    })
                    .ToList();
            });
        }
    }
}