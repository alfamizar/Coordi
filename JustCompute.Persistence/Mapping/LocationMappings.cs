using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistence.Repository.Models;
using JustCompute.Persistence.Repository.Models.DTOs;

namespace JustCompute.Persistence.Mapping
{
    /// <summary>
    /// Hand-written conversions between the domain models and the SQLite row types.
    ///
    /// These replace an AutoMapper profile. The convention-based mapper had to be told, member by
    /// member, not to do the wrong thing — its name matching happily mapped the location's id onto
    /// the city row — so the configuration was already as long as the code below while being far
    /// harder to read, and it cost reflection at runtime: the linker had to be handed
    /// <c>preserve="all"</c> over whole assemblies to stop it trimming members only reflection
    /// could see, which in turn kept LLVM compilation switched off for release builds.
    /// </summary>
    public static class LocationMappings
    {
        /// <summary>A saved location, joined with its city row, as the domain sees it.</summary>
        public static Location ToDomain(this LocationWithCityDTO row) => new()
        {
            Id = row.Id,
            Name = row.Name ?? string.Empty,
            Latitude = row.Latitude,
            Longitude = row.Longitude,
            IsActive = row.IsActive,
            IsCurrent = row.IsCurrent,
            City = new City
            {
                // The city's own row id, never the location's — they are separate sequences and
                // only ever agreed by coincidence.
                Id = row.CityRowId,
                CityName = row.CityName,
                CountryName = row.CountryName,
                Population = row.Population,
            },
            // A null id means a row saved before zones were stored. Leaving it empty makes the
            // location resolve its zone from its coordinates, which also repairs the daylight
            // saving the old whole-hour column could never represent.
            TimeZoneId = row.TimeZoneId ?? string.Empty,
        };

        public static List<Location> ToDomain(this IEnumerable<LocationWithCityDTO> rows) =>
            [.. rows.Select(ToDomain)];

        public static CityTable ToCityTable(this Location location) => new()
        {
            Id = location.City.Id,
            CityName = location.City.CityName,
            CountryName = location.City.CountryName,
            Population = location.City.Population,
        };

        public static LocationTable ToLocationTable(this Location location) => new()
        {
            Id = location.Id,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            CityId = location.City.Id,
            IsActive = location.IsActive ? 1 : 0,
            IsCurrent = location.IsCurrent ? 1 : 0,
            TimeZoneId = location.TimeZoneId,
            // Legacy whole-hour column, kept so an older build reading this database still works.
            TimeZoneOffset = (int)location.GetUtcOffset(DateTime.UtcNow).TotalHours,
        };

        /// <summary>A catalogue city offered as a place the user can pick.</summary>
        public static Location ToDomainLocation(this WorldCityTable row) => new()
        {
            Name = row.City,
            Latitude = row.Lat,
            Longitude = row.Lng,
            IsActive = true,
            IsCurrent = false,
            City = new City
            {
                CityName = row.City,
                CountryName = row.Country,
                Population = row.Population,
            },
            // Id and zone deliberately left unset: it is not a saved row yet, and the zone is
            // resolved from the city's own coordinates.
        };

        public static List<Location> ToDomainLocations(this IEnumerable<WorldCityTable> rows) =>
            [.. rows.Select(ToDomainLocation)];

        public static City ToCity(this WorldCityTable row) => new()
        {
            CityName = row.CityAscii,
            CountryName = row.Country,
            Population = row.Population,
        };
    }
}
