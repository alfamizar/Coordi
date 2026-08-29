using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Weather;
using Compute.Core.Domain.Services.Weather;
using JustCompute.Shared.ViewModels;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Features.Today
{
    /// <summary>
    /// One place, one date, everything true about it — and a date control that turns every
    /// card into a time machine. This replaces the year-long Sun and Moon lists and absorbs
    /// the old standalone Time Travel screen.
    /// </summary>
    public partial class TodayViewModel : BaseViewModel, ICompute
    {
        private const int ForecastDays = 7;

        private readonly IWeatherService _weatherService;
        private CancellationTokenSource? _weatherCancellationTokenSource;

        // Set once the user moves off "now" — after that the date is theirs and a location change
        // must not silently drag it somewhere else.
        private bool _hasUserChosenDate;
        private bool _syncingDateToLocation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowingToday))]
        [NotifyPropertyChangedFor(nameof(CurrentTime))]
        [NotifyPropertyChangedFor(nameof(IsDateWithinForecast))]
        private DateTime selectedDate = DateTime.Today;

        /// <summary>
        /// The place being shown. Everything on this screen is in its own time — the device's
        /// clock and calendar are irrelevant to what the sky does there — and the offset is read
        /// per date, so time travel across a daylight-saving change lands on the right hour.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowingToday))]
        [NotifyPropertyChangedFor(nameof(CurrentTime))]
        [NotifyPropertyChangedFor(nameof(IsDateWithinForecast))]
        private Location? location;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private CelestialSnapshot? snapshot;

        [ObservableProperty]
        private WeatherForecast? weatherForecast;

        /// <summary>Skeleton is up while the forecast is in flight.</summary>
        [ObservableProperty]
        private bool isWeatherLoading;

        /// <summary>
        /// The forecast could not be fetched. Deliberately a quiet, retryable state rather than
        /// a hidden card: weather is secondary here, but silently vanishing leaves no way back.
        /// </summary>
        [ObservableProperty]
        private bool isWeatherFailed;

        [ObservableProperty]
        private string locationName = string.Empty;

        /// <summary>
        /// True while the app is showing the placeholder location, i.e. the user has not chosen
        /// anywhere yet. Drives the onboarding card.
        /// </summary>
        [ObservableProperty]
        private bool isPlaceholderLocation;

        /// <summary>Wall-clock time at the location, which is what every reading here is against.</summary>
        private DateTime LocationNow =>
            Location is { } place ? DateTime.UtcNow + place.GetUtcOffset(DateTime.UtcNow) : DateTime.UtcNow;

        /// <summary>True while the screen is showing the current date at the location.</summary>
        public bool IsShowingToday => SelectedDate.Date == LocationNow.Date;

        /// <summary>
        /// The forecast runs seven days from today, so it stays meaningful while time-travelling
        /// inside that window — only dates outside it have no weather to show.
        /// </summary>
        public bool IsDateWithinForecast =>
            SelectedDate.Date >= LocationNow.Date &&
            SelectedDate.Date <= LocationNow.Date.AddDays(ForecastDays - 1);

        /// <summary>
        /// The "you are here" marker on the sun-path chart. Null on any other date — drawing a
        /// current-position marker on a day that is not today would be a lie. It has to be the
        /// location's clock: the chart's axis is the location's day, so the device's would sit
        /// hours off, or on the wrong day entirely.
        /// </summary>
        public DateTime? CurrentTime => IsShowingToday ? LocationNow : null;

        public TodayViewModel(ViewModelServices services, IWeatherService weatherService)
            : base(services)
        {
            _weatherService = weatherService;
        }

        [RelayCommand]
        private void PreviousDay() => ShiftDate(-1);

        [RelayCommand]
        private void NextDay() => ShiftDate(1);

        [RelayCommand]
        private void PreviousMonth() => ShiftMonths(-1);

        [RelayCommand]
        private void NextMonth() => ShiftMonths(1);

        [RelayCommand]
        private void GoToToday()
        {
            _hasUserChosenDate = false;
            SelectedDate = LocationNow.Date;
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                await LoadItems();
            }
            finally
            {
                // RefreshView spins until the view model says otherwise, including on failure.
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Locations is a Shell flyout destination, not a registered modal page — going through
        /// NavigateToAsync&lt;T&gt; silently does nothing.
        /// </summary>
        [RelayCommand]
        private Task SetLocation() => _navigationService.NavigateToShellRouteAsync("locations");

        private void ShiftDate(int days) => SelectedDate = SelectedDate.AddDays(days);

        private void ShiftMonths(int months) => SelectedDate = SelectedDate.AddMonths(months);

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (!_syncingDateToLocation)
            {
                _hasUserChosenDate = true;
            }

            _ = LoadItems();
        }

        protected override async Task GetData(Location location)
        {
            Location = location;

            // The screen opens on "now", and "now" belongs to the place, not to the phone. Across
            // enough longitude those are different calendar days, so land on the location's.
            if (!_hasUserChosenDate && SelectedDate.Date != LocationNow.Date)
            {
                _syncingDateToLocation = true;
                SelectedDate = LocationNow.Date;
                _syncingDateToLocation = false;
                return; // the assignment above kicked off a fresh load for the right date
            }

            var date = SelectedDate.Date;
            var latitude = location.Latitude;
            var longitude = location.Longitude;

            // Read the offset for the day being shown, not for today: step across the end of
            // daylight saving and every time on this screen shifts by an hour, as it should.
            var offsetHours = location.GetUtcOffsetHours(DateTime.SpecifyKind(date, DateTimeKind.Utc));

            LocationName = location.Name;
            IsPlaceholderLocation = !global::JustCompute.Shared.Helpers.Settings.HasUserSetLocation;

            // The snapshot is pure computation, so keep it off the UI thread — a year of
            // timezone lookups plus the lunar series is not free.
            Snapshot = await Task.Run(
                () => CelestialSnapshot.For(latitude, longitude, date, offsetHours));

            // Stepping the date fires a fresh load without waiting for the previous one, so two
            // can overlap; anything computed for a date the user has already left is dropped.
            if (SelectedDate.Date != date) return;

            if (IsDateWithinForecast)
            {
                await LoadWeatherForecastAsync(latitude, longitude, date);
            }
            else
            {
                CancelWeatherLoad();
                WeatherForecast = null;
                IsWeatherLoading = false;
                IsWeatherFailed = false;
            }
        }

        protected override void ClearData()
        {
            CancelWeatherLoad();
            Snapshot = null;
            WeatherForecast = null;
            LocationName = string.Empty;
            Location = null;
        }

        [RelayCommand]
        private async Task RetryWeather()
        {
            var location = _gpsLocationService.SelectedLocation;
            if (location == null || !IsDateWithinForecast) return;

            await LoadWeatherForecastAsync(location.Latitude, location.Longitude, SelectedDate.Date);
        }

        private async Task LoadWeatherForecastAsync(double latitude, double longitude, DateTime forDate)
        {
            CancelWeatherLoad();
            var cts = new CancellationTokenSource();
            _weatherCancellationTokenSource = cts;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsWeatherFailed = false;
                // Only show the skeleton when there is nothing to replace; on a refresh the
                // existing strip stays put rather than flashing back to placeholders.
                IsWeatherLoading = WeatherForecast == null;
            });

            try
            {
                var forecast = await _weatherService
                    .GetDailyForecastAsync(latitude, longitude, ForecastDays, cts.Token)
                    .ConfigureAwait(false);

                if (cts.IsCancellationRequested || SelectedDate.Date != forDate) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherForecast = forecast;
                    IsWeatherLoading = false;
                    IsWeatherFailed = forecast == null;
                });
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer request or a location change — nothing to report.
            }
            catch (Exception)
            {
                if (cts.IsCancellationRequested || SelectedDate.Date != forDate) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    WeatherForecast = null;
                    IsWeatherLoading = false;
                    IsWeatherFailed = true;
                });
            }
        }

        private void CancelWeatherLoad()
        {
            _weatherCancellationTokenSource?.Cancel();
            _weatherCancellationTokenSource?.Dispose();
            _weatherCancellationTokenSource = null;
        }

        public override Task OnNavigatedToAsync() => LoadItems();
    }
}
