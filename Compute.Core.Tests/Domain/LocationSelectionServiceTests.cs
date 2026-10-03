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

        private static readonly GeoPoint Krakow = new(50.0647, 19.9450);
        private static readonly GeoPoint Warsaw = new(52.2297, 21.0122);

        /// <summary>A fix in a named town, the way the GPS service names one: from the catalogue.</summary>
        private static Location FixIn(string town, double latitude, double longitude) =>
            new()
            {
                Name = town,
                Latitude = latitude,
                Longitude = longitude,
                IsCurrent = true,
                City = new City { CityName = town, CountryName = "Poland" },
            };

        private sealed class Harness
        {
            public FakeDevice Device { get; } = new();
            public FakePermissions Permissions { get; } = new();
            public FakeLocations Locations { get; } = new();
            public MemoryStore Store { get; }
            public FakeTime Time { get; } = new();
            public LocationSelectionService Service { get; }
            public int Adoptions { get; private set; }
            public List<CityChangeProposedEventArgs> Proposals { get; } = [];

            /// <param name="store">What an earlier launch left behind, for a test of the next one.</param>
            public Harness(MemoryStore? store = null)
            {
                Store = store ?? new MemoryStore();
                Service = new LocationSelectionService(Device, Permissions, Locations, Store, Time);
                Service.DeviceFixAdopted += OnAdopted;
                Service.CityChangeProposed += OnProposed;
            }

            private void OnAdopted(object? sender, EventArgs e) => Adoptions++;

            private void OnProposed(object? sender, CityChangeProposedEventArgs e) => Proposals.Add(e);
        }

        /// <summary>A launch that opens on Kraków, kept from the one before.</summary>
        private static async Task<Harness> LaunchedOnKrakow()
        {
            var h = new Harness();
            h.Store.LastDevicePosition = Krakow;
            await h.Service.RestorePersistedSelectionAsync();
            return h;
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
        public async Task RefreshingFromTheDevice_AsksOnlyWhileFollowingItAndPermissionExists()
        {
            var denied = new Harness();
            denied.Permissions.Granted = false;
            await denied.Service.RefreshFromDeviceAsync();
            Assert.Equal(0, denied.Device.Requests);

            var chosen = new Harness();
            chosen.Service.Select(Saved(3));
            await chosen.Service.RefreshFromDeviceAsync();
            Assert.Equal(0, chosen.Device.Requests);

            var fresh = new Harness();
            await fresh.Service.RefreshFromDeviceAsync();
            Assert.Equal(1, fresh.Device.Requests);
            Assert.Equal("Paris", fresh.Service.SelectedLocation.Name);
        }

        [Fact]
        public async Task ScreensAskingTogetherShareOneRequest()
        {
            var h = new Harness();
            var gate = new TaskCompletionSource<bool>();
            h.Permissions.Gate = gate.Task;

            var calls = Enumerable.Range(0, 5).Select(_ => h.Service.RefreshFromDeviceAsync()).ToArray();
            gate.SetResult(true);
            await Task.WhenAll(calls);

            Assert.Equal(1, h.Device.Requests);
        }

        // ---- opening on the kept position, and asking before changing town ------------------

        [Fact]
        public async Task ALaunchOpensOnTheKeptPosition_AtOnce_WithoutAReload()
        {
            var h = await LaunchedOnKrakow();

            var opened = h.Service.SelectedLocation;
            Assert.Equal("Kraków", opened.Name);
            Assert.True(opened.IsCurrent);
            Assert.Same(opened, h.Device.DeviceLocation);
            Assert.False(h.Service.ShouldPromptForLocation);
            Assert.Equal(0, h.Device.Requests);
            Assert.Equal(0, h.Adoptions);
        }

        [Fact]
        public async Task AKeptPositionDoesNotDisplaceAChosenPlace()
        {
            var h = new Harness();
            h.Locations.Saved.Add(Saved(7));
            h.Store.SelectedLocationId = 7;
            h.Store.HasUserChosen = true;
            h.Store.LastDevicePosition = Krakow;

            await h.Service.RestorePersistedSelectionAsync();

            Assert.Equal(7, h.Service.SelectedLocation.Id);
            Assert.Null(h.Device.DeviceLocation);
        }

        [Fact]
        public async Task AFreshFixInTheSameTownIsTakenSilently()
        {
            var h = await LaunchedOnKrakow();
            var fresh = FixIn("Kraków", 50.0800, 19.9300);
            h.Device.NextFix = fresh;

            await h.Service.RefreshFromDeviceAsync();

            Assert.Empty(h.Proposals);
            Assert.Same(fresh, h.Service.SelectedLocation);
            Assert.Equal(1, h.Adoptions);
            Assert.Equal(new GeoPoint(50.0800, 19.9300), h.Store.LastDevicePosition);
        }

        [Fact]
        public async Task AFreshFixInAnotherTownIsAskedAbout_AndNothingMovesBeforeTheAnswer()
        {
            var h = await LaunchedOnKrakow();
            var krakow = h.Service.SelectedLocation;
            var warsaw = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            h.Device.NextFix = warsaw;

            await h.Service.RefreshFromDeviceAsync();

            var asked = Assert.Single(h.Proposals);
            Assert.Same(krakow, asked.From);
            Assert.Same(warsaw, asked.To);
            Assert.Same(krakow, h.Service.SelectedLocation);
            Assert.Same(krakow, h.Device.DeviceLocation);
            Assert.Equal(Krakow, h.Store.LastDevicePosition);
            Assert.Equal(0, h.Adoptions);
        }

        [Fact]
        public async Task SwitchingMovesToTheNewTown_AndKeepsItForNextTime()
        {
            var h = await LaunchedOnKrakow();
            var warsaw = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            h.Device.NextFix = warsaw;
            await h.Service.RefreshFromDeviceAsync();

            h.Service.AcceptProposedCityChange();

            Assert.Same(warsaw, h.Service.SelectedLocation);
            Assert.Same(warsaw, h.Device.DeviceLocation);
            Assert.Equal(Warsaw, h.Store.LastDevicePosition);
            Assert.Equal(1, h.Adoptions);
        }

        [Fact]
        public async Task KeepingTheOldTownLastsForThisLaunch_AndTheNextLaunchAsksAgain()
        {
            var h = await LaunchedOnKrakow();
            h.Device.NextFix = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            await h.Service.RefreshFromDeviceAsync();

            h.Service.DeclineProposedCityChange();

            Assert.Equal("Kraków", h.Service.SelectedLocation.Name);
            Assert.Equal(Krakow, h.Store.LastDevicePosition);

            // Another screen loading later in the same launch does not ask again.
            await h.Service.RefreshFromDeviceAsync();
            Assert.Single(h.Proposals);
            Assert.Equal(1, h.Device.Requests);

            var next = new Harness(h.Store);
            next.Device.NextFix = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            await next.Service.RestorePersistedSelectionAsync();
            await next.Service.RefreshFromDeviceAsync();

            Assert.Equal("Kraków", next.Service.SelectedLocation.Name);
            Assert.Single(next.Proposals);
        }

        [Fact]
        public async Task AFirstLaunchHasNothingToAskAbout()
        {
            var h = new Harness();
            var warsaw = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            h.Device.NextFix = warsaw;

            await h.Service.RestorePersistedSelectionAsync();
            await h.Service.RefreshFromDeviceAsync();

            Assert.Empty(h.Proposals);
            Assert.Same(warsaw, h.Service.SelectedLocation);
            Assert.Equal(Warsaw, h.Store.LastDevicePosition);
        }

        [Fact]
        public async Task ANeighbouringTownCloserThanFiveKilometresIsNotAMove()
        {
            // The nearest town flips at a town's edge on a few hundred metres of network fix.
            var h = await LaunchedOnKrakow();
            var nextDoor = FixIn("Zielonki", Krakow.Latitude + 0.027, Krakow.Longitude);
            h.Device.NextFix = nextDoor;

            await h.Service.RefreshFromDeviceAsync();

            Assert.Empty(h.Proposals);
            Assert.Same(nextDoor, h.Service.SelectedLocation);
        }

        [Fact]
        public async Task APlaceChosenBeforeTheAnswerIsLeftAlone()
        {
            var h = await LaunchedOnKrakow();
            h.Device.NextFix = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);
            await h.Service.RefreshFromDeviceAsync();

            h.Service.Select(Saved(7, "Gdańsk"));
            h.Service.AcceptProposedCityChange();

            Assert.Equal(7, h.Service.SelectedLocation.Id);
        }

        [Fact]
        public async Task HavingPickedTheDevicesOwnRow_LaterLaunchesStillFollowTheDevice()
        {
            // It used to open on London ever after: a pick, so no fill, and no saved place to restore.
            var first = new Harness();
            var row = FixIn("Kraków", Krakow.Latitude, Krakow.Longitude);
            first.Device.Arrive(row);
            first.Service.Select(row);
            Assert.True(first.Store.HasUserChosen);
            Assert.Null(first.Store.SelectedLocationId);

            var next = new Harness(first.Store);
            next.Device.NextFix = FixIn("Kraków", 50.0700, 19.9400);
            await next.Service.RestorePersistedSelectionAsync();
            Assert.Equal("Kraków", next.Service.SelectedLocation.Name);

            await next.Service.RefreshFromDeviceAsync();
            Assert.Equal(1, next.Device.Requests);
        }

        [Fact]
        public async Task ComingBackAfterHalfAnHourAsksAgain_ButAGlanceAwayDoesNot()
        {
            var h = await LaunchedOnKrakow();
            h.Device.NextFix = FixIn("Kraków", 50.0700, 19.9400);
            await h.Service.RefreshFromDeviceAsync();
            h.Device.NextFix = FixIn("Warsaw", Warsaw.Latitude, Warsaw.Longitude);

            h.Time.Advance(TimeSpan.FromMinutes(10));
            await h.Service.RefreshOnReturnAsync();
            Assert.Empty(h.Proposals);
            Assert.Equal(1, h.Device.Requests);

            h.Time.Advance(TimeSpan.FromMinutes(25));
            await h.Service.RefreshOnReturnAsync();
            var asked = Assert.Single(h.Proposals);
            Assert.Equal("Kraków", asked.From.Name);
            Assert.Equal("Warsaw", asked.To.Name);
            Assert.Equal(2, h.Device.Requests);
        }

        // ---- fakes -------------------------------------------------------------------------

        private sealed class FakeDevice : IDeviceLocationProvider
        {
            public Location? DeviceLocation { get; private set; }
            public bool IsGettingDeviceLocation => false;
            public int Requests { get; private set; }

            /// <summary>What the next request answers with; Paris when unset.</summary>
            public Location? NextFix { get; set; }

            public event EventHandler<DeviceLocationChangedEventArgs>? DeviceLocationChanged;

            public Task WaitForPendingFixAsync() => Task.CompletedTask;

            public Task<Result<Location, FaultCode>> GetDeviceGeoLocation()
            {
                var fix = Take();
                Arrive(fix);
                return Task.FromResult<Result<Location, FaultCode>>(fix);
            }

            public Task<Result<Location, FaultCode>> FetchDeviceGeoLocationAsync() =>
                Task.FromResult<Result<Location, FaultCode>>(Take());

            public void PublishDeviceLocation(Location position) => Replace(position);

            private Location Take()
            {
                Requests++;
                return NextFix ?? Fix();
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

            /// <summary>Named after the nearest of a few towns, as the catalogue would name it.</summary>
            public Task<Location> GetLocationFromCoordinates(double latitude, double longitude, int locationId = -1)
            {
                var here = new GeoPoint(latitude, longitude);
                var (name, _) = new[] { ("Kraków", Krakow), ("Warsaw", Warsaw), ("Paris", new GeoPoint(48.8566, 2.3522)) }
                    .MinBy(town => town.Item2.DistanceMetersTo(here));

                return Task.FromResult(new Location
                {
                    Id = locationId,
                    Name = name,
                    Latitude = latitude,
                    Longitude = longitude,
                    City = new City { CityName = name, CountryName = "Poland" },
                });
            }
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
            public GeoPoint? LastDevicePosition { get; set; }
        }

        private sealed class FakeTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 10, 3, 9, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by) => _now += by;
        }
    }
}
