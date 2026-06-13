using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using Compute.Core.Navigation;
using Compute.Core.UI;
using Compute.Core.Domain.Services;
using JustCompute.Shared.ViewModels;


namespace JustCompute.Shared.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly IDialogService _dialogService;
        protected readonly IGPSLocationService _gpsLocationService;
        protected readonly ILocationService _locationService;
        protected readonly INavigationService _navigationService;

        public static readonly int TotalNumberOfDaysInTheCurrentYear = DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366 : 365;

        public Dictionary<string, ICommand> Commands { get; protected set; }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string title = string.Empty;

        protected BaseViewModel(ViewModelServices services)
        {
            Commands = [];
            _dialogService = services.DialogService;
            _gpsLocationService = services.GpsLocationService;
            _locationService = services.LocationService;
            _navigationService = services.NavigationService;
        }

        protected virtual async Task LoadItems()
        {
            IsBusy = true;
            if (_gpsLocationService.IsGettingDeviceLocation && _gpsLocationService.GettingDeviceLocationFinished is not null)
            {
                await _gpsLocationService.GettingDeviceLocationFinished.Task;
            }

            var location = _gpsLocationService.SelectedLocation;

            if (location == null)
            {
                ClearData();
                IsBusy = false;
                return;
            }

            await GetData(location.LatitudeDouble, location.LongitudeDouble, location.TimeZoneOffset.Hours);

            IsBusy = false;
        }

        protected virtual Task GetData(double latitude, double longitude, int timeZoneOffset)
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
