using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository;
using System.Text;
using DeviceLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using Compute.Core.Repository;
using AutoMapper;
using JustCompute.Persistance.Repository.Models.DTOs;
using JustCompute.Persistance.Repository.Constants;
using DotNext;
using Compute.Core.Domain.Errors;

namespace JustCompute.Services.LocationService
{
    public class LocationManager : ILocationManager
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IMapper _mapper;

        private CancellationTokenSource _cancelTokenSource;

        public TaskCompletionSource<bool> GettingLocationFinished { get; private set; }
        public bool IsGettingCurrentLocation { get; private set; }
        public Location CurrentLocation { get; set; }

        public LocationManager(IStringLocalizer<AppStringsRes> localizer, IMapper mapper)
        {
            _localizer = localizer;
            _mapper = mapper;
        }

        public async Task<Result<Location, FaultCode>> RequestCurrentLocation()
        {
            try
            {
                GettingLocationFinished = new TaskCompletionSource<bool>();

                IsGettingCurrentLocation = true;

                GeolocationRequest request = new(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceLocation deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);

                var timeZoneInfo = TimeZoneInfo.Local;
                var offset = timeZoneInfo.GetUtcOffset(DateTime.Now).Hours;

                var city = await GetCity(deviceLocation.Latitude, deviceLocation.Longitude);
                Location location = new Location()
                {
                    Latitude = deviceLocation.Latitude,
                    Longitude = deviceLocation.Longitude,
                    City = city,
                    TimeZoneOffset = offset
                };

                CurrentLocation = location;

                return location;
            }
            catch (FeatureNotSupportedException)
            {
                return new(FaultCode.FeatureNotSupported);
            }
            catch (FeatureNotEnabledException)
            {
                return new(FaultCode.FeatureNotEnabled);
            }
            catch (PermissionException)
            {
                return new(FaultCode.PermissionException);
            }
            catch (Exception)
            {
                return new(FaultCode.GenericGetLocationException);
            }
            finally
            {
                IsGettingCurrentLocation = false;
                GettingLocationFinished.TrySetResult(true);
            }
        }

                public void CancelRequest()
        {
            if (IsGettingCurrentLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }

        private async Task<City> GetCity(double latitude, double longitude)
        {
            IWorldCitiesDatabase<WorldCityTable> database = await WorldCitiesDatabase.Instance;

            var worldCity = await database.GetTheNearestCityAsync(latitude, longitude);
            City city = new City()
            {
                CityName = worldCity.CityAscii ?? _localizer.GetString("UnknownLocationLabel"),
                CountryName = worldCity.Country,
                Population = worldCity.Population
            };

            return city;
        }

        public async Task<List<Location>> GetSavedLocations()
        {
            // wow! Raw query works!^^
            ISavedLocationsDatabase database = await SavedLocationsDatabase.Instance;
            var savedLocations = await database.GetItemsWithQueryAsync<LocationWithCityDTO>(RepositoryConstants.LocationWithCityQuery);
            // mapper works! xD
            var locations = _mapper.Map<List<Location>>(savedLocations);
            return locations;
        }

        public async Task SaveLocation(Location location)
        {
            ISavedLocationsDatabase database = await SavedLocationsDatabase.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await database.SaveItemAsync(cityDTO);

            // update FOREIGN KEY
            locationDTO.CityId = cityDTO.Id;
            await database.SaveItemAsync(locationDTO);
        }

        public async Task DeleteLocation(Location location)
        {
            ISavedLocationsDatabase database = await SavedLocationsDatabase.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            // Thanks to ON DELETE CASCADE location will be deleted automatically
            await database.DeleteItemAsync(cityDTO);
        }
    }
}
