using Compute.Core.Domain.Entities.Models;
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
using Compute.Core.Domain.Services;
using Compute.Core.Common.Exceptions.Location;

namespace JustCompute.Services.LocationService
{
    public class LocationManager : ILocationManager
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IMapper _mapper;
        private Timer? _timer;

        private CancellationTokenSource? _cancelTokenSource;

        public TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; private set; }
        public bool IsGettingDeviceLocation { get; private set; }
        public Location? DeviceLocation { get; private set; }
        public Location? SelectedLocation { get; set; }

        public LocationManager(IStringLocalizer<AppStringsRes> localizer, IMapper mapper)
        {
            _localizer = localizer;
            _mapper = mapper;
            StartTimer();
        }

        private void StartTimer()
        {
            _timer = new(
                NullifyDeviceLocation,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(2)
            );
        }

        private void NullifyDeviceLocation(object? state)
        {
            DeviceLocation = null;
        }

        public async Task<Result<Location, FaultCode>> GetDeviceLocation()
        {
            try
            {
                GettingDeviceLocationFinished = new TaskCompletionSource<bool>();

                IsGettingDeviceLocation = true;

                GeolocationRequest request = new(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceLocation? deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token)
                    ?? throw new DeviceLocationUnavailableException("await Geolocation.Default.GetLocationAsync returned null");
                var timeZoneInfo = TimeZoneInfo.Local;
                var offset = timeZoneInfo.GetUtcOffset(DateTime.Now).Hours;

                var city = await GetCity(deviceLocation.Latitude, deviceLocation.Longitude);
                Location location = new()
                {
                    Latitude = deviceLocation.Latitude.ToString(),
                    Longitude = deviceLocation.Longitude.ToString(),
                    City = city,
                };

                DeviceLocation = location;

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
            catch (DeviceLocationUnavailableException)
            {
                return new(FaultCode.DeviceLocationUnavailable);
            }
            catch (Exception)
            {
                return new(FaultCode.GenericGetLocationException);
            }
            finally
            {
                IsGettingDeviceLocation = false;
                GettingDeviceLocationFinished?.TrySetResult(true);
            }
        }

        public void CancelRequest()
        {
            if (IsGettingDeviceLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
                _cancelTokenSource.Cancel();
        }

        private async Task<City> GetCity(double latitude, double longitude)
        {
            IWorldCitiesDatabase<WorldCityTable> database = await WorldCitiesDatabase.Instance;

            var worldCity = await database.GetTheNearestCityAsync(latitude, longitude);
            City city = new()
            {
                CityName = worldCity?.CityAscii ?? _localizer.GetString("UnknownLocationLabel"),
                CountryName = worldCity?.Country ?? _localizer.GetString("UnknownLocationLabel"),
                Population = worldCity?.Population ?? 0
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

        public async Task<List<Location>> GetLocationsByCity(string searchParam)
        {
            IWorldCitiesDatabase<WorldCityTable> database = await WorldCitiesDatabase.Instance;

            var locations = await database.FilterByCity(searchParam);
            return _mapper.Map<List<Location>>(locations);
        }
    }
}
