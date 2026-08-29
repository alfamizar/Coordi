using AutoMapper;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistence.AutoMapper;
using JustCompute.Persistence.Repository.Models;
using JustCompute.Persistence.Repository.Models.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compute.Core.Tests.Persistence
{
    /// <summary>
    /// The location and city tables have independent autoincrement sequences, so a location's id
    /// says nothing about which city row belongs to it. AutoMapper's name convention will happily
    /// map <c>Location.Id</c> onto <c>CityTable.Id</c>, which is right only by coincidence and
    /// silently corrupts the foreign key the moment the two sequences drift apart.
    /// </summary>
    public class LocationMappingTests
    {
        private static IMapper CreateMapper() =>
            new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>(), NullLoggerFactory.Instance)
                .CreateMapper();

        private static Location LocationWithDistinctIds() => new()
        {
            Id = 7,
            Name = "Tokyo",
            Latitude = 35.6839,
            Longitude = 139.7744,
            TimeZoneId = "Asia/Tokyo",
            City = new City { Id = 3, CityName = "Tokyo", CountryName = "Japan", Population = 39_105_000 },
        };

        [Fact]
        public void LocationToCityTable_UsesTheCityRowId_NotTheLocationId()
        {
            var cityTable = CreateMapper().Map<CityTable>(LocationWithDistinctIds());

            Assert.Equal(3, cityTable.Id);
            Assert.Equal("Tokyo", cityTable.CityName);
            Assert.Equal("Japan", cityTable.CountryName);
        }

        [Fact]
        public void LocationToLocationTable_PointsTheForeignKeyAtTheCityRow()
        {
            var locationTable = CreateMapper().Map<LocationTable>(LocationWithDistinctIds());

            Assert.Equal(3, locationTable.CityId);
            Assert.Equal("Asia/Tokyo", locationTable.TimeZoneId);
        }

        [Fact]
        public void LoadedLocation_CarriesTheCityRowIdBack()
        {
            var dto = new LocationWithCityDTO
            {
                Id = 7,
                Name = "Tokyo",
                Latitude = 35.6839,
                Longitude = 139.7744,
                CityRowId = 3,
                CityName = "Tokyo",
                CountryName = "Japan",
                Population = 39_105_000,
                TimeZoneId = "Asia/Tokyo",
            };

            var location = CreateMapper().Map<Location>(dto);

            Assert.Equal(7, location.Id);
            Assert.Equal(3, location.City.Id);
            Assert.Equal("Asia/Tokyo", location.TimeZoneId);
        }

        /// <summary>A round trip must not quietly swap the two ids.</summary>
        [Fact]
        public void RoundTrip_KeepsLocationAndCityIdsDistinct()
        {
            var mapper = CreateMapper();
            var original = LocationWithDistinctIds();

            var locationTable = mapper.Map<LocationTable>(original);
            var cityTable = mapper.Map<CityTable>(original);

            var reloaded = mapper.Map<Location>(new LocationWithCityDTO
            {
                Id = locationTable.Id == 0 ? original.Id : locationTable.Id,
                Name = locationTable.Name,
                Latitude = locationTable.Latitude,
                Longitude = locationTable.Longitude,
                CityRowId = cityTable.Id,
                CityName = cityTable.CityName!,
                CountryName = cityTable.CountryName!,
                Population = cityTable.Population,
                TimeZoneId = locationTable.TimeZoneId,
            });

            Assert.Equal(original.Id, reloaded.Id);
            Assert.Equal(original.City.Id, reloaded.City.Id);
            Assert.NotEqual(reloaded.Id, reloaded.City.Id);
        }
    }
}
