using Compute.Core.Domain.Entities.Models.Distance;
using Compute.Core.Domain.Entities.Models.Fuel;

namespace Compute.Core.Tests.Domain
{
    public class FuelCalculatorTests
    {
        /// <summary>500 km, which is 310.7 miles.</summary>
        private const double FiveHundredKm = 500_000;

        [Fact]
        public void LitersPer100Km_CountsDownwards()
        {
            // 500 km at 7 L/100 km = 35 L.
            var estimate = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.LitersPer100Km, 7.0, null)!;

            Assert.Equal(35.0, estimate.Volume, 6);
        }

        [Fact]
        public void KilometersPerLiter_CountsUpwards()
        {
            // 500 km at 14 km/L = 35.7 L. The same car as above, quoted the Japanese way.
            var estimate = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.KilometersPerLiter, 14.0, null)!;

            Assert.Equal(35.71, estimate.Volume, 2);
        }

        [Fact]
        public void MilesPerGallon_UsesMilesAndCountsUpwards()
        {
            // 310.7 miles at 34 mpg = 9.14 US gallons.
            var estimate = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.MilesPerUsGallon, 34.0, null)!;

            Assert.Equal(9.14, estimate.Volume, 2);
        }

        [Fact]
        public void TheSameCarQuotedInUsAndUkMpg_NeedsTheSameFuel()
        {
            // A UK gallon is ~20% bigger, so the same car reads ~40 mpg there against 34 here —
            // and the fuel needed must agree once each is measured in its own gallon.
            var us = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.MilesPerUsGallon, 34.0, null)!;
            var uk = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.MilesPerImperialGallon, 40.8, null)!;

            double usLiters = us.Volume * 3.785411784;
            double ukLiters = uk.Volume * 4.54609;

            Assert.Equal(usLiters, ukLiters, 1);
        }

        [Fact]
        public void VolumeUnitFollowsTheMode()
        {
            Assert.Equal(FuelVolumeUnit.Liters, FuelCalculator.VolumeUnitFor(FuelConsumptionMode.LitersPer100Km));
            Assert.Equal(FuelVolumeUnit.Liters, FuelCalculator.VolumeUnitFor(FuelConsumptionMode.KilometersPerLiter));
            Assert.Equal(FuelVolumeUnit.UsGallons, FuelCalculator.VolumeUnitFor(FuelConsumptionMode.MilesPerUsGallon));
            Assert.Equal(FuelVolumeUnit.ImperialGallons, FuelCalculator.VolumeUnitFor(FuelConsumptionMode.MilesPerImperialGallon));
        }

        [Theory]
        [InlineData("US", DistanceType.Kilometers, FuelConsumptionMode.MilesPerUsGallon)]
        [InlineData("GB", DistanceType.Kilometers, FuelConsumptionMode.MilesPerImperialGallon)]
        [InlineData("JP", DistanceType.Kilometers, FuelConsumptionMode.KilometersPerLiter)]
        [InlineData("DE", DistanceType.Kilometers, FuelConsumptionMode.LitersPer100Km)]
        [InlineData("PL", DistanceType.Miles, FuelConsumptionMode.MilesPerUsGallon)]
        [InlineData(null, DistanceType.Kilometers, FuelConsumptionMode.LitersPer100Km)]
        public void TheDefaultFollowsWhereTheReaderIs(
            string? region, DistanceType unit, FuelConsumptionMode expected) =>
            Assert.Equal(expected, FuelCalculator.DefaultFor(region, unit));

        [Fact]
        public void APriceTurnsTheVolumeIntoACost()
        {
            var estimate = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.LitersPer100Km, 7.0, 1.80)!;

            Assert.Equal(63.0, estimate.Cost!.Value, 6);
        }

        [Fact]
        public void NothingToWorkFrom_IsBlankRatherThanZero()
        {
            Assert.Null(FuelCalculator.Estimate(0, FuelConsumptionMode.LitersPer100Km, 7, 1.8));
            Assert.Null(FuelCalculator.Estimate(FiveHundredKm, FuelConsumptionMode.LitersPer100Km, 0, 1.8));
        }

        [Fact]
        public void AZeroPrice_LeavesTheVolumeButNoCost()
        {
            var estimate = FuelCalculator.Estimate(
                FiveHundredKm, FuelConsumptionMode.LitersPer100Km, 7.0, 0)!;

            Assert.Equal(35.0, estimate.Volume, 6);
            Assert.Null(estimate.Cost);
        }
    }
}
