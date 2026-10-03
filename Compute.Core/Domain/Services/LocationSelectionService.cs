using Compute.Core.Common.Events;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Repository;

namespace Compute.Core.Domain.Services
{
    /// <summary>
    /// Decides which place every screen computes from.
    ///
    /// Three candidates compete for the slot: a place the user picked, the device's own fix, and
    /// an invented placeholder. A pick always wins; failing that the app fills the slot in with
    /// the fix and, until one arrives, with the placeholder. The ranking is
    /// <see cref="LocationSelection"/>; this class applies it to state that changes over time.
    ///
    /// This used to live inside the GPS service, which made the policy untestable (the service
    /// is part of the MAUI app) and mixed "where is the device" with "where does the user want".
    /// It also relied on every caller being on the main thread without saying so: the
    /// placeholder was created lazily with a non-atomic ??=, and two threads could have made two
    /// of them, after which the identity checks below misfire. State is now guarded by one lock
    /// and the placeholder is created once.
    ///
    /// While the selection follows the device, the position it last had is kept, and a launch
    /// opens on it at once rather than waiting up to half a minute for a fix. The fresh fix that
    /// follows is taken silently if it is in the same town; if it is in another, it is held back
    /// and the reader is asked first, the way Jakdojade asks before changing city.
    /// </summary>
    public sealed class LocationSelectionService : ILocationSelection
    {
        private readonly IDeviceLocationProvider _device;
        private readonly IPermissionGateService _permissions;
        private readonly ILocationService _locations;
        private readonly ILocationSelectionStore _store;
        private readonly TimeProvider _time;

        /// <summary>How long the app has to be away before coming back asks the device again.</summary>
        public static readonly TimeSpan RefreshAfterAway = TimeSpan.FromMinutes(30);

        private readonly Location _placeholder = Location.CreatePlaceholder();
        private readonly WeakEvent<EventArgs> _deviceFixAdopted = new();
        private readonly WeakEvent<CityChangeProposedEventArgs> _cityChangeProposed = new();
        private readonly Lock _gate = new();

        /// <summary>What the user picked or the app filled in; null means "on the placeholder".</summary>
        private Location? _selected;

        /// <summary>The fix standing in for a choice, when the selection is one.</summary>
        private Location? _adoptedFix;

        /// <summary>
        /// The town on screen that the next fresh fix is compared with, and the only one: it is the
        /// town the reader was last shown, which is what makes moving away from it worth a question.
        /// The kept position at launch, or whatever is on screen when the app returns after a while
        /// away. Cleared by the fix it was waiting for, and by anything the user picks.
        /// </summary>
        private Location? _confirmAgainst;

        /// <summary>A fix in another town, held back until the reader answers.</summary>
        private Location? _proposed;

        /// <summary>When a fresh fix was last taken or offered. Null until then: one look per launch.</summary>
        private DateTimeOffset? _refreshedAt;

        private Task<bool>? _restore;
        private int _fillInFlight;

        public LocationSelectionService(
            IDeviceLocationProvider device,
            IPermissionGateService permissions,
            ILocationService locations,
            ILocationSelectionStore store,
            TimeProvider time)
        {
            _device = device;
            _permissions = permissions;
            _locations = locations;
            _store = store;
            _time = time;

            // A method group, not a lambda: the provider may hold its subscribers weakly.
            _device.DeviceLocationChanged += OnDeviceLocationChanged;
        }

        public event EventHandler<EventArgs> DeviceFixAdopted
        {
            add => _deviceFixAdopted.Add(value);
            remove => _deviceFixAdopted.Remove(value);
        }

        public event EventHandler<CityChangeProposedEventArgs> CityChangeProposed
        {
            add => _cityChangeProposed.Add(value);
            remove => _cityChangeProposed.Remove(value);
        }

        public Location SelectedLocation
        {
            get
            {
                lock (_gate)
                {
                    return _selected ?? _placeholder;
                }
            }
        }

        public bool ShouldPromptForLocation
        {
            get
            {
                lock (_gate)
                {
                    return !_store.HasUserChosen && _selected is null;
                }
            }
        }

        public void Select(Location location)
        {
            ArgumentNullException.ThrowIfNull(location);

            lock (_gate)
            {
                // The placeholder can appear as a row when the list would otherwise be empty.
                // Tapping it is not a choice of anywhere, so it clears rather than records one.
                // A pick of any kind settles the question the town on screen was waiting to raise.
                _confirmAgainst = null;

                if (ReferenceEquals(location, _placeholder))
                {
                    _selected = null;
                    _adoptedFix = null;
                    _store.SelectedLocationId = null;
                    return;
                }

                _selected = location;
                // A choice even when it lands on the device's own row: the adoption no longer
                // stands in for one.
                _adoptedFix = null;
                _store.SelectedLocationId = location.IsSaved ? location.Id : null;
                _store.HasUserChosen = true;

                // Picking the device's own row means following the device, so its position is
                // the one to open on next time.
                if (location.IsCurrent)
                {
                    _store.LastDevicePosition = new GeoPoint(location.Latitude, location.Longitude);
                }
            }
        }

        public Task RestorePersistedSelectionAsync()
        {
            // Locked, because checking and starting are two steps: two screens loading at once on
            // a cold start could otherwise both find nothing running and both run the restore.
            lock (_gate)
            {
                // A finished attempt that failed is not remembered. It used to be, for the whole
                // session, and every screen awaiting the restore then failed along with it; now
                // the next load simply tries again. Decided here rather than by the attempt
                // resetting itself, because an attempt that fails before its first real await
                // finishes before it has been stored, and would reset nothing.
                if (_restore is null || (_restore.IsCompletedSuccessfully && !_restore.Result))
                {
                    _restore = RestoreAsync();
                }

                return _restore;
            }
        }

        /// <returns>False when the saved places could not be read, so the attempt is worth repeating.</returns>
        private async Task<bool> RestoreAsync()
        {
            try
            {
                int? wanted;
                lock (_gate)
                {
                    if (IsUserChoice) return true;
                    wanted = _store.SelectedLocationId;
                }

                if (wanted is not int id)
                {
                    await RestoreLastDevicePositionAsync().ConfigureAwait(false);
                    return true;
                }

                var saved = await _locations.GetSavedLocations().ConfigureAwait(false);
                var match = saved.FirstOrDefault(location => location.Id == id);
                if (match is null) return true;

                lock (_gate)
                {
                    // A place chosen while the database was being read is newer than the one on
                    // disk. A stand-in is not, and gives way.
                    if (!IsUserChoice)
                    {
                        _selected = match;
                        _adoptedFix = null;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // Until a read succeeds the app stays on whatever it was already showing.
                return false;
            }
        }

        /// <summary>
        /// Opens on where the device was last time, when the selection follows it. Without this
        /// every launch drew nothing until a fresh fix arrived — up to half a minute indoors, and
        /// the London placeholder when none did.
        /// </summary>
        private async Task RestoreLastDevicePositionAsync()
        {
            if (_store.LastDevicePosition is not GeoPoint kept) return;

            // Named the way a fresh fix is, from the bundled catalogue: no network involved.
            var position = await _locations.GetLocationFromCoordinates(kept.Latitude, kept.Longitude).ConfigureAwait(false);
            position.IsCurrent = true;

            lock (_gate)
            {
                // A fix or a pick that landed while the town was being looked up is newer.
                if (_selected is not null) return;

                _selected = position;
                _adoptedFix = position;
                _confirmAgainst = position;
            }

            // Announced only now, with the selection already on it, so following it moves nothing
            // and no screen reloads for the place it is about to compute anyway. The Locations
            // screen shows it as the device's row until a fresh fix replaces it.
            _device.PublishDeviceLocation(position);
        }

        public async Task RefreshFromDeviceAsync()
        {
            if (!ShouldRefreshFromDevice) return;

            // One request between all the screens that ask: they share this service, and several
            // loads landing together would otherwise each start their own.
            if (Interlocked.Exchange(ref _fillInFlight, 1) == 1) return;

            try
            {
                // Checked, never requested, and awaited on the caller's context: both calls
                // below reach platform APIs that expect the main thread.
                if (!await _permissions.RefreshLocationPermissionState()) return;
                if (_device.IsGettingDeviceLocation) return;

                bool somethingToCompare;
                lock (_gate)
                {
                    somethingToCompare = _confirmAgainst is not null;
                }

                if (!somethingToCompare)
                {
                    // Nothing on screen a fix could carry the reader away from — the placeholder at
                    // most. Taken as it comes, and announced before the request settles, so a
                    // screen waiting on it finds the selection already moved.
                    if ((await _device.GetDeviceGeoLocation()).IsSuccessful)
                    {
                        lock (_gate)
                        {
                            _refreshedAt = _time.GetUtcNow();
                        }
                    }

                    return;
                }

                var fetched = await _device.FetchDeviceGeoLocationAsync();
                if (fetched.IsSuccessful)
                {
                    TakeOrPropose(fetched.Value);
                }
            }
            finally
            {
                // Reset so a later load can try again: permission may be granted by then.
                Volatile.Write(ref _fillInFlight, 0);
            }
        }

        public Task RefreshOnReturnAsync()
        {
            lock (_gate)
            {
                // Only once this launch has had its look, and only after a real absence: a glance
                // at another app and back is not a journey.
                if (_refreshedAt is not { } last || _time.GetUtcNow() - last < RefreshAfterAway) return Task.CompletedTask;
                if (!IsFollowingDevice || _selected is not { IsCurrent: true } onScreen) return Task.CompletedTask;

                _refreshedAt = null;
                _confirmAgainst = onScreen;
            }

            return RefreshFromDeviceAsync();
        }

        /// <summary>The same town is taken silently; another one is held back and offered.</summary>
        private void TakeOrPropose(Location fresh)
        {
            Location? from = null;

            lock (_gate)
            {
                _refreshedAt = _time.GetUtcNow();
                var shown = _confirmAgainst;
                _confirmAgainst = null;

                // Still on the town the question is about, still following the device, and the
                // fix is somewhere else: nothing moves until the reader says so.
                if (shown is not null
                    && ReferenceEquals(_selected, shown)
                    && IsFollowingDevice
                    && LocationSelection.IsDifferentTown(shown, fresh))
                {
                    _proposed = fresh;
                    from = shown;
                }
            }

            if (from is not null)
            {
                _cityChangeProposed.Raise(this, new CityChangeProposedEventArgs(from, fresh));
                return;
            }

            // The same town, or nothing left to ask about: the fix is simply where the device is.
            _device.PublishDeviceLocation(fresh);
        }

        public void AcceptProposedCityChange()
        {
            Location? fresh;
            lock (_gate)
            {
                fresh = _proposed;
                _proposed = null;
            }

            // Announced, and the selection follows it the way it follows any fix — unless the
            // user has picked a place of their own in the meantime, which it then leaves alone.
            if (fresh is not null)
            {
                _device.PublishDeviceLocation(fresh);
            }
        }

        public void DeclineProposedCityChange()
        {
            // Neither announced nor kept: the town on screen stays the device's position for the
            // rest of this launch, and the next launch opens on it and asks again.
            lock (_gate)
            {
                _proposed = null;
            }
        }

        private void OnDeviceLocationChanged(object? sender, DeviceLocationChangedEventArgs e)
        {
            bool moved;

            lock (_gate)
            {
                if (!LocationSelection.ShouldAdoptDeviceFix(_selected, _placeholder, _adoptedFix, e.Previous))
                {
                    return;
                }

                // Following the fix onto a new object keeps a user's choice a user's choice.
                // Only a stand-in becomes the new stand-in.
                bool wasUserChoice = IsUserChoice;

                moved = !ReferenceEquals(_selected, e.Current);
                _selected = e.Current;
                if (!wasUserChoice) _adoptedFix = e.Current;

                // Kept for the next launch to open on.
                _store.LastDevicePosition = new GeoPoint(e.Current.Latitude, e.Current.Longitude);

                // Any fix but the kept position itself answers what that position was waiting to ask.
                if (!ReferenceEquals(e.Current, _confirmAgainst))
                {
                    _confirmAgainst = null;
                    _refreshedAt = _time.GetUtcNow();
                }
            }

            // Outside the lock: a handler reads SelectedLocation straight back.
            if (moved)
            {
                _deviceFixAdopted.Raise(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Following the device and not yet refreshed this launch. Following means no saved place
        /// is the choice: nothing picked, or the device's own row picked. The second used to be
        /// missed, so someone who had once tapped their own position opened on London ever after.
        /// </summary>
        private bool ShouldRefreshFromDevice
        {
            get
            {
                lock (_gate)
                {
                    return _refreshedAt is null && IsFollowingDevice;
                }
            }
        }

        /// <summary>Must be read under <see cref="_gate"/>.</summary>
        private bool IsFollowingDevice =>
            _store.SelectedLocationId is null && (_selected is null || _selected.IsCurrent);

        /// <summary>Must be read under <see cref="_gate"/>.</summary>
        private bool IsUserChoice => LocationSelection.IsUserChoice(_selected, _placeholder, _adoptedFix);
    }
}
