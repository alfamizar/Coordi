using CommunityToolkit.Mvvm.ComponentModel;
using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Shared.Abstractions.UI;
using Compute.Core.Domain.Services;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;


namespace JustCompute.Shared.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly IDialogService _dialogService;
        protected readonly IGPSLocationService _gpsLocationService;
        protected readonly ILocationService _locationService;
        protected readonly INavigationService _navigationService;

        public static readonly int TotalNumberOfDaysInTheCurrentYear = DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366 : 365;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string title = string.Empty;

        protected BaseViewModel(ViewModelServices services)
        {
            _dialogService = services.DialogService;
            _gpsLocationService = services.GpsLocationService;
            _locationService = services.LocationService;
            _navigationService = services.NavigationService;
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
                await _gpsLocationService.RestorePersistedSelectedLocation();

                if (_gpsLocationService.IsGettingDeviceLocation && _gpsLocationService.GettingDeviceLocationFinished is not null)
                {
                    await _gpsLocationService.GettingDeviceLocationFinished.Task;
                }

                var location = _gpsLocationService.SelectedLocation;

                if (location == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(ClearData);
                    return;
                }

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

        protected virtual void ClearData() { }

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

        public virtual bool OnBackButtonPressed()
        {
            _navigationService.NavigateToDefaultShellItem();
            return true;
        }

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
