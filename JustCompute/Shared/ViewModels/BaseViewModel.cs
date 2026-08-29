using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Navigation;
using Compute.Core.UI;
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

        protected virtual async Task LoadItems()
        {
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
                await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
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

        public virtual Task OnPageAppearingAsync() => Task.CompletedTask;

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

        public virtual void OnAppWindowCreated() { }
        public virtual void OnAppWindowActivated() { }
        public virtual void OnAppWindowResumed() { }
        public virtual void OnAppWindowBackgrounding() { }
        public virtual void OnAppWindowStopped() { }
        public virtual void OnAppWindowDestroying() { }
    }
}
