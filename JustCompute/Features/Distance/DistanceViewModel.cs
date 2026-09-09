using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Compute.Core.Domain.Entities.Models.Distance;
using Compute.Core.Domain.Entities.Models.Fuel;
using Compute.Core.Domain.Entities.Models.Route;
using JustCompute.Features.SearchByCity;
using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Shared.ViewModels;
using JustCompute.Shared.Helpers;
using System.Collections.ObjectModel;
using System.Globalization;
using Location = Compute.Core.Domain.Entities.Models.Location;
using JustCompute.Resources.Strings;
using Microsoft.Extensions.Localization;
using System.ComponentModel;

namespace JustCompute.Features.Distance
{
    /// <summary>
    /// The Ruler: a route through as many places as you like, measured leg by leg.
    ///
    /// It used to be two fixed points and a single number, which could answer exactly one
    /// question and looked like a form while doing it. A route answers the same question — two
    /// pins is still a straight line — and also "how far is it if I go via here", which is what
    /// a ruler is usually for.
    /// </summary>
    public partial class DistanceViewModel : BaseViewModel, IResultHandler
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasStops))]
        [NotifyPropertyChangedFor(nameof(HasRoute))]
        private ObservableCollection<RouteStop> stops = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasRoute))]
        private RouteResult? route;

        [ObservableProperty]
        private string totalDistance = string.Empty;

        [ObservableProperty]
        private string directDistance = string.Empty;

        [ObservableProperty]
        private string detourRatio = string.Empty;

        [ObservableProperty]
        private bool hasDetour;

        /// <summary>
        /// Litres per 100 km, mpg's metric cousin — or whatever volume the reader thinks in, per
        /// 100 of their distance unit. Text, not a number: a field mid-edit is not always valid.
        /// </summary>
        [ObservableProperty]
        private string fuelConsumption = global::JustCompute.Shared.Helpers.Settings.FuelConsumption;

        [ObservableProperty]
        private string fuelPrice = global::JustCompute.Shared.Helpers.Settings.FuelPrice;

        [ObservableProperty]
        private ObservableCollection<FuelUnitOption> fuelUnits = [];

        /// <summary>
        /// Which convention the consumption figure is in. Guessed from the reader's region on
        /// first visit, then remembered — the number means nothing without it.
        /// </summary>
        [ObservableProperty]
        private FuelUnitOption? selectedFuelUnit;

        [ObservableProperty]
        private string fuelNeeded = string.Empty;

        [ObservableProperty]
        private string fuelCost = string.Empty;

        [ObservableProperty]
        private bool hasFuelEstimate;

        [ObservableProperty]
        private bool hasFuelCost;

        /// <summary>
        /// The stored choice, or a guess from where the reader is. Region beats the distance
        /// setting: a British user who switched the app to kilometres still buys fuel by the
        /// imperial gallon.
        /// </summary>
        private static FuelConsumptionMode RestoreOrGuessMode()
        {
            string stored = global::JustCompute.Shared.Helpers.Settings.FuelConsumptionMode;
            if (Enum.TryParse(stored, out FuelConsumptionMode saved))
            {
                return saved;
            }

            string? region = null;
            try
            {
                region = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch (Exception)
            {
                // No region on this device; the distance unit is the remaining hint.
            }

            return FuelCalculator.DefaultFor(
                region, global::JustCompute.Shared.Helpers.Settings.DistanceType);
        }

        partial void OnSelectedFuelUnitChanged(FuelUnitOption? value)
        {
            if (value is null) return;

            global::JustCompute.Shared.Helpers.Settings.FuelConsumptionMode = value.Mode.ToString();
            RecalculateFuel();
        }

        partial void OnFuelConsumptionChanged(string value)
        {
            global::JustCompute.Shared.Helpers.Settings.FuelConsumption = value;
            RecalculateFuel();
        }

        partial void OnFuelPriceChanged(string value)
        {
            global::JustCompute.Shared.Helpers.Settings.FuelPrice = value;
            RecalculateFuel();
        }

        public bool HasStops => Stops.Count > 0;

        /// <summary>Two pins are the minimum that can be measured; below that the hint shows.</summary>
        public bool HasRoute => Route is not null;

        private readonly IStringLocalizer<AppStringsRes> _localizer;
        private readonly DistanceFormatter _distanceFormatter;

        public DistanceViewModel(
            ViewModelServices services,
            IStringLocalizer<AppStringsRes> localizer,
            DistanceFormatter distanceFormatter) : base(services)
        {
            _localizer = localizer;
            _distanceFormatter = distanceFormatter;

            FuelUnits =
            [
                new(FuelConsumptionMode.LitersPer100Km, localizer.GetString("FuelModeLitersPer100KmLabel")),
                new(FuelConsumptionMode.KilometersPerLiter, localizer.GetString("FuelModeKmPerLiterLabel")),
                new(FuelConsumptionMode.MilesPerUsGallon, localizer.GetString("FuelModeMpgUsLabel")),
                new(FuelConsumptionMode.MilesPerImperialGallon, localizer.GetString("FuelModeMpgUkLabel")),
            ];

            selectedFuelUnit = FuelUnits.FirstOrDefault(o => o.Mode == RestoreOrGuessMode())
                               ?? FuelUnits[0];

            Stops.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasStops));
                Renumber();
            };
        }

        /// <summary>Set once the opening pin has been offered, whatever became of it.</summary>
        private bool _seeded;

        protected override Task GetData(Location location)
        {
            // Seed the route with wherever the user is, so the screen opens with something on it
            // rather than an empty list and a hint — but only on first arrival.
            //
            // Keyed on having seeded rather than on the list being empty: picking a city returns
            // to this screen, which re-enters it, and an empty-list test then re-added the
            // current location alongside the city just chosen. Clearing the route is something
            // the user did on purpose, and it has to stay done.
            if (!_seeded)
            {
                _seeded = true;

                if (Stops.Count == 0)
                {
#if DEBUG
                    // A screenshot run can hand the Ruler a real route. The pins live in memory,
                    // so unlike a saved place this cannot be arranged before the app starts.
                    if (global::JustCompute.Shared.Helpers.ScreenshotHarness.SeededRouteStops.Count > 1)
                    {
                        foreach (var stop in global::JustCompute.Shared.Helpers.ScreenshotHarness.SeededRouteStops)
                        {
                            Add(stop);
                        }
                        Renumber();
                        return Task.CompletedTask;
                    }
#endif
                    Add(location.Clone());
                    return Task.CompletedTask;
                }
            }

            // Re-measure on every entry, not only when the route changes: the distance unit is
            // chosen on another screen, and coming back from it used to leave every figure here
            // in the old unit until a pin happened to be added or removed.
            Recalculate();
            return Task.CompletedTask;
        }

        protected override void ClearData()
        {
            ForgetAll();
            Stops.Clear();
            Recalculate();
        }

        [RelayCommand]
        private async Task AddFromSearch() =>
            await _navigationService.NavigateToAsync<SearchByCityViewModel>(SearchLocationContext.ReturnResult);

        [RelayCommand]
        private void AddCurrentLocation()
        {
            Location? here = _gpsLocationService.DeviceLocation ?? _gpsLocationService.SelectedLocation;
            if (here is null) return;

            Add(here.Clone());
        }

        [RelayCommand]
        private void Undo()
        {
            if (Stops.Count == 0) return;

            Forget(Stops[^1]);
            Stops.RemoveAt(Stops.Count - 1);
            Recalculate();
        }

        [RelayCommand]
        private void Clear()
        {
            ForgetAll();
            Stops.Clear();
            Recalculate();
        }

        [RelayCommand]
        private void Remove(RouteStop? stop)
        {
            if (stop is null) return;

            Forget(stop);
            Stops.Remove(stop);
            Recalculate();
        }

        /// <summary>Opens or closes a pin's coordinate editor. Only one is open at a time.</summary>
        [RelayCommand]
        private void ToggleEditor(RouteStop? stop)
        {
            if (stop is null) return;

            bool opening = !stop.IsExpanded;

            foreach (RouteStop other in Stops)
            {
                other.IsExpanded = false;
            }

            stop.IsExpanded = opening;
        }

        /// <summary>
        /// Re-measures after the user has typed into a pin's coordinates, and drops the name that
        /// came with it — a hand-typed position is no longer that city.
        ///
        /// Driven by the editable's own property change rather than by the entry's
        /// "stopped typing" command: that behaviour passes the *entry text* when no
        /// CommandParameter is set, so a command typed as RelayCommand&lt;RouteStop&gt; was being
        /// handed a string. The generated command throws on a parameter of the wrong type, so
        /// editing a coordinate could take the app down — and never re-measured either way.
        /// </summary>
        private void OnStopPositionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(EditableLocation.Latitude) or nameof(EditableLocation.Longitude)))
            {
                return;
            }

            if (Stops.FirstOrDefault(s => ReferenceEquals(s.Editable, sender)) is { } stop)
            {
                stop.Editable.Name = string.Empty;
            }

            Recalculate();
        }

        /// <summary>The city picked on the search screen comes back here.</summary>
        public void ApplyResult(object? result)
        {
            if (result is Location picked)
            {
                Add(picked);
            }
        }

        private void Add(Location location)
        {
            var stop = new RouteStop(location);
            stop.Editable.PropertyChanged += OnStopPositionChanged;

            Stops.Add(stop);
            Recalculate();
        }

        /// <summary>Drops the handler with the pin, so a removed stop keeps nothing alive.</summary>
        private void Forget(RouteStop stop) => stop.Editable.PropertyChanged -= OnStopPositionChanged;

        private void ForgetAll()
        {
            foreach (RouteStop stop in Stops)
            {
                Forget(stop);
            }
        }

        private void Renumber()
        {
            for (int i = 0; i < Stops.Count; i++)
            {
                Stops[i].Number = i + 1;
            }
        }

        private void Recalculate()
        {
            Renumber();

            Route = RoutePlanner.Measure([.. Stops.Select(s => s.Location)]);

            foreach (RouteStop stop in Stops)
            {
                Location at = stop.Location;
                stop.Name = at.Name;
                stop.Coordinates = $"{at.LatitudeDms}   {at.LongitudeDms}";
                stop.Leg = string.Empty;
                stop.HasLeg = false;
            }

            if (Route is null)
            {
                TotalDistance = string.Empty;
                DirectDistance = string.Empty;
                DetourRatio = string.Empty;
                HasDetour = false;
                OnPropertyChanged(nameof(HasRoute));

                // Also clears the fuel figures. Leaving them behind kept the last route's litres
                // and cost in state after the route was cleared.
                RecalculateFuel();
                return;
            }

            foreach (RouteLeg leg in Route.Legs)
            {
                RouteStop from = Stops[leg.FromNumber - 1];
                from.Leg = $"↓ {Format(leg.DistanceMeters)}   ·   {Bearing(leg.InitialBearingDeg)}°";
                from.HasLeg = true;
            }

            TotalDistance = Format(Route.TotalMeters);
            DirectDistance = $"{Format(Route.DirectMeters)}   ·   {Bearing(Route.DirectBearingDeg)}°";

            // Only worth showing once there is a via-point: a two-pin route *is* the straight
            // line, so its ratio is always 1× and says nothing.
            HasDetour = Route.DetourRatio is not null && Route.Legs.Count > 1;
            DetourRatio = Route.DetourRatio is { } ratio
                ? ratio.ToString("0.##", CultureInfo.CurrentCulture) + "×"
                : string.Empty;

            OnPropertyChanged(nameof(HasRoute));
            RecalculateFuel();
        }

        /// <summary>
        /// What the route costs to drive. Recomputed both when the route changes and when either
        /// figure is typed into, since either can move the answer.
        /// </summary>
        private void RecalculateFuel()
        {
            FuelConsumptionMode mode = SelectedFuelUnit?.Mode ?? FuelConsumptionMode.LitersPer100Km;

            FuelEstimate? estimate = FuelCalculator.Estimate(
                Route?.TotalMeters ?? 0, mode, ParseNumber(FuelConsumption) ?? 0, ParseNumber(FuelPrice));

            HasFuelEstimate = estimate is not null;
            HasFuelCost = estimate?.Cost is not null;

            // The volume unit follows the convention, so a price "per gallon" is never quietly
            // multiplied by litres.
            string volumeUnit = FuelCalculator.VolumeUnitFor(mode) == FuelVolumeUnit.Liters
                ? _localizer.GetString("LiterAbbreviationLabel")
                : _localizer.GetString("GallonAbbreviationLabel");

            FuelNeeded = estimate is null ? string.Empty : $"{Round(estimate.Volume)} {volumeUnit}";

            // Money keeps its minor units whatever the magnitude. Rounding a cost to whole
            // numbers past 100 the way a volume can be rounded turns 110.65 into "111", which
            // is the one figure on this screen someone might actually hand over.
            FuelCost = estimate?.Cost is { } cost
                ? cost.ToString("N2", CultureInfo.CurrentCulture)
                : string.Empty;
        }

        /// <summary>
        /// Accepts the reader's own decimal separator first, then a plain dot — a keypad that
        /// types "." on a comma locale should not silently mean nothing.
        /// </summary>
        private static double? ParseNumber(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double local))
            {
                return local;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double dot)
                ? dot
                : null;
        }

        /// <summary>
        /// A whole-degree bearing. Rounding alone turns 359.7° into "360°", which is not a
        /// bearing — the scale is [0, 360) and due north is 0.
        /// </summary>
        private static int Bearing(double degrees) => (int)Math.Round(degrees) % 360;

        /// <summary>Metres in the reader's chosen unit. Shared with Speed &amp; Distance, which
        /// is the other screen that measures a journey.</summary>
        private string Format(double meters) => _distanceFormatter.Format(meters);

        private static string Round(double value) => DistanceFormatter.Number(value);

        public override Task OnNavigatedToAsync() => LoadItems();
    }
}
