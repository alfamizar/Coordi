using Compute.Core.Domain.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Common.Messaging;
using Compute.Core.Domain.Entities.Models.Time;
using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Shared.Abstractions.UI;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.ViewModels.Messages;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.ComponentModel;
using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;
using JustCompute.Presentation.Locations;

namespace JustCompute.Features.InputLocation
{
    public partial class InputLocationViewModel : BaseViewModel, IQueryParameter
    {
        private readonly ILocationService _locationService;

        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly IToastService _toastService;
        private readonly IMessagingService _messagingService;
        private LocationInputContext? _locationInputContext;

        [ObservableProperty]
        private TimeZoneOffset selectedTimeZoneOffset;

        /// <summary>The picker's choices. Fixed, so there is nothing to observe.</summary>
        public IReadOnlyList<TimeZoneOffset> TimeZoneOffsets => TimeZoneOffset.WholeHourOffsets;

        [ObservableProperty]
        private EditableLocation location = new();

        /// <summary>
        /// The same page serves both contexts, so the title has to say which one it is —
        /// it read "Add Location" even when editing an existing place.
        /// </summary>
        [ObservableProperty]
        private string pageTitle = string.Empty;


        public InputLocationViewModel(
            ViewModelServices services,
            ILocationService locationService,
            IToastService toastService,
            IMessagingService messagingService,
            IStringLocalizer<AppStringsRes> localizer
            )
            : base(services)
        {
            _locationService = locationService;
            _toastService = toastService;
            _messagingService = messagingService;
            _localizer = localizer;


            pageTitle = localizer.GetString("AddLocationLabel");
            selectedTimeZoneOffset = TimeZoneOffset.DefaultTimeZoneOffset;

            PropertyChanged += OnPropertyChanged;
            Location.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Location) ||
                e.PropertyName == nameof(Location.Name) ||
                e.PropertyName == nameof(Location.IsLatitudeValid) ||
                e.PropertyName == nameof(Location.IsLongitudeValid))
            {
                SaveLocationCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Judged on what the fields hold, not on the numbers behind them: a field that does not
        /// read as a coordinate leaves the last one that did in place, and saving that would store
        /// a place the user never typed.
        /// </summary>
        private bool CanSaveLocation()
        {
            return !string.IsNullOrWhiteSpace(Location?.Name)
                   && Location.IsLatitudeValid
                   && Location.IsLongitudeValid;
        }

        [RelayCommand]
        private void GoBack() => OnBackButtonPressed();

        [RelayCommand(CanExecute = nameof(CanSaveLocation))]
        private async Task SaveLocation()
        {
            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");

            switch (_locationInputContext.Value)
            {
                case LocationInputContext.Add:
                    {
                        if (Location != null)
                            await SaveLocationIfNotExists(Location.ToLocation());
                        break;
                    }
                case LocationInputContext.Edit:
                    {
                        if (Location != null)
                            await UpdateLocation(Location.ToLocation());
                        break;
                    }
            }
        }

        private async Task SaveLocationIfNotExists(Location location)
        {
            var savedLocations = await _locationService.GetSavedLocations();
            if (!savedLocations.Any(x => x.Name == location.Name))
            {
                await _locationService.SaveLocation(location);

                // Announce it like Edit and Delete already do. Without this the new place only
                // surfaces on the next full re-init of the Locations screen, which is exactly the
                // sort of thing that gets skipped while a device fix is still in flight.
                _messagingService.Send(new LocationMessage(location, LocationInputContext.Add));

                // The place is saved and now current, so going back one step to the city search
                // the user has finished with is the wrong destination — return to Locations,
                // where the result of what they just did is actually visible.
                _locationInputContext = null;
                await _navigationService.NavigateToShellRouteAsync("locations");
            }
            else
            {
                await _toastService.ShowToast(_localizer.GetString("DuplicatedLocationToastMessge"));
            }
        }

        private async Task UpdateLocation(Location location)
        {
            await _locationService.UpdateLocation(location);

            _messagingService.Send(new LocationMessage(location, LocationInputContext.Edit));

            OnBackButtonPressed();
        }

        [RelayCommand]
        private async Task PrefillCoordinates()
        {
            if (Location == null || IsBusy) return;

            if (_device.DeviceLocation is null)
            {
                IsBusy = true;
                await _device.GetDeviceGeoLocation();
                IsBusy = false;
            }

            Location.Latitude = _device.DeviceLocation?.Latitude ?? Location.Latitude;
            Location.Longitude = _device.DeviceLocation?.Longitude ?? Location.Longitude;
        }

        public void ApplyQueryParameter(object? parameter)
        {
            if (parameter is LocationEditorArgs args)
            {
                if (Location != null)
                {
                    Location.PropertyChanged -= OnPropertyChanged;
                }

                _locationInputContext = args.Context;

                if (args.Location is not null)
                {
                    // EditableLocation already takes a detached copy, so editing never reaches
                    // the caller's instance and a cancelled edit leaves the lists untouched.
                    Location = new EditableLocation(args.Location);
                }

                if (Location != null)
                {
                    Location.PropertyChanged += OnPropertyChanged;
                }
            }

            if (!_locationInputContext.HasValue) throw new Exception("VM context parameter must be specified");

            PageTitle = _locationInputContext.Value == LocationInputContext.Edit
                ? _localizer.GetString("EditLocationLabel")
                : _localizer.GetString("AddLocationLabel");

            // Nothing is blanked here. Adding *from a search* arrives with the city already
            // chosen, and clearing its name left the user retyping the place they had just
            // picked — with Save disabled until they did. Adding from scratch arrives with no
            // location at all, so the field is empty anyway.
        }

        public override bool OnBackButtonPressed()
        {
            _locationInputContext = null;
            _navigationService.NavigateBackAsync();
            return true;
        }
    }
}
