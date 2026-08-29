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
                Latitude = latitude,
                Longitude = longitude,
                City = city,
                // The Locations list labels every row by Name, so leaving it unset showed the
                // device's own position as a nameless pair of coordinates.
                Name = city.CityName,
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

            // Persist the city's generated ID back into the location record.
            locationDTO.CityId = cityDTO.Id;
            await _locationsRepository.SaveItemAsync(locationDTO);

            // Propagate the IDs back to the domain object. The city id matters as much as the
            // location's: a later update or delete has to address the right city row, and the two
            // id sequences are independent.
            location.Id = locationDTO.Id;
            location.City.Id = cityDTO.Id;
        }

        public async Task UpdateLocation(Location location)
        {
            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            if (cityDTO.Id <= 0)
            {
                // Nothing to point the foreign key at; writing it would blank out the link.
                throw new InvalidOperationException(
                    $"Location '{location.Name}' has no city row to update.");
            }

            await _locationsRepository.UpdateItemAsync(cityDTO);

            locationDTO.CityId = cityDTO.Id;
            await _locationsRepository.UpdateItemAsync(locationDTO);
        }

        public async Task DeleteLocation(Location location)
        {
            var cityDTO = _mapper.Map<CityTable>(location);

            if (cityDTO.Id <= 0)
            {
                // Deleting id 0 would silently match nothing — or the wrong row.
                throw new InvalidOperationException(
                    $"Location '{location.Name}' has no city row to delete.");
            }

            // Thanks to ON DELETE CASCADE location will be deleted automatically
            await _locationsRepository.DeleteItemAsync(cityDTO);
        }

        /// <summary>
        /// How many cities a search will return. The catalogue holds tens of thousands; a browse
        /// list only needs enough to scroll, and typing narrows it long before the cap matters.
        /// </summary>
        public const int MaxSearchResults = 200;

        public async Task<List<Location>> SearchLocations(string searchParam)
        {
            var locations = await _worldCitiesRepository.FilterByCity(searchParam, MaxSearchResults);
            return _mapper.Map<List<Location>>(locations);
        }
    }
}