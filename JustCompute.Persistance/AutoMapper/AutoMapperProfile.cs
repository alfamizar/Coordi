using AutoMapper;
using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository.Models.DTOs;

namespace JustCompute.Persistance.AutoMapper
{
    // todo: deal with linker ("mylinker") that deletes methods that are required by mapper
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Location, LocationWithCityDTO>()
                .ForMember(dest => dest.CityName, x => x.MapFrom(source => source.City.CityName))
                .ForMember(dest => dest.CountryName, x => x.MapFrom(source => source.City.CountryName))
                .ForMember(dest => dest.Population, x => x.MapFrom(source => source.City.Population))
                .ReverseMap();

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
                .ForMember(dest => dest.TimeZoneOffset, x => x.MapFrom(source => source.TimeZoneOffset));
        }
    }
}
