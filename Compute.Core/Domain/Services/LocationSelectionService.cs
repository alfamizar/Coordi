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
    /// </summary>
    public sealed class LocationSelectionService : ILocationSelection
    {
        private readonly IDeviceLocationProvider _device;
        private readonly IPermissionGateService _permissions;
        private readonly ILocationService _locations;
        private readonly ILocationSelectionStore _store;

        private readonly Location _placeholder = Location.CreatePlaceholder();
        private readonly WeakEvent<EventArgs> _deviceFixAdopted = new();
        private readonly Lock _gate = new();

        /// <summary>What the user picked or the app filled in; null means "on the placeholder".</summary>
        private Location? _selected;

        /// <summary>The fix standing in for a choice, when the selection is one.</summary>
        private Location? _adoptedFix;

        private Task<bool>? _restore;
        private int _fillInFlight;

        public LocationSelectionService(
            IDeviceLocationProvider device,
            IPermissionGateService permissions,
            ILocationService locations,
            ILocationSelectionStore store)
        {
            _device = device;
            _permissions = permissions;
            _locations = locations;
            _store = store;

            // A method group, not a lambda: the provider may hold its subscribers weakly.
            _device.DeviceLocationChanged += OnDeviceLocationChanged;
        }

        public event EventHandler<EventArgs> DeviceFixAdopted
        {
            add => _deviceFixAdopted.Add(value);
            remove => _deviceFixAdopted.Remove(value);
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

                if (wanted is not int id) return true;

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

        public async Task FillFromDeviceIfUnchosenAsync()
        {
            if (!ShouldPromptForLocation) return;

            // One request between all the screens that ask: they share this service, and several
            // first loads landing together would otherwise each start their own.
            if (Interlocked.Exchange(ref _fillInFlight, 1) == 1) return;

            try
            {
                // Checked, never requested, and awaited on the caller's context: both calls
                // below reach platform APIs that expect the main thread.
                if (!await _permissions.RefreshLocationPermissionState()) return;
                if (_device.IsGettingDeviceLocation) return;

                await _device.GetDeviceGeoLocation();
            }
            finally
            {
                // Reset so a later load can try again: permission may be granted by then.
                Volatile.Write(ref _fillInFlight, 0);
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
            }

            // Outside the lock: a handler reads SelectedLocation straight back.
            if (moved)
            {
                _deviceFixAdopted.Raise(this, EventArgs.Empty);
            }
        }

        /// <summary>Must be read under <see cref="_gate"/>.</summary>
        private bool IsUserChoice => LocationSelection.IsUserChoice(_selected, _placeholder, _adoptedFix);
    }
}
