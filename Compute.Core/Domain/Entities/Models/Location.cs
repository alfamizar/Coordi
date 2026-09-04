using Compute.Astro;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Utils;

namespace Compute.Core.Domain.Entities.Models
{
    /// <summary>
    /// A place the app can show data for.
    ///
    /// A plain object: it raises no change notifications, because the domain layer should not
    /// have to know how a view redraws. The one screen that edits a location in place works on
    /// an observable copy of its own (EditableLocation) and hands back a finished Location.
    /// </summary>
    public class Location
    {
        private string? _timeZoneId;
        private bool _timeZoneIdResolved;
        private double _latitude;
        private double _longitude;

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>Moving the pin drops a zone that was resolved from the old coordinates.</summary>
        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                InvalidateResolvedTimeZone();
            }
        }

        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                InvalidateResolvedTimeZone();
            }
        }

        public City City { get; set; } = new();

        /// <summary>
        /// The placeholder the app falls back to before the user has chosen anywhere — so every
        /// screen has real data to show instead of an error. Greenwich is the natural choice:
        /// it is where the prime meridian is defined.
        /// </summary>
        public static Location CreatePlaceholder() => new()
        {
            Name = "London",
            Latitude = 51.5074,
            Longitude = -0.1278,
            // Named outright so the placeholder never depends on a lookup, and so it observes
            // British Summer Time like the real place does.
            TimeZoneId = "Europe/London",
        };

        /// <summary>Latitude as degrees/minutes/seconds, e.g. <c>N 51° 30' 26.64"</c>.</summary>
        public string LatitudeDms => GeoFormat.FormatDms(Latitude, GeoFormat.Axis.Latitude);

        /// <summary>Longitude as degrees/minutes/seconds, e.g. <c>W 0° 7' 40.08"</c>.</summary>
        public string LongitudeDms => GeoFormat.FormatDms(Longitude, GeoFormat.Axis.Longitude);

        /// <summary>
        /// True once this place exists as a row in the user's database. The device's own position
        /// and the Greenwich placeholder are neither editable nor deletable, and both are told
        /// apart by having no persisted id.
        /// </summary>
        public bool IsSaved => Id > 0;

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        /// <summary>
        /// This location's time zone: an IANA id such as <c>Asia/Tokyo</c>, resolved from the
        /// coordinates on first use, or a fixed <c>UTC±HH:MM</c> id when the user pinned the
        /// offset by hand. Persisted, so a saved place keeps the zone it was created with.
        /// </summary>
        public string TimeZoneId
        {
            get
            {
                if (!_timeZoneIdResolved)
                {
                    _timeZoneId = TimeZoneUtils.GetTimeZoneId(Latitude, Longitude);
                    _timeZoneIdResolved = true;
                }

                return _timeZoneId ?? string.Empty;
            }
            set
            {
                _timeZoneId = value;
                _timeZoneIdResolved = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// The UTC offset in force here at the given instant. Ask per date — the answer changes
        /// across a daylight-saving boundary, which is exactly what time travel crosses.
        /// </summary>
        public TimeSpan GetUtcOffset(DateTime utc) =>
            TimeZoneUtils.GetUtcOffset(TimeZoneId, utc, Longitude);

        /// <summary>Same, in hours, for the astronomy layer.</summary>
        public double GetUtcOffsetHours(DateTime utc) => GetUtcOffset(utc).TotalHours;

        /// <summary>
        /// The offset in force right now — what the UI displays. Assigning one (from the Add
        /// Location picker) pins the location to that fixed offset, overriding the zone lookup.
        /// </summary>
        public TimeZoneOffset TimeZoneOffset
        {
            get => TimeZoneOffset.FromOffset(GetUtcOffset(DateTime.UtcNow));
            set
            {
                if (value is null) return;

                // A two-way picker writes its current value straight back when the screen loads.
                // Pinning on that would replace the resolved zone — and its daylight saving —
                // for every location merely opened in the editor. Only a genuine change pins.
                if (value.Offset == GetUtcOffset(DateTime.UtcNow)) return;

                TimeZoneId = TimeZoneUtils.ToFixedOffsetId(value.Offset);
            }
        }

        /// <summary>
        /// A detached copy. The edit screen binds straight to the object it is given, so without
        /// this a cancelled edit would leave its changes in every list still holding that instance.
        /// </summary>
        public Location Clone() => new()
        {
            Id = Id,
            Name = Name,
            Latitude = Latitude,
            Longitude = Longitude,
            IsActive = IsActive,
            IsCurrent = IsCurrent,
            TimeZoneId = TimeZoneId,
            City = new City
            {
                Id = City.Id,
                CityName = City.CityName,
                CountryName = City.CountryName,
                Population = City.Population,
            },
        };

        /// <summary>
        /// Moving the pin invalidates a looked-up zone, but never an offset the user chose
        /// deliberately — that override is the whole point of the picker.
        /// </summary>
        private void InvalidateResolvedTimeZone()
        {
            // Only a hand-picked offset survives the move. A bare "UTC" here is a lookup
            // result — the answer for (0, 0), which is what a location reads as before its
            // coordinates are filled in — and treating that as a pin left the place reporting
            // UTC wherever it was subsequently put.
            if (TimeZoneUtils.IsPinnedFixedOffset(_timeZoneId)) return;

            _timeZoneId = null;
            _timeZoneIdResolved = false;
        }
    }
}
