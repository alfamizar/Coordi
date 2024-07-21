using AutoMapper;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Time;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository.Models.DTOs;

namespace JustCompute.Persistance.AutoMapper
{
    // todo: deal with linker ("mylinker") that prevents deleting methods by shrinker which are required by mapper
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<LocationWithCityDTO, Location>()
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => new City
                {
                    CityName = src.CityName,
                    CountryName = src.CountryName,
                    Population = src.Population
                }))
                .ForMember(dest => dest.TimeZoneOffset, opt => opt.MapFrom(src => 
                TimeZoneOffset.GetUtcOffsets().First(timeZone => timeZone.Hours == src.TimeZoneOffset)));

            CreateMap<Location, CityTable>()
                .ForMember(dest => dest.CityName, x => x.MapFrom(source => source.City.CityName))
                .ForMember(dest => dest.CountryName, x => x.MapFrom(source => source.City.CountryName))
                .ForMember(dest => dest.Population, x => x.MapFrom(source => source.City.Population));

            CreateMap<Location, LocationTable>()
                .ForMember(dest => dest.Name, x => x.MapFrom(source => source.Name))
                .ForMember(dest => dest.Latitude, x => x.MapFrom(source => source.Latitude))
                .ForMember(dest => dest.Longitude, x => x.MapFrom(source => source.Longitude))
                .ForMember(dest => dest.CityId, x => x.MapFrom(source => source.Id))
                .ForMember(dest => dest.IsActive, x => x.MapFrom(source => source.IsActive))
                .ForMember(dest => dest.IsCurrent, x => x.MapFrom(source => source.IsCurrent))
                .ForMember(dest => dest.TimeZoneOffset, x => x.MapFrom(source => source.TimeZoneOffset.Hours));

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
                .ForMember(dest => dest.TimeZoneOffset, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<WorldCityTable, City>()
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.CityAscii))
                .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country))
                .ForMember(dest => dest.Population, opt => opt.MapFrom(src => src.Population));
        }
    }
}
