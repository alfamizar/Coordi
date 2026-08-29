using AutoMapper;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Time;
using JustCompute.Persistence.Repository.Models;
using JustCompute.Persistence.Repository.Models.DTOs;

namespace JustCompute.Persistence.AutoMapper
{
    // TODO: linker ("mylinker") prevents the shrinker from removing methods required by AutoMapper — needs a proper linker config.
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<LocationWithCityDTO, Location>()
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => new City
                {
                    Id = src.CityRowId,
                    CityName = src.CityName,
                    CountryName = src.CountryName,
                    Population = src.Population
                }))
                // A null id means a row saved before zones were stored: leaving it unset makes the
                // location resolve its zone from its coordinates, which also repairs the daylight
                // saving the old whole-hour column could never represent.
                .ForMember(dest => dest.TimeZoneId, opt => opt.MapFrom(src => src.TimeZoneId ?? string.Empty))
                .ForMember(dest => dest.TimeZoneOffset, opt => opt.Ignore());

            CreateMap<Location, CityTable>()
                // Without this AutoMapper's name convention would map the LOCATION's id onto the
                // city row, which only ever pointed at the right row by coincidence.
                .ForMember(dest => dest.Id, x => x.MapFrom(source => source.City.Id))
                .ForMember(dest => dest.CityName, x => x.MapFrom(source => source.City.CityName))
                .ForMember(dest => dest.CountryName, x => x.MapFrom(source => source.City.CountryName))
                .ForMember(dest => dest.Population, x => x.MapFrom(source => source.City.Population));

            CreateMap<Location, LocationTable>()
                .ForMember(dest => dest.Name, x => x.MapFrom(source => source.Name))
                .ForMember(dest => dest.Latitude, x => x.MapFrom(source => source.Latitude))
                .ForMember(dest => dest.Longitude, x => x.MapFrom(source => source.Longitude))
                .ForMember(dest => dest.CityId, x => x.MapFrom(source => source.City.Id))
                .ForMember(dest => dest.IsActive, x => x.MapFrom(source => source.IsActive))
                .ForMember(dest => dest.IsCurrent, x => x.MapFrom(source => source.IsCurrent))
                .ForMember(dest => dest.TimeZoneId, x => x.MapFrom(source => source.TimeZoneId))
                .ForMember(dest => dest.TimeZoneOffset, x => x.MapFrom(source => (int)source.TimeZoneOffset.Offset.TotalHours));

            CreateMap<WorldCityTable, Location>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.City))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Lat))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Lng))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => new City
                {
                    CityName = src.City,
                    CountryName = src.Country,
                    Population = src.Population
                }))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsCurrent, opt => opt.MapFrom(src => false))
                // Left unset so the zone is resolved from the city's own coordinates.
                .ForMember(dest => dest.TimeZoneOffset, opt => opt.Ignore())
                .ForMember(dest => dest.TimeZoneId, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<WorldCityTable, City>()
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.CityAscii))
                .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country))
                .ForMember(dest => dest.Population, opt => opt.MapFrom(src => src.Population));
        }
    }
}
