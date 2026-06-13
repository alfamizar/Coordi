using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistence.Repository.Models;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Repository;
using AutoMapper;
using JustCompute.Persistence.Repository.Models.DTOs;
using JustCompute.Persistence.Repository.Constants;
using Compute.Core.Domain.Services;
using System.Globalization;

namespace JustCompute.Services.LocationService
{
    public class LocationService(
        IMapper mapper,
        ILocationsRepository locationsRepository,
        IWorldCitiesRepository<WorldCityTable> worldCitiesRepository) : ILocationService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ILocationsRepository _locationsRepository = locationsRepository;
        private readonly IWorldCitiesRepository<WorldCityTable> _worldCitiesRepository = worldCitiesRepository;

        public async Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1)
        {
            var city = await GetTheClosestCityToCoordinates(latitude, longitude);
            Location location = new()
            {
                Id = locationId,
                Latitude = latitude.ToString(CultureInfo.InvariantCulture),
                Longitude = longitude.ToString(CultureInfo.InvariantCulture),
                City = city,
            };
            return location;
        }

        public async Task<City> GetTheClosestCityToCoordinates(double latitude, double longitude)
        {
            var worldCity = await _worldCitiesRepository.GetTheNearestCityAsync(latitude, longitude);

            return _mapper.Map<City>(worldCity);
        }

        public async Task<List<Location>> GetSavedLocations()
        {
            var savedLocations = await _locationsRepository.GetItemsWithQueryAsync<LocationWithCityDTO>(RepositoryConstants.LocationWithCityQuery);
            var locations = _mapper.Map<List<Location>>(savedLocations);
            return locations;
        }

        public async Task SaveLocation(Location location)
        {
            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await _locationsRepository.SaveItemAsync(cityDTO);

            locationDTO.CityId = cityDTO.Id;
            await _locationsRepository.SaveItemAsync(locationDTO);
        }

        public async Task UpdateLocation(Location location)
        {
            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await _locationsRepository.UpdateItemAsync(cityDTO);

            locationDTO.CityId = cityDTO.Id;
            await _locationsRepository.UpdateItemAsync(locationDTO);
        }

        public async Task DeleteLocation(Location location)
        {
            var cityDTO = _mapper.Map<CityTable>(location);
            // Thanks to ON DELETE CASCADE location will be deleted automatically
            await _locationsRepository.DeleteItemAsync(cityDTO);
        }

        public async Task<List<Location>> SearchLocations(string searchParam)
        {
            var locations = await _worldCitiesRepository.FilterByCity(searchParam);
            return _mapper.Map<List<Location>>(locations);
        }
    }
}
