using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Eclipses;
using Compute.Core.Extensions;
using Compute.Core.Utils;

namespace Compute.Core.Domain.Services.Sun
{
    public class SunService : ISunService
    {
        public async Task<List<SolarEclipseInfo>> GetSunEclipsesAsync(Location location, DateTime date)
        {
            var lat = location.Latitude;
            var lng = location.Longitude;

            return await Task.Run(() =>
            {
                var (startJd, endJd) = EclipseTableRange.ForCenturyOf(date);

                return Eclipse.SolarLocalCircumstancesBetween(startJd, endJd, lat, lng)
                    .Select(e =>
                    {
                        // One offset per eclipse, taken at greatest eclipse. Resolving each contact
                        // separately could straddle a daylight-saving change and print an event
                        // whose contacts run backwards.
                        var offsetHours = location.GetUtcOffsetHours(
                            AstroTime.DateTimeFromJulianDay(e.GlobalMaximumJdUtc));

                        return new SolarEclipseInfo
                        {
                            Date = AstroTime.DateTimeFromJulianDay(e.GlobalMaximumJdUtc).AddHours(offsetHours).Date,
                            Type = e.GlobalType,
                            LocalType = e.LocalType,
                            IsVisible = e.Visible,
                            PartialEclipseBegin = CelestialTimeUtils.ToLocalTimeOrDefault(e.PartialBeginJdUtc, offsetHours),
                            PartialEclipseEnd = CelestialTimeUtils.ToLocalTimeOrDefault(e.PartialEndJdUtc, offsetHours),
                            MaximumEclipse = CelestialTimeUtils.ToLocalTimeOrDefault(e.MaximumJdUtc, offsetHours),
                            CentralEclipseBegin = CelestialTimeUtils.ToLocalTimeOrDefault(e.CentralBeginJdUtc, offsetHours),
                            CentralEclipseEnd = CelestialTimeUtils.ToLocalTimeOrDefault(e.CentralEndJdUtc, offsetHours),
                            CentralDuration = e.CentralDurationSeconds is { } seconds
                                ? TimeSpan.FromSeconds(seconds)
                                : TimeSpan.Zero,
                            Magnitude = e.MagnitudeAtMax,
                        };
                    })
                    .ToList();
            });
        }
    }
}