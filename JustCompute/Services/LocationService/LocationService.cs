using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository;
using Location = Compute.Core.Domain.Entities.Models.Location;
using Compute.Core.Repository;
using AutoMapper;
using JustCompute.Persistance.Repository.Models.DTOs;
using JustCompute.Persistance.Repository.Constants;
using Compute.Core.Domain.Services;

namespace JustCompute.Services.LocationService
{
    public class LocationService(IMapper mapper) : ILocationService
    {
        private readonly IMapper _mapper = mapper;

        public async Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1)
        {
            var city = await GetTheClosestCityToCoordinates(latitude, longitude);
            Location location = new()
            {
                Id = locationId,
                Latitude = latitude.ToString(),
                Longitude = longitude.ToString(),
                City = city,
            };
            return location;
        }

        public async Task<City> GetTheClosestCityToCoordinates(double latitude, double longitude)
        {
            IWorldCitiesRepository<WorldCityTable> repository = await WorldCitiesRepository.Instance;

            var worldCity = await repository.GetTheNearestCityAsync(latitude, longitude);

            return _mapper.Map<City>(worldCity);
        }

        public async Task<List<Location>> GetSavedLocations()
        {
            // Raw query also works
            ILocationsRepository repository = await SavedLocationsRepository.Instance;
            var savedLocations = await repository.GetItemsWithQueryAsync<LocationWithCityDTO>(RepositoryConstants.LocationWithCityQuery);
            var locations = _mapper.Map<List<Location>>(savedLocations);
            return locations;
        }

        public async Task SaveLocation(Location location)
        {
            ILocationsRepository repository = await SavedLocationsRepository.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await repository.SaveItemAsync(cityDTO);

            // update FOREIGN KEY
            locationDTO.CityId = cityDTO.Id;
            await repository.SaveItemAsync(locationDTO);
        }

        public async Task UpdateLocation(Location location)
        {
            ILocationsRepository repository = await SavedLocationsRepository.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await repository.UpdateItemAsync(cityDTO);

            // update FOREIGN KEY
            locationDTO.CityId = cityDTO.Id;
            await repository.UpdateItemAsync(locationDTO);
        }

        public async Task DeleteLocation(Location location)
        {
            ILocationsRepository repository = await SavedLocationsRepository.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            // Thanks to ON DELETE CASCADE location will be deleted automatically
            await repository.DeleteItemAsync(cityDTO);
        }

        public async Task<List<Location>> SearchLocations(string searchParam)
        {
            IWorldCitiesRepository<WorldCityTable> repository = await WorldCitiesRepository.Instance;

            var locations = await repository.FilterByCity(searchParam);
            return _mapper.Map<List<Location>>(locations);
        }
    }
}
