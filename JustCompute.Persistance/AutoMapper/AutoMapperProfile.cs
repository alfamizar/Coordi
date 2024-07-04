using AutoMapper;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Extensions;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository.Models.DTOs;

namespace JustCompute.Persistance.AutoMapper
{
    // todo: deal with linker ("mylinker") that deletes methods that are required by mapper
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

            /*CreateMap<WorldCityTable, Location>()
                .ForMember(dest => dest.Name, x => x.MapFrom(source => source.City))
                .ForMember(dest => dest.Latitude, x => x.MapFrom(source => source.Lat))
                .ForMember(dest => dest.Longitude, x => x.MapFrom(source => source.Lng))
                .ForMember(dest => dest.Id, x => x.MapFrom(source => 2))
                .ForMember(dest => dest.IsActive, x => x.MapFrom(source => true))
                .ForMember(dest => dest.IsCurrent, x => x.MapFrom(source => true))
                .ForMember(dest => dest.TimeZoneOffset, x => x.MapFrom(source => 2))
                .ReverseMap();*/

            /*CreateMap<WorldCityTable, Location>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignore Id mapping
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Lat))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Lng))
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Set IsActive as needed
            .ForMember(dest => dest.IsCurrent, opt => opt.Ignore()) // Set IsCurrent as needed
            .ForMember(dest => dest.TimeZoneOffset, opt => opt.Ignore()); // Set TimeZoneOffset */

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
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true)) // Assuming IsActive should default to true
                .ForMember(dest => dest.IsCurrent, opt => opt.MapFrom(src => false)) // Assuming IsCurrent should default to false
                .ForMember(dest => dest.TimeZoneOffset, opt => opt.Ignore()) // If you want to ignore the TimeZoneOffset field
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // Auto-generated Id should be ignored during mapping

            /*TimeZoneOffset
                .GetUtcOffsets()
                .FirstOrDefault(offset => offset.DisplayName == TimeExtensions.GetTimeZoneId(src.Lat, src.Lng))
                ?? TimeZoneOffset.DefaultTimeZoneOffset)*/

        }
    }
}
