using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using JustCompute.Services;
using Compute.Core.Navigation;
using Compute.Core.UI;
using Compute.Core.Domain.Services;
using JustCompute.Presentation.ViewModels.Common;


namespace JustCompute.Presentation.ViewModels.Base
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

        protected BaseViewModel()
        {
            Commands = [];
            _dialogService = ServicesProvider.GetService<IDialogService>();
            _gpsLocationService = ServicesProvider.GetService<IGPSLocationService>();
            _locationService = ServicesProvider.GetService<ILocationService>();
            _navigationService = ServicesProvider.GetService<INavigationService>();
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

        public virtual void OnPageAppearing()
        {
            // Custom logic
        }

        public virtual void OnPageDisappearing()
        {
            // Custom logic
        }

        public virtual bool OnBackButtonPressed()
        {
            _navigationService.NavigateToDefaultShellItem();
            return true;
        }

        public virtual void OnNavigatedFrom()
        {
            // Custom logic
        }

        public virtual void OnNavigatingFrom()
        {
            // Custom logic
        }

        public virtual async void OnNavigatedTo()
        {
            if (this is ICompute)
            {
                await LoadItems();
            }
        }

        public virtual void OnAppWindowCreated()
        {
            // Custom logic
        }

        public virtual void OnAppWindowActivated()
        {
            // Custom logic
        }

        public virtual void OnAppWindowResumed()
        {
            // Custom logic
        }

        public virtual void OnAppWindowBackgrounding()
        {
            // Custom logic
        }

        public virtual void OnAppWindowStopped()
        {
            // Custom logic
        }
        public virtual void OnAppWindowDestroying()
        {
            // Custom logic
        }
    }
}