using System.Globalization;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Time;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.Locations
{
    /// <summary>
    /// An editable, observable view of a location.
    ///
    /// Three screens edit coordinates in place — Add/Edit Location and both points on the
    /// Distance screen — binding two-way and reacting as the user types. All of that needs
    /// change notification, which used to come from the domain <see cref="Location"/> itself:
    /// every consumer of the domain model paid for an MVVM dependency so those screens could
    /// edit.
    ///
    /// It wraps a live domain object rather than copying its fields out. Copying looked simpler
    /// but silently dropped a rule: the displayed UTC offset is *derived* from the coordinates
    /// unless the user pins one, so a snapshot left "UTC±0" on screen after the coordinates were
    /// filled in from a device fix. Delegating keeps that rule in one place — the domain model.
    /// </summary>
    public class EditableLocation : ObservableObject
    {
        private readonly Location _location;
        private string _latitudeText;
        private string _longitudeText;

        public EditableLocation() : this(new Location()) { }

        public EditableLocation(Location location)
        {
            // Detached: a cancelled edit must not leave its changes in the list still holding
            // the instance it was opened from.
            _location = location.Clone();
            _latitudeText = FieldText(_location.Latitude);
            _longitudeText = FieldText(_location.Longitude);
        }

        public string Name
        {
            get => _location.Name;
            set => Set(_location.Name, value, v => _location.Name = v);
        }

        public string CityName
        {
            get => _location.City.CityName;
            set => Set(_location.City.CityName, value, v => _location.City.CityName = v);
        }

        public string CountryName
        {
            get => _location.City.CountryName;
            set => Set(_location.City.CountryName, value, v => _location.City.CountryName = v);
        }

        /// <summary>
        /// The latitude as a number. Set from code — a device fix, a city picked, a stop moved —
        /// it rewrites the field; typed into the field, it follows <see cref="LatitudeText"/>.
        /// </summary>
        public double Latitude
        {
            get => _location.Latitude;
            set
            {
                if (!ApplyLatitude(value)) return;
                SetProperty(ref _latitudeText, FieldText(value), nameof(LatitudeText));
                OnPropertyChanged(nameof(IsLatitudeValid));
            }
        }

        public double Longitude
        {
            get => _location.Longitude;
            set
            {
                if (!ApplyLongitude(value)) return;
                SetProperty(ref _longitudeText, FieldText(value), nameof(LongitudeText));
                OnPropertyChanged(nameof(IsLongitudeValid));
            }
        }

        /// <summary>
        /// What the latitude field holds, exactly as typed. Bound instead of <see cref="Latitude"/>:
        /// a two-way binding straight to a number converted every keystroke and wrote it back, so
        /// a decimal separator vanished the moment it was typed — "50,5" became 505, in English as
        /// well as German, and only a dot in English survived. The text is never rewritten while
        /// it is typed; the number follows it whenever it reads as a latitude, comma or dot.
        /// </summary>
        public string LatitudeText
        {
            get => _latitudeText;
            set
            {
                if (!SetProperty(ref _latitudeText, value ?? string.Empty)) return;
                OnPropertyChanged(nameof(IsLatitudeValid));
                if (ParseWithin(_latitudeText, 90.0) is double latitude) ApplyLatitude(latitude);
            }
        }

        public string LongitudeText
        {
            get => _longitudeText;
            set
            {
                if (!SetProperty(ref _longitudeText, value ?? string.Empty)) return;
                OnPropertyChanged(nameof(IsLongitudeValid));
                if (ParseWithin(_longitudeText, 180.0) is double longitude) ApplyLongitude(longitude);
            }
        }

        /// <summary>Whether the field reads as a latitude. While it does not, the last one that did stands.</summary>
        public bool IsLatitudeValid => ParseWithin(_latitudeText, 90.0) is not null;

        public bool IsLongitudeValid => ParseWithin(_longitudeText, 180.0) is not null;

        /// <summary>
        /// The offset shown in the picker. Assigning one pins the location to that fixed offset;
        /// the domain setter is what tells a deliberate choice apart from the picker echoing back
        /// the value it was just given.
        /// </summary>
        public TimeZoneOffset TimeZoneOffset
        {
            get => _location.TimeZoneOffset;
            set
            {
                if (value is null) return;

                _location.TimeZoneOffset = value;
                OnPropertyChanged();
            }
        }

        /// <summary>The edited values as a domain location, ready to save.</summary>
        public Location ToLocation() => _location.Clone();

        /// <summary>
        /// The number on its own, without touching the text: a field being typed into keeps what
        /// was typed. The zone is resolved from the coordinates, so the displayed offset moves with
        /// them — unless the user has pinned one, which the domain model decides.
        /// </summary>
        private bool ApplyLatitude(double latitude)
        {
            if (!Set(_location.Latitude, latitude, v => _location.Latitude = v, nameof(Latitude))) return false;
            OnPropertyChanged(nameof(TimeZoneOffset));
            return true;
        }

        private bool ApplyLongitude(double longitude)
        {
            if (!Set(_location.Longitude, longitude, v => _location.Longitude = v, nameof(Longitude))) return false;
            OnPropertyChanged(nameof(TimeZoneOffset));
            return true;
        }

        /// <summary>Decimal degrees with a comma or a dot, or DMS, as the Converter screen reads them.</summary>
        private static double? ParseWithin(string text, double limit) =>
            GeoFormat.ParseOrNull(text) is double value && Math.Abs(value) <= limit ? value : null;

        /// <summary>Written with a dot, which every culture reads back.</summary>
        private static string FieldText(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private bool Set<T>(T current, T value, Action<T> assign, [CallerMemberName] string? property = null)
        {
            if (EqualityComparer<T>.Default.Equals(current, value)) return false;

            assign(value);
            OnPropertyChanged(property);
            return true;
        }
    }
}
