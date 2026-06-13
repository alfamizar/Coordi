using Compute.Core.Extensions;

namespace Compute.Core.Tests.Extensions
{
    public class TimeExtensionsTests
    {
        [Fact]
        public void StripMilliseconds_RemovesSubSecondComponent()
        {
            var original = new TimeSpan(0, 1, 2, 3, 456);

            var stripped = original.StripMilliseconds();

            Assert.Equal(0, stripped.Milliseconds);
            Assert.Equal(new TimeSpan(0, 1, 2, 3), stripped);
        }

        [Fact]
        public void StripMilliseconds_PreservesDaysHoursMinutesSeconds()
        {
            var original = new TimeSpan(5, 10, 30, 45, 999);

            var stripped = original.StripMilliseconds();

            Assert.Equal(5, stripped.Days);
            Assert.Equal(10, stripped.Hours);
            Assert.Equal(30, stripped.Minutes);
            Assert.Equal(45, stripped.Seconds);
            Assert.Equal(0, stripped.Milliseconds);
        }

        [Fact]
        public void StripMilliseconds_NoSubSecondComponent_IsUnchanged()
        {
            var original = new TimeSpan(0, 2, 15, 0);

            Assert.Equal(original, original.StripMilliseconds());
        }
    }
}
