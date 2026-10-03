using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Shared.Helpers;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Services;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;


namespace JustCompute.Shared.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly INavigationService _navigationService;

        /// <summary>The place every screen computes from.</summary>
        protected readonly ILocationSelection _selection;

        /// <summary>Where the device is, when a screen wants that rather than the selection.</summary>
        protected readonly IDeviceLocationProvider _device;

        public static readonly int TotalNumberOfDaysInTheCurrentYear = DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366 : 365;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string title = string.Empty;

        protected BaseViewModel(ViewModelServices services)
        {
            _navigationService = services.Navigation;
            _selection = services.Selection;
            _device = services.Device;

            // Only screens that load location data have anything to redo. A method group, not a
            // lambda: the event holds its subscribers weakly.
            if (this is ICompute)
            {
                _selection.DeviceFixAdopted += OnDeviceFixAdopted;
            }
        }

        /// <summary>
        /// The place the data on screen was computed for. Kept so a selection that changes
        /// underneath a loaded screen can be noticed.
        /// </summary>
        private Location? _loadedFor;

        private void OnDeviceFixAdopted(object? sender, EventArgs e)
        {
            // The app moved to the device's fix because nothing was chosen. Screens read the
            // selection once per load, so without this they would go on showing the placeholder's
            // answers for a location the app has already left behind.
            if (!_hasLoadedOnce) return;
            if (ReferenceEquals(_loadedFor, _selection.SelectedLocation)) return;

            LoadItems().Forget(GetType().Name);
        }

        /// <summary>
        /// How many loads are in flight. Stepping the date fires a fresh load without waiting
        /// for the previous one, so several overlap; the spinner belongs to all of them, and
        /// only the last to finish may put it away.
        /// </summary>
        private int _loadsInFlight;

        protected virtual async Task LoadItems()
        {
            _hasLoadedOnce = true;
            Interlocked.Increment(ref _loadsInFlight);
            await MainThread.InvokeOnMainThreadAsync(() => IsBusy = true);

            // Every exit resets it, including the ones nobody plans for. Without this a throw
            // anywhere below — a database read losing a race, a screen's own GetData failing —
            // left IsBusy stuck true, and since BasePage swallows the exception the only
            // symptom was a spinner turning forever over an empty screen.
            try
            {
                // Whichever screen loads first must not read the placeholder before the user's
                // own choice has been read back from storage. Restoring runs once per launch.
                await _selection.RestorePersistedSelectionAsync();

                // Unawaited: the platform can sit on this for up to 30 seconds. When a fix is
                // taken it becomes the selection, and DeviceFixAdopted brings this screen back to
                // reload; when it is in another town the reader is asked first.
                _selection.RefreshFromDeviceAsync().Forget(nameof(ILocationSelection.RefreshFromDeviceAsync));

                // Waited on only while there is nothing better than the placeholder to draw: a
                // first launch, or one with no position kept from the last. Every other launch
                // draws the kept position at once — it used to wait for the fix every time, and
                // showed an empty screen for as long as the GPS took.
                if (LocationIdentity.IsPlaceholder(_selection.SelectedLocation))
                {
                    await _device.WaitForPendingFixAsync();
                }

                var location = _selection.SelectedLocation;
                _loadedFor = location;

                // Started on the UI thread on purpose. Everything above can hand back on a
                // background thread — a database read, or the device-fix task completing on the
                // platform's own callback thread — and a GetData begun there resumes there too,
                // so every property it sets would be raised off the UI thread and silently fail
                // to reach the views. Starting here means its continuations come back here.
                await MainThread.InvokeOnMainThreadAsync(() => GetData(location));
            }
            finally
            {
                // Not unconditionally: whoever finishes first would otherwise clear the spinner
                // while slower loads are still running, leaving the screen looking settled over
                // data that is still arriving.
                if (Interlocked.Decrement(ref _loadsInFlight) == 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
                }
            }
        }

        /// <summary>
        /// Takes the location itself rather than a coordinate triple: a UTC offset is a function
        /// of the date, so each screen has to ask for the one that matches what it is showing.
        /// </summary>
        protected virtual Task GetData(Location location)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Loads the screen's data the first time it is shown.
        ///
        /// Loading is otherwise driven only by <see cref="OnNavigatedToAsync"/>, and Shell raises
        /// no navigation event for the item that is already current — so the landing screen,
        /// reached by starting the app rather than by navigating, never loaded at all. It showed
        /// its chrome over no data until the user went somewhere else and came back.
        ///
        /// Guarded so a normal navigation, which raises both events, still loads exactly once.
        /// </summary>
        public virtual Task OnPageAppearingAsync()
        {
            if (this is not ICompute || _hasLoadedOnce)
            {
                return Task.CompletedTask;
            }

            return LoadItems();
        }

        public virtual Task OnPageDisappearingAsync() => Task.CompletedTask;

        /// <summary>
        /// Back from any top-level screen returns to Today; on Today it is left to Android, which
        /// closes the app. It used to be consumed there as well — "navigating" to the screen
        /// already showing — so Back did nothing at all on Today and could never leave the app.
        /// </summary>
        public virtual bool OnBackButtonPressed() => _navigationService.NavigateToDefaultShellItem();

        public virtual void OnNavigatedFrom() { }

        public virtual void OnNavigatingFrom() { }

        public virtual Task OnNavigatedToAsync()
        {
            return this is ICompute ? LoadItems() : Task.CompletedTask;
        }

        /// <summary>Set by the first completed load, so appearing does not re-fetch every time.</summary>
        private bool _hasLoadedOnce;

        public virtual void OnAppWindowCreated() { }
        public virtual void OnAppWindowActivated() { }
        public virtual void OnAppWindowResumed() { }
        public virtual void OnAppWindowBackgrounding() { }
        public virtual void OnAppWindowStopped() { }
        public virtual void OnAppWindowDestroying() { }
    }
}
