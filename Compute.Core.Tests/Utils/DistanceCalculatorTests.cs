using Compute.Core.Domain.Entities.Models;
using Compute.Core.Utils;

namespace Compute.Core.Tests.Utils
{
    /// <summary>
    /// Speed is distance over time, and the time has to come from the GPS fixes themselves.
    /// Stamping each fix on arrival measured how long the callback took to reach the UI thread,
    /// so a stuttering main thread reported the traveller as slower than they were.
    /// </summary>
    public class DistanceCalculatorTests
    {
        // Two points ~111 m apart (0.001° of latitude), a clean number to reason about.
        private static readonly GeoPoint Start = new(51.5000, -0.1278);
        private static readonly GeoPoint OneStepNorth = new(51.5010, -0.1278);

        private static readonly DateTime T0 = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void GetSpeed_UsesTheIntervalBetweenFixes()
        {
            var calculator = new DistanceCalculator();

            calculator.AddFix(Start, T0);
            calculator.AddFix(OneStepNorth, T0.AddSeconds(10));

            var expected = Start.DistanceMetersTo(OneStepNorth) / 10.0;

            Assert.Equal(expected, calculator.GetSpeed(), 6);
        }

        /// <summary>
        /// The same two fixes ten seconds apart must give the same speed no matter how late the
        /// second callback arrived — that is the whole point of using the hardware timestamps.
        /// </summary>
        [Fact]
        public void GetSpeed_IsUnaffectedByWhenTheFixWasDelivered()
        {
            var prompt = new DistanceCalculator();
            prompt.AddFix(Start, T0);
            prompt.AddFix(OneStepNorth, T0.AddSeconds(10));

            var delayed = new DistanceCalculator();
            delayed.AddFix(Start, T0);
            // Callback stalled for a further 40 s; the fix itself is still 10 s after the first.
            Thread.Sleep(20);
            delayed.AddFix(OneStepNorth, T0.AddSeconds(10));

            Assert.Equal(prompt.GetSpeed(), delayed.GetSpeed(), 6);
        }

        [Fact]
        public void GetSpeed_IsZeroBeforeTwoFixesExist()
        {
            var calculator = new DistanceCalculator();
            Assert.Equal(0, calculator.GetSpeed());

            calculator.AddFix(Start, T0);
            Assert.Equal(0, calculator.GetSpeed());
        }

        /// <summary>Out-of-order or duplicate timestamps must not produce a negative speed.</summary>
        [Fact]
        public void GetSpeed_IsZeroWhenNoTimeHasPassed()
        {
            var calculator = new DistanceCalculator();

            calculator.AddFix(Start, T0);
            calculator.AddFix(OneStepNorth, T0);

            Assert.Equal(0, calculator.GetSpeed());
        }

        [Fact]
        public void AddFix_SeedsTheStartingPointAndTracksThePreviousOne()
        {
            var calculator = new DistanceCalculator();

            calculator.AddFix(Start, T0);
            Assert.Equal(Start, calculator.StartingPoint);
            Assert.Null(calculator.PreviousPoint);

            calculator.AddFix(OneStepNorth, T0.AddSeconds(5));
            Assert.Equal(Start, calculator.StartingPoint);
            Assert.Equal(Start, calculator.PreviousPoint);
            Assert.Equal(OneStepNorth, calculator.LastPoint);
            Assert.Equal(T0.AddSeconds(5), calculator.LastFixTimestampUtc);
        }

        [Fact]
        public void GetDirectDistance_MeasuresFromTheStartingPoint()
        {
            var calculator = new DistanceCalculator();

            calculator.AddFix(Start, T0);
            calculator.AddFix(OneStepNorth, T0.AddSeconds(5));

            Assert.Equal(Start.DistanceMetersTo(OneStepNorth), calculator.GetDirectDistance(), 6);
        }

        [Fact]
        public void Reset_ClearsEveryAccumulatedFix()
        {
            var calculator = new DistanceCalculator();
            calculator.StartAltitude = 42;
            calculator.AddFix(Start, T0);
            calculator.AddFix(OneStepNorth, T0.AddSeconds(5));

            calculator.Reset();

            Assert.Null(calculator.StartingPoint);
            Assert.Null(calculator.PreviousPoint);
            Assert.Null(calculator.LastPoint);
            Assert.Null(calculator.LastFixTimestampUtc);
            Assert.Null(calculator.StartAltitude);
            Assert.Equal(0, calculator.GetSpeed());
        }

        [Fact]
        public void Elevation_IsMeasuredFromBelowSeaLevelStarts()
        {
            // The Dead Sea shore. -1 used to mean "no altitude recorded", so a run starting
            // exactly a metre below sea level reported no climb at all.
            var calculator = new DistanceCalculator { StartAltitude = -1 };

            Assert.Equal(431, calculator.GetElevation(430));
            Assert.Equal(-9, calculator.GetElevation(-10));
        }

        [Fact]
        public void Elevation_IsZeroUntilBothAltitudesAreKnown()
        {
            var calculator = new DistanceCalculator();

            Assert.Equal(0, calculator.GetElevation(430));

            calculator.StartAltitude = 100;
            Assert.Equal(0, calculator.GetElevation(null));
            Assert.Equal(330, calculator.GetElevation(430));
        }

    }
}
