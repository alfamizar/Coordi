using CommunityToolkit.Mvvm.ComponentModel;
using Compute.Core.Domain.Entities.Models.Time;
using Compute.Core.Utils;
using System.Globalization;

namespace Compute.Core.Domain.Entities.Models
{
    public partial class Location : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        private string name = string.Empty;

        // String property for data binding with Entry views
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeZoneOffset))]
        private string latitude = "0";

        // Computed property for double representation of latitude
        public double LatitudeDouble
        {
            get
            {
                if (double.TryParse(Latitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double latValue) && latValue >= -90 && latValue <= 90)
                {
                    return Math.Round(latValue, 8);
                }
                // Handle invalid format
                return 0; // or throw an exception or log an error
            }
        }

        // String property for data binding with Entry views
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeZoneOffset))]
        private string longitude = "0";

        // Computed property for double representation of longitude
        public double LongitudeDouble
        {
            get
            {
                if (double.TryParse(Longitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lonValue) && lonValue >= -180 && lonValue <= 180)
                {
                    return Math.Round(lonValue, 8);
                }
                // Handle invalid format
                return 0; // or throw an exception or log an error
            }
        }

        [ObservableProperty]
        private City city = new();

        public bool IsActive { get; set; }

        public bool IsCurrent { get; set; }

        public TimeZoneOffset TimeZoneOffset {
            get 
            {
                var timeZoneId = TimeZoneUtils.GetTimeZoneId(LatitudeDouble, LongitudeDouble);

                return TimeZoneOffset
                    .GetUtcOffsets()
                    .FirstOrDefault(offset => offset.DisplayName == timeZoneId) ?? TimeZoneOffset.DefaultTimeZoneOffset;
            } 
        }
    }
}
