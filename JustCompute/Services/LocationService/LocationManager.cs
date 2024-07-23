﻿using Compute.Core.Domain.Entities.Models;
using JustCompute.Persistance.Repository.Models;
using JustCompute.Persistance.Repository;
using DeviceGeoLocation = Microsoft.Maui.Devices.Sensors.Location;
using Location = Compute.Core.Domain.Entities.Models.Location;
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
    public class LocationManager(IMapper mapper) : ILocationManager
    {
        private readonly WeakEventManager _eventManager = new();
        private readonly IMapper _mapper = mapper;

        private CancellationTokenSource? _cancelTokenSource;

        public TaskCompletionSource<bool>? GettingDeviceLocationFinished { get; private set; }
        public bool IsGettingDeviceLocation { get; private set; }

        private Location? _deviceLocation;
        public Location? DeviceLocation
        {
            get => _deviceLocation;
            private set
            {
                if (_deviceLocation != value)
                {
                    _deviceLocation = value;
                    if (_deviceLocation != null)
                    {
                        // Notify subscribers
                        OnDeviceLocationChanged();
                    }
                }
            }
        }

        public Location? SelectedLocation { get; set; }

        public event EventHandler<EventArgs> DeviceLocationChanged
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public async Task<Result<Location, FaultCode>> GetDeviceGeoLocation()
        {
            try
            {
                GettingDeviceLocationFinished = new TaskCompletionSource<bool>();

                IsGettingDeviceLocation = true;

                GeolocationRequest request = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30));

                _cancelTokenSource = new CancellationTokenSource();

                DeviceGeoLocation? deviceLocation = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token)
                    ?? throw new DeviceLocationUnavailableException("await Geolocation.Default.GetLocationAsync returned null");

                DeviceLocation = await GetLocationModel(deviceLocation.Latitude, deviceLocation.Longitude);

                return DeviceLocation;
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

        public async void OnStartListeningDeciveGeoLocation()
        {
            try
            {
                Geolocation.LocationChanged += Geolocation_LocationChanged;
                var request = new GeolocationListeningRequest((GeolocationAccuracy.Best));
                var success = await Geolocation.StartListeningForegroundAsync(request);
            }
            catch (Exception ex)
            {
                // Unable to start listening for location changes
            }
        }

        public void OnStopListeningDeciveLocation()
        {
            try
            {
                Geolocation.LocationChanged -= Geolocation_LocationChanged;
                Geolocation.StopListeningForeground();
            }
            catch (Exception ex)
            {
                // Unable to stop listening for location changes
            }
        }

        private void OnDeviceLocationChanged()
        {
            _eventManager.HandleEvent(this, EventArgs.Empty, nameof(DeviceLocationChanged));
        }

        private async void Geolocation_LocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            DeviceLocation = await GetLocationModel(e.Location.Latitude, e.Location.Longitude);
        }

        private async Task<Location> GetLocationModel(double latitude, double longitude, int locationId = ILocationManager.DeviceLocationId)
        {
            var city = await GetCity(latitude, longitude);
            Location location = new()
            {
                Id = locationId,
                Latitude = latitude.ToString(),
                Longitude = longitude.ToString(),
                City = city,
            };
            return location;
        }

        private async Task<City> GetCity(double latitude, double longitude)
        {
            IWorldCitiesDatabase<WorldCityTable> database = await WorldCitiesDatabase.Instance;

            var worldCity = await database.GetTheNearestCityAsync(latitude, longitude);

            return _mapper.Map<City>(worldCity);
        }

        public async Task<List<Location>> GetSavedLocations()
        {
            // Raw query also works
            ISavedLocationsDatabase database = await SavedLocationsDatabase.Instance;
            var savedLocations = await database.GetItemsWithQueryAsync<LocationWithCityDTO>(RepositoryConstants.LocationWithCityQuery);
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

        public async Task UpdateLocation(Location location)
        {
            ISavedLocationsDatabase database = await SavedLocationsDatabase.Instance;

            var cityDTO = _mapper.Map<CityTable>(location);
            var locationDTO = _mapper.Map<LocationTable>(location);

            await database.UpdateItemAsync(cityDTO);

            // update FOREIGN KEY
            locationDTO.CityId = cityDTO.Id;
            await database.UpdateItemAsync(locationDTO);
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
