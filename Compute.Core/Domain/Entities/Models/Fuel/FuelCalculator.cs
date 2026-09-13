using Compute.Core.Domain.Entities.Models.Distance;

namespace Compute.Core.Domain.Entities.Models.Fuel
{
    /// <summary>
    /// What a route costs to drive, in whichever way the reader counts fuel.
    ///
    /// Money stays unitless: the app has no idea whether someone spends euros or forints, and a
    /// wrong currency symbol is worse than none. The volume does not — it follows from the
    /// consumption mode, so a price "per gallon" is not quietly multiplied by litres.
    /// </summary>
    public static class FuelCalculator
    {
        private const double MilesPerKilometer = 0.621371;

        public static FuelVolumeUnit VolumeUnitFor(FuelConsumptionMode mode) => mode switch
        {
            FuelConsumptionMode.MilesPerUsGallon => FuelVolumeUnit.UsGallons,
            FuelConsumptionMode.MilesPerImperialGallon => FuelVolumeUnit.ImperialGallons,
            _ => FuelVolumeUnit.Liters,
        };

        /// <summary>
        /// The convention someone is most likely to want, from where they are and how they
        /// already asked to see distances.
        ///
        /// A guess, and only a starting point — the picker is right there. Region first, because
        /// it is the stronger signal: a British user who switched the app to kilometres still
        /// almost certainly buys fuel by the imperial gallon.
        /// </summary>
        public static FuelConsumptionMode DefaultFor(string? regionCode, DistanceType distanceUnit) =>
            regionCode?.ToUpperInvariant() switch
            {
                "US" or "PR" or "GU" or "VI" => FuelConsumptionMode.MilesPerUsGallon,
                "GB" or "IM" or "JE" or "GG" => FuelConsumptionMode.MilesPerImperialGallon,
                // Where km/L is the everyday figure rather than L/100 km.
                "JP" or "KR" or "IN" or "BD" or "LK" or "NP" or "MM" or "EG" or "NG" or "ZA"
                    => FuelConsumptionMode.KilometersPerLiter,
                _ => distanceUnit == DistanceType.Miles
                    ? FuelConsumptionMode.MilesPerUsGallon
                    : FuelConsumptionMode.LitersPer100Km,
            };

        /// <summary>
        /// The fuel a route needs, and what it costs.
        ///
        /// Returns null when there is nothing to work from — no distance, or no consumption
        /// figure. A zero would read as an answer rather than a blank.
        /// </summary>
        public static FuelEstimate? Estimate(
            double meters, FuelConsumptionMode mode, double consumption, double? pricePerVolume)
        {
            if (meters <= 0 || consumption <= 0)
            {
                return null;
            }

            double kilometers = meters / 1000.0;
            double miles = kilometers * MilesPerKilometer;

            double volume = mode switch
            {
                // Counting downwards: more litres per distance means more fuel.
                FuelConsumptionMode.LitersPer100Km => kilometers / 100.0 * consumption,

                // Counting upwards: more distance per unit of fuel means less fuel.
                FuelConsumptionMode.KilometersPerLiter => kilometers / consumption,
                FuelConsumptionMode.MilesPerUsGallon => miles / consumption,
                FuelConsumptionMode.MilesPerImperialGallon => miles / consumption,

                _ => kilometers / 100.0 * consumption,
            };

            return new FuelEstimate(volume, pricePerVolume is > 0 ? volume * pricePerVolume : null);
        }
    }
}
