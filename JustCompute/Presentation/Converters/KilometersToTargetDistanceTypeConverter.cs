using Compute.Core.Domain.Entities.Models.Distance;
using JustCompute.Resources.Strings;
using JustCompute.Services;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace JustCompute.Presentation.Converters
{
    public class KilometersToTargetDistanceTypeConverter : IValueConverter
    {
        private readonly IStringLocalizer<AppStringsRes> _localizer = ServicesProvider.GetService<IStringLocalizer<AppStringsRes>>();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double distanceInKm)
            {
                Distance distance = new(distanceInKm);
                DistanceType distanceType = Helpers.Settings.DistanceType;

                string distanceLabelKey = "DistanceFormattedLabel";
                string abbreviationLabelKey = distanceType switch
                {
                    DistanceType.Meters => "MetersAbbreviationLabel",
                    DistanceType.Kilometers => "KilometersAbbreviationLabel",
                    DistanceType.Miles => "MilesAbbreviationLabel",
                    DistanceType.Feets => "FeetsAbbreviationLabel",
                    DistanceType.NauticalMiles => "NauticalMilesAbbreviationLabel",
                    _ => "KilometersAbbreviationLabel"
                };
                string distanceLabel = _localizer.GetString(distanceLabelKey);
                string abbreviationLabel = _localizer.GetString(abbreviationLabelKey);

                return $"{string.Format(distanceLabel, distance.GetByType(distanceType), abbreviationLabel)}";
            }

            return $"Expected DistanceType type for {value?.ToString()}";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
