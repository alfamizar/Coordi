using Compute.Core.Common.Results;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Errors;
using Compute.Core.Domain.Services;
using Compute.Core.Repository;

namespace Compute.Core.Tests.Domain
{
    /// <summary>
    /// The policy that decides what every screen computes from. It lived inside the MAUI GPS
    /// service until now, where nothing could test it; these pin the behaviour it had there.
    /// </summary>
    public class LocationSelectionServiceTests
    {
        private static Location Fix(string name = "Paris") =>
            new() { Name = name, Latitude = 48.8566, Longitude = 2.3522, IsCurrent = true };

        private static Location Saved(int id, string name = "Warsaw") =>
            new() { Id = id, Name = name, Latitude = 52.2297, Longitude = 21.0122 };

        private sealed class Harness
        {
            public FakeDevice Device { get; } = new();
            public FakePermissions Permissions { get; } = new();
            public FakeLocations Locations { get; } = new();
            public MemoryStore Store { get; } = new();
            public LocationSelectionService Service { get; }
            public int Adoptions { get; private set; }

            public Harness()
            {
                Service = new LocationSelectionService(Device, Permissions, Locations, Store);
                Service.DeviceFixAdopted += OnAdopted;
            }

            private void OnAdopted(object? sender, EventArgs e) => Adoptions++;
        }

        [Fact]
        public void AFreshInstallComputesFromThePlaceholderAndSaysSo()
        {
            var h = new Harness();

            Assert.Equal("London", h.Service.SelectedLocation.Name);
            Assert.False(h.Service.SelectedLocation.IsSaved);
            Assert.True(h.Service.ShouldPromptForLocation);
        }

        [Fact]
        public void TheSamePlaceholderComesBackEveryTime()
        {
            // The identity checks depend on there being exactly one.
            var h = new Harness();

            Assert.Same(h.Service.SelectedLocation, h.Service.SelectedLocation);
        }

        [Fact]
        public void AnArrivingFixIsAdopted_WithoutBeingRecordedAsAChoice()
        {
            var h = new Harness();
            var fix = Fix();

            h.Device.Arrive(fix);

            Assert.Same(fix, h.Service.SelectedLocation);
            Assert.False(h.Service.ShouldPromptForLocation);
            Assert.False(h.Store.HasUserChosen);
            Assert.Null(h.Store.SelectedLocationId);
            Assert.Equal(1, h.Adoptions);
        }

        [Fact]
        public void AChoiceIsRecorded_AndAFixDoesNotDisplaceIt()
        {
            var h = new Harness();
            var warsaw = Saved(7);

            h.Service.Select(warsaw);
            h.Device.Arrive(Fix());

            Assert.Same(warsaw, h.Service.SelectedLocation);
            Assert.True(h.Store.HasUserChosen);
            Assert.Equal(7, h.Store.SelectedLocationId);
            Assert.Equal(0, h.Adoptions);
        }

        [Fact]
        public void ChoosingThePlaceholderRowClearsTheChoiceRatherThanRecordingIt()
        {
            var h = new Harness();
            var placeholder = h.Service.SelectedLocation;   // before anything is chosen, this is it
            h.Service.Select(Saved(7));

            h.Service.Select(placeholder);

            Assert.Same(placeholder, h.Service.SelectedLocation);
            Assert.Null(h.Store.SelectedLocationId);
        }

        [Fact]
        public async Task ThePersistedChoiceIsRestored_AndBeatsAFixThatArrivedFirst()
        {
            var h = new Harness();
            var warsaw = Saved(7);
            h.Locations.Saved.Add(warsaw);
            h.Store.SelectedLocationId = 7;
            h.Store.HasUserChosen = true;

            h.Device.Arrive(Fix());
            await h.Service.RestorePersistedSelectionAsync();

            Assert.Same(warsaw, h.Service.SelectedLocation);
        }

        [Fact]
        public async Task ARestoreDoesNotOverwriteAChoiceMadeWhileItWasReading()
        {
            var h = new Harness();
            h.Locations.Saved.Add(Saved(7));
            h.Store.SelectedLocationId = 7;
            var gate = new TaskCompletionSource();
            h.Locations.Gate = gate.Task;

            var restore = h.Service.RestorePersistedSelectionAsync();
            var berlin = Saved(9, "Berlin");
            h.Service.Select(berlin);
            gate.SetResult();
            await restore;

            Assert.Same(berlin, h.Service.SelectedLocation);
        }

        [Fact]
        public async Task AFailedRestoreDoesNotThrow_AndIsTriedAgainNextTime()
        {
            var h = new Harness();
            h.Store.SelectedLocationId = 7;
            h.Locations.Throw = true;

            await h.Service.RestorePersistedSelectionAsync();
            Assert.Equal("London", h.Service.SelectedLocation.Name);

            h.Locations.Throw = false;
            h.Locations.Saved.Add(Saved(7));
            await h.Service.RestorePersistedSelectionAsync();

            Assert.Equal(7, h.Service.SelectedLocation.Id);
            Assert.Equal(2, h.Locations.Reads);
        }

        [Fact]
        public async Task RestoreRunsOnceWhenItSucceeds()
        {
            var h = new Harness();
            h.Store.SelectedLocationId = 7;
            h.Locations.Saved.Add(Saved(7));

            await Task.WhenAll(
                h.Service.RestorePersistedSelectionAsync(),
                h.Service.RestorePersistedSelectionAsync(),
                h.Service.RestorePersistedSelectionAsync());
            await h.Service.RestorePersistedSelectionAsync();

            Assert.Equal(1, h.Locations.Reads);
        }

        [Fact]
        public void ADeviceRowTheUserChoseFollowsTheFixOntoTheListsObject_AndStaysTheirChoice()
        {
            var h = new Harness();
            var fresh = Fix();
            h.Device.Arrive(fresh);
            h.Service.Select(fresh);

            // The Locations screen moves the fix onto the row it already shows.
            var row = Fix();
            h.Device.Replace(row);

            Assert.Same(row, h.Service.SelectedLocation);
            Assert.True(h.Store.HasUserChosen);
        }

        [Fact]
        public async Task FillingFromTheDevice_AsksOnlyWhenNothingIsChosenAndPermissionExists()
        {
            var denied = new Harness();
            denied.Permissions.Granted = false;
            await denied.Service.FillFromDeviceIfUnchosenAsync();
            Assert.Equal(0, denied.Device.Requests);

            var chosen = new Harness();
            chosen.Service.Select(Saved(3));
            await chosen.Service.FillFromDeviceIfUnchosenAsync();
            Assert.Equal(0, chosen.Device.Requests);

            var fresh = new Harness();
            await fresh.Service.FillFromDeviceIfUnchosenAsync();
            Assert.Equal(1, fresh.Device.Requests);
            Assert.Equal("Paris", fresh.Service.SelectedLocation.Name);
        }

        [Fact]
        public async Task ScreensAskingTogetherShareOneRequest()
        {
            var h = new Harness();
            var gate = new TaskCompletionSource<bool>();
            h.Permissions.Gate = gate.Task;

            var calls = Enumerable.Range(0, 5).Select(_ => h.Service.FillFromDeviceIfUnchosenAsync()).ToArray();
            gate.SetResult(true);
            await Task.WhenAll(calls);

            Assert.Equal(1, h.Device.Requests);
        }

        // ---- fakes -------------------------------------------------------------------------

        private sealed class FakeDevice : IDeviceLocationProvider
        {
            public Location? DeviceLocation { get; private set; }
            public bool IsGettingDeviceLocation => false;
            public int Requests { get; private set; }
            public event EventHandler<DeviceLocationChangedEventArgs>? DeviceLocationChanged;

            public Task WaitForPendingFixAsync() => Task.CompletedTask;

            public Task<Result<Location, FaultCode>> GetDeviceGeoLocation()
            {
                Requests++;
                var fix = Fix();
                Arrive(fix);
                return Task.FromResult<Result<Location, FaultCode>>(fix);
            }

            public void KeepListInstance(Location listInstance) => Replace(listInstance);

            public void Arrive(Location fix) => Replace(fix);

            public void Replace(Location next)
            {
                var previous = DeviceLocation;
                DeviceLocation = next;
                DeviceLocationChanged?.Invoke(this, new DeviceLocationChangedEventArgs(previous, next));
            }
        }

        private sealed class FakePermissions : IPermissionGateService
        {
            public bool Granted { get; set; } = true;
            public Task<bool>? Gate { get; set; }
            public bool? LastKnownLocationPermissionGranted => Granted;
            public event EventHandler<bool>? LocationPermissionStateChanged { add { } remove { } }
            public Task<bool> RefreshLocationPermissionState() => Gate ?? Task.FromResult(Granted);
        }

        private sealed class FakeLocations : ILocationService
        {
            public List<Location> Saved { get; } = [];
            public bool Throw { get; set; }
            public Task? Gate { get; set; }
            public int Reads { get; private set; }

            public async Task<List<Location>> GetSavedLocations()
            {
                Reads++;
                if (Gate is not null) await Gate;
                if (Throw) throw new InvalidOperationException("database is locked");
                return [.. Saved];
            }

            public Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1) => throw new NotSupportedException();
            public Task<City> GetTheClosestCityToCoordinates(double latitude, double longitude) => throw new NotSupportedException();
            public Task SaveLocation(Location location) => throw new NotSupportedException();
            public Task UpdateLocation(Location location) => throw new NotSupportedException();
            public Task DeleteLocation(Location location) => throw new NotSupportedException();
            public Task<List<Location>> SearchLocations(string searchParam) => throw new NotSupportedException();
        }

        private sealed class MemoryStore : ILocationSelectionStore
        {
            public int? SelectedLocationId { get; set; }
            public bool HasUserChosen { get; set; }
        }
    }
}
