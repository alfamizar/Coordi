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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeZoneOffset))]
        private string latitude = "0";

        public double LatitudeDouble
        {
            get
            {
                if (double.TryParse(Latitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double latValue) && latValue >= -90 && latValue <= 90)
                {
                    return Math.Round(latValue, 8);
                }
                return 0;
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeZoneOffset))]
        private string longitude = "0";

        public double LongitudeDouble
        {
            get
            {
                if (double.TryParse(Longitude.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lonValue) && lonValue >= -180 && lonValue <= 180)
                {
                    return Math.Round(lonValue, 8);
                }
                return 0;
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
