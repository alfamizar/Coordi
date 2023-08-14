﻿using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.AstroSign;
using Compute.Core.Domain.Services;
using CoordinateSharp;

namespace Compute.Core.Services
{
    public class SunService : ISunService
    {
        public async Task<List<BaseCelestialBodyCycle>> GetSunCyclesAsync(double lat, double lng, DateTime date, int timeZoneOffset)
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
                coordinate.Offset += timeZoneOffset;

                for (int index = 0; index < TotalNumberOfDaysInTheCurrentYear; index++)
                {
                    coordinate.GeoDate = date + TimeSpan.FromDays(index);

                    BaseCelestialBodyCycle celestialInfo = new()
                    {
                        GeoDate = coordinate.GeoDate,
                        RiseTime = coordinate.CelestialInfo.SunRise,
                        SetTime = coordinate.CelestialInfo.SunSet,
                        ZodiacSign = (AstroZodiacSign)coordinate.CelestialInfo.AstrologicalSigns.EZodiacSign
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
                return Celestial.Get_Solar_Eclipse_Table(lat, lng, DateTime.Now);
            });
        }
    }
}