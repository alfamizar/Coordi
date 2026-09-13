using Compute.Astro;
using Compute.Core.Domain.Entities.Models;
using Compute.Core.Domain.Entities.Models.Weather;

namespace Compute.Core.Tests.Domain
{
    public class SkySummaryTests
    {
        [Theory]
        [InlineData(WeatherCondition.Clear, SkyVerdict.Clear)]
        [InlineData(WeatherCondition.PartlyCloudy, SkyVerdict.Partly)]
        [InlineData(WeatherCondition.Cloudy, SkyVerdict.Cloudy)]
        [InlineData(WeatherCondition.Rain, SkyVerdict.Cloudy)]
        [InlineData(WeatherCondition.Thunderstorm, SkyVerdict.Cloudy)]
        [InlineData(WeatherCondition.Unknown, SkyVerdict.Unknown)]
        public void VerdictFollowsTheCondition(WeatherCondition condition, SkyVerdict expected) =>
            Assert.Equal(expected, SkySummary.VerdictFor(condition));

        [Fact]
        public void FogIsCloudy_BecauseTheSkyCannotBeSeenThroughIt()
        {
            Assert.Equal(SkyVerdict.Cloudy, SkySummary.VerdictFor(WeatherCondition.Fog));
        }

        [Fact]
        public void DarkWindowRunsFromLastLightToTheNextMorningsFirstLight()
        {
            var day = new DateTime(2026, 3, 20);
            List<TwilightStage> stages =
            [
                new(TwilightLabel.FirstLight, day.AddHours(4).AddMinutes(8)),
                new(TwilightLabel.CivilDawn, day.AddHours(5).AddMinutes(28)),
                new(TwilightLabel.CivilDusk, day.AddHours(18).AddMinutes(48)),
                new(TwilightLabel.LastLight, day.AddHours(20).AddMinutes(8)),
            ];

            var window = SkySummary.DarkWindow(stages);

            Assert.NotNull(window);
            Assert.Equal(day.AddHours(20).AddMinutes(8), window!.Value.From);
            // Carried to the following morning, not left as this morning's 04:08.
            Assert.Equal(day.AddDays(1).AddHours(4).AddMinutes(8), window.Value.To);
            Assert.True(window.Value.To > window.Value.From);
        }

        [Fact]
        public void NoAstronomicalDarkness_HasNoWindow()
        {
            // A northern summer night: the Sun crosses -12 but never -18, so the astronomical
            // stages are absent and there is no dark window to report.
            var day = new DateTime(2026, 6, 21);
            List<TwilightStage> stages =
            [
                new(TwilightLabel.NauticalDawn, day.AddHours(2).AddMinutes(51)),
                new(TwilightLabel.CivilDawn, day.AddHours(4)),
                new(TwilightLabel.CivilDusk, day.AddHours(21).AddMinutes(57)),
                new(TwilightLabel.NauticalDusk, day.AddHours(23).AddMinutes(6)),
            ];

            Assert.Null(SkySummary.DarkWindow(stages));
        }

        [Fact]
        public void NoStagesAtAll_HasNoWindow() =>
            Assert.Null(SkySummary.DarkWindow([]));
    }
}
