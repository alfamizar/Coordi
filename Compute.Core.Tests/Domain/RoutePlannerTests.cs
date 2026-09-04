using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Route;

namespace Compute.Core.Tests.Domain
{
    public class RoutePlannerTests
    {
        private static Location At(double lat, double lon, string name = "") =>
            new() { Name = name, Latitude = lat, Longitude = lon };

        private static readonly Location London = At(51.5074, -0.1278, "London");
        private static readonly Location Paris = At(48.8566, 2.3522, "Paris");
        private static readonly Location Berlin = At(52.5200, 13.4050, "Berlin");

        [Fact]
        public void FewerThanTwoPoints_HasNoRoute()
        {
            Assert.Null(RoutePlanner.Measure([]));
            Assert.Null(RoutePlanner.Measure([London]));
        }

        [Fact]
        public void TwoPoints_MeasureTheKnownDistanceAndBearing()
        {
            var route = RoutePlanner.Measure([London, Paris])!;

            // London–Paris is ~343 km; the initial bearing is south-east, around 148°.
            Assert.Single(route.Legs);
            Assert.InRange(route.TotalMeters / 1000, 340, 346);
            Assert.InRange(route.Legs[0].InitialBearingDeg, 140, 155);

            // With two points the route is the straight line, so there is no detour.
            Assert.Equal(route.TotalMeters, route.DirectMeters, 3);
            Assert.Equal(1.0, route.DetourRatio!.Value, 6);
        }

        [Fact]
        public void LegsAreNumberedFromOne_AndJoinConsecutivePoints()
        {
            var route = RoutePlanner.Measure([London, Paris, Berlin])!;

            Assert.Equal(2, route.Legs.Count);
            Assert.Equal((1, 2), (route.Legs[0].FromNumber, route.Legs[0].ToNumber));
            Assert.Equal((2, 3), (route.Legs[1].FromNumber, route.Legs[1].ToNumber));
        }

        [Fact]
        public void TotalIsTheSumOfLegs_AndDirectIsFirstToLast()
        {
            var route = RoutePlanner.Measure([London, Paris, Berlin])!;

            Assert.Equal(route.Legs.Sum(l => l.DistanceMeters), route.TotalMeters, 3);

            var straight = RoutePlanner.Measure([London, Berlin])!;
            Assert.Equal(straight.TotalMeters, route.DirectMeters, 3);
        }

        [Fact]
        public void GoingViaSomewhere_IsLongerThanGoingDirect()
        {
            var route = RoutePlanner.Measure([London, Paris, Berlin])!;

            Assert.True(route.TotalMeters > route.DirectMeters);
            Assert.True(route.DetourRatio > 1.0);
        }

        [Fact]
        public void ARouteReturningToItsStart_HasNoDetourRatio()
        {
            // Dividing by a line of zero length would report something spectacular and meaningless.
            var loop = RoutePlanner.Measure([London, Paris, London])!;

            Assert.Null(loop.DetourRatio);
            Assert.True(loop.TotalMeters > 0);
        }

        [Fact]
        public void RepeatedPoints_MeasureZeroWithoutFailing()
        {
            var route = RoutePlanner.Measure([London, London])!;

            Assert.Equal(0, route.TotalMeters, 3);
            Assert.Null(route.DetourRatio);
        }
    }
}
