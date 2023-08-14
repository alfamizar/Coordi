using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using Compute.Core.Services;
using JustCompute.Services;
using Compute.Core.Domain.Services;
using Location = Compute.Core.Domain.Entities.Models.Location;


namespace JustCompute.Presentation.ViewModels.Base
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly IDialogService _dialogService;
        protected readonly ILocationManager _locationManager;

        public static readonly int TotalNumberOfDaysInTheCurrentYear = DateTime.IsLeapYear(DateTime.UtcNow.Year) ? 366 : 365;

        public Dictionary<string, ICommand> Commands { get; protected set; }

        [ObservableProperty]
        bool isBusy;

        [ObservableProperty]
        string title;

        [ObservableProperty]
        Location location;

        protected BaseViewModel()
        {
            Commands = new Dictionary<string, ICommand>();
            _dialogService = ServicesProvider.Current.GetService<IDialogService>();
            _locationManager = ServicesProvider.Current.GetService<ILocationManager>();
        }

        protected async Task LoadItems()
        {
            IsBusy = true;

            if (!_locationManager.GettingLocationFinished.Task.IsCompleted)
            {
                await _locationManager.GettingLocationFinished.Task;
            }
            Location = _locationManager.CurrentLocation;

            if (Location == null)
            {
                IsBusy = false;
                return;
            }

            await GetData(Location.Latitude, Location.Longitude, Location.TimeZoneOffset);

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

        public virtual void OnBackButtonPressed()
        {
            // Custom logic
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
            await LoadItems();
            // Custom logic
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