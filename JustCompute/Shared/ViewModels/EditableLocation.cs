using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Time;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Shared.ViewModels
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

        public EditableLocation() : this(new Location()) { }

        public EditableLocation(Location location)
        {
            // Detached: a cancelled edit must not leave its changes in the list still holding
            // the instance it was opened from.
            _location = location.Clone();
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

        public double Latitude
        {
            get => _location.Latitude;
            set
            {
                if (Set(_location.Latitude, value, v => _location.Latitude = v))
                {
                    // The zone is resolved from the coordinates, so the displayed offset moves
                    // with them — unless the user has pinned one, which the domain model decides.
                    OnPropertyChanged(nameof(TimeZoneOffset));
                }
            }
        }

        public double Longitude
        {
            get => _location.Longitude;
            set
            {
                if (Set(_location.Longitude, value, v => _location.Longitude = v))
                {
                    OnPropertyChanged(nameof(TimeZoneOffset));
                }
            }
        }

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

        private bool Set<T>(T current, T value, Action<T> assign,
                            [System.Runtime.CompilerServices.CallerMemberName] string? property = null)
        {
            if (EqualityComparer<T>.Default.Equals(current, value)) return false;

            assign(value);
            OnPropertyChanged(property);
            return true;
        }
    }
}
