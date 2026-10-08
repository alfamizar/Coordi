using JustCompute.Shared.Helpers;
using Compute.Core.Domain.Entities.Models.Distance;
using System.Globalization;

namespace JustCompute.Features.Distance
{
    /// <summary>
    /// A distance in kilometres, in the unit the user chose, with its abbreviation: "377073.21 km".
    /// The value alone — the row it sits in says what it is.
    /// </summary>
    public class KilometersToTargetDistanceTypeConverter : IValueConverter
    {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double distanceInKm)
            {
                global::Compute.Core.Domain.Entities.Models.Distance.Distance distance = new(distanceInKm);
                DistanceType distanceType = global::JustCompute.Shared.Helpers.Settings.DistanceType;

                string abbreviationLabelKey = distanceType switch
                {
                    DistanceType.Meters => "MetersAbbreviationLabel",
                    DistanceType.Kilometers => "KilometersAbbreviationLabel",
                    DistanceType.Miles => "MilesAbbreviationLabel",
                    DistanceType.Feets => "FeetsAbbreviationLabel",
                    DistanceType.NauticalMiles => "NauticalMilesAbbreviationLabel",
                    _ => "KilometersAbbreviationLabel"
                };
                string abbreviationLabel = Strings.Get(abbreviationLabelKey);

                return string.Format(CultureInfo.CurrentCulture, "{0:0.##} {1}", distance.GetByType(distanceType), abbreviationLabel);
            }

            return $"Expected DistanceType type for {value?.ToString()}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
